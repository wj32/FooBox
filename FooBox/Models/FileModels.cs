using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace FooBox.Models
{
    public class FileManager : IDisposable
    {
        public const string RootFolderTag = "Root";
        public const string InternalClientTag = "Internal";
        private const string IdChars = "abcdefghijklmnopqrstuvwxyz0123456789!@^_-";

        public static bool IsFooBoxSetUp()
        {
            using (var context = new FooBoxContext())
                return context.Files.Count() != 0;
        }

        private FooBoxContext _context;
        private bool _contextOwned;
        private System.Security.Cryptography.HashAlgorithm _sha512Algorithm = System.Security.Cryptography.SHA512.Create();

        #region Class

        public FileManager()
            : this(new FooBoxContext(), true)
        { }

        public FileManager(FooBoxContext context)
            : this(context, false)
        { }

        public FileManager(FooBoxContext context, bool contextOwned)
        {
            _context = context;
            _contextOwned = contextOwned;
        }

        public void Dispose()
        {
            if (_contextOwned)
                _context.Dispose();
            _sha512Algorithm.Dispose();
        }

        public FooBoxContext Context
        {
            get { return _context; }
        }

        #endregion

        #region Setup

        public void InitialSetup()
        {
            using (var userManager = new UserManager(_context))
            {
                if (_context.Files.Count() != 0)
                    throw new Exception("The database is already set up.");

                var rootFolder = _context.Folders.Add(new Folder
                {
                    Name = "",
                    Tag = RootFolderTag,
                    Owner = userManager.GetDefaultUser()
                });

                _context.SaveChanges();
            }
        }

        #endregion

        #region Blobs

        public static readonly string BlobDataDirectory = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Blobs");

        private static object _blobLock = new object();

        private DirectoryInfo AccessBlobDataDirectory()
        {
            if (!Directory.Exists(BlobDataDirectory))
                return Directory.CreateDirectory(BlobDataDirectory);

            return new DirectoryInfo(BlobDataDirectory);
        }

        public string GenerateBlobKey()
        {
            Random r = new Random();
            char[] c = new char[32];

            for (int i = 0; i < c.Length; i++)
                c[i] = IdChars[r.Next(0, IdChars.Length)];

            return new string(c);
        }

        public string GetBlobFileName(string blobKey)
        {
            return BlobDataDirectory + "\\" + blobKey;
        }

        public string FindBlob(string hash)
        {
            var upperHash = hash.ToUpperInvariant();
            return (from blob in _context.Blobs where blob.Hash == upperHash select blob.Key).SingleOrDefault();
        }

        #endregion

        #region Files

        public Document FindDocument(long documentId)
        {
            return (from document in _context.Documents where document.Id == documentId select document).SingleOrDefault();
        }

        public Folder FindFolder(long folderId)
        {
            return (from folder in _context.Folders where folder.Id == folderId select folder).SingleOrDefault();
        }

        public Folder GetRootFolder()
        {
            return (from folder in _context.Folders where folder.Tag == RootFolderTag select folder).Single();
        }

        public Folder CreateUserRootFolder(User user)
        {
            var userRootFolder = _context.Folders.Add(new Folder
            {
                Name = user.Id.ToString(),
                Owner = user
            });
            user.RootFolder = userRootFolder;
            GetRootFolder().Files.Add(userRootFolder);

            _context.SaveChanges();

            return userRootFolder;
        }

        #endregion

        #region Clients

        private string GenerateClientSecret()
        {
            Random r = new Random();
            char[] c = new char[128];

            for (int i = 0; i < c.Length; i++)
                c[i] = IdChars[r.Next(0, IdChars.Length)];

            return new string(c);
        }

        public Client CreateClient(long userId, string name, string tag = null)
        {
            var client = _context.Clients.Add(new Client
            {
                Name = name,
                Tag = tag,
                UserId = userId,
                Secret = GenerateClientSecret(),
                AccessTime = DateTime.UtcNow
            });
            _context.SaveChanges();

            return client;
        }

        public Client FindClient(long clientId)
        {
            return (from client in _context.Clients where client.Id == clientId select client).SingleOrDefault();
        }

        public Client GetInternalClient(long userId)
        {
            return (from client in _context.Clients where client.UserId == userId && client.Tag == InternalClientTag select client).Single();
        }

        public bool DeleteClient(long clientId)
        {
            Client client = FindClient(clientId);

            if (client == null)
                return false;

            client.State = ObjectState.Deleted;

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Client upload

        public static readonly string ClientUploadDirectory = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Uploads");

        private DirectoryInfo AccessClientUploadDirectory(long clientId)
        {
            string path = ClientUploadDirectory + "\\" + clientId.ToString();

            if (!Directory.Exists(path))
                return Directory.CreateDirectory(path);

            return new DirectoryInfo(path);
        }

        #endregion

        #region Synchronization

        public ClientSyncResult SyncClientChanges(ClientSyncData clientData)
        {
            var clientNodes = ChangeNode.FromItems(clientData.Changes);

            using (var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            using (var userManager = new UserManager(_context))
            {
                // Construct the list of changes that have occurred from the client's base changelist ID up to now.

                var intermediateChanges =
                    from changelist in _context.Changelists
                    where changelist.Id > clientData.BaseChangelistId
                    join change in _context.Changes on changelist.Id equals change.ChangelistId into changes
                    orderby changelist.Id
                    select new { ChangeListId = changelist.Id, Changes = changes };
                var intermediateNodes =
                    from x in intermediateChanges
                    select new
                    {
                        ChangelistId = x.ChangeListId,
                        Nodes = ChangeNode.FromItems(from change in x.Changes
                                                     select new ChangeItem
                                                         {
                                                             FullName = change.FullFileName,
                                                             Type = change.Type,
                                                             IsFolder = change.IsFolder
                                                         })
                    };

                var mergedNodes = ChangeNode.CreateRoot();
                long lastChangelistId = clientData.BaseChangelistId;

                foreach (var changelist in intermediateNodes)
                {
                    mergedNodes.SequentialMerge(changelist.Nodes);
                    lastChangelistId = changelist.ChangelistId;
                }

                // Check if the client's changes conflict with ours.

                if (mergedNodes.PreservingConflicts(clientNodes))
                {
                    return new ClientSyncResult
                    {
                        State = ClientSyncResultState.Conflict,
                        LastChangelistId = lastChangelistId,
                        Changes = mergedNodes.ToItems()
                    };
                }

                // The client's changes don't conflict, so turn it into a sequential changelist.

                mergedNodes.MakeSequentialByPreserving(clientNodes);

                // Check for all required data.

                var clientChangesByFullName = clientData.Changes.ToDictionary(change => change.FullName);
                var uploadDirectory = AccessClientUploadDirectory(clientData.ClientId);
                var presentHashes = new Dictionary<string, Blob>();
                var missingHashes = new HashSet<string>();
                var missingBlobHashes = new Dictionary<string, ClientChange>();

                foreach (var addNode in clientNodes.RecursiveEnumerate())
                {
                    if (addNode.Type != ChangeType.Add || !addNode.IsFolder)
                        continue;

                    var clientChange = clientChangesByFullName[addNode.FullName];
                    var hash = clientChange.Hash.ToUpperInvariant();

                    if (presentHashes.ContainsKey(hash) || missingHashes.Contains(hash))
                        continue;

                    var blobForHash = (from blob in _context.Blobs where blob.Hash == hash select blob).SingleOrDefault();

                    if (blobForHash != null || System.IO.File.Exists(uploadDirectory.FullName + "\\" + hash))
                    {
                        presentHashes.Add(hash, blobForHash);

                        if (blobForHash == null)
                            missingBlobHashes.Add(hash, clientChange);
                    }
                    else
                    {
                        missingHashes.Add(hash);
                    }
                }

                if (missingHashes.Count != 0)
                {
                    return new ClientSyncResult
                    {
                        State = ClientSyncResultState.UploadRequired,
                        LastChangelistId = lastChangelistId,
                        Changes = mergedNodes.ToItems(),
                        UploadRequiredFor = missingHashes
                    };
                }

                // Create blobs for hashes with no associated blobs.

                foreach (var hash in missingBlobHashes.Keys)
                {
                    var clientChange = missingBlobHashes[hash];
                    var blobKey = GenerateBlobKey();

                    try
                    {
                        System.IO.File.Copy(uploadDirectory.FullName + "\\" + hash, GetBlobFileName(blobKey));
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            System.IO.File.Delete(GetBlobFileName(blobKey));
                        }
                        catch
                        { }

                        return new ClientSyncResult
                        {
                            State = ClientSyncResultState.Error,
                            Exception = ex
                        };
                    }

                    presentHashes[hash] = _context.Blobs.Add(new Blob { Key = blobKey, Size = clientChange.Size, Hash = hash });
                }

                // Apply the changes to the database.

                return null;
            }
        }

        #endregion
    }

    public class ClientChange : ChangeItem
    {
        public long Size { get; set; }
        public string Hash { get; set; }
        public string UploadFileName { get; set; }
        public FileStream UploadStream { get; set; }
        public string DisplayName { get; set; }

        public Blob AssociatedBlob { get; set; }
    }

    public class ClientSyncData
    {
        public ClientSyncData()
        {
            this.Changes = new HashSet<ClientChange>();
        }

        /// <summary>
        /// The client that is making the changes.
        /// </summary>
        public long ClientId { get; set; }

        /// <summary>
        /// The changelist that the client is currently synchronized to.
        /// </summary>
        public long BaseChangelistId { get; set; }

        public ICollection<ClientChange> Changes { get; set; }
    }

    public enum ClientSyncResultState
    {
        Error,
        Conflict,
        UploadRequired,
        Success
    }

    public class ClientSyncResult
    {
        public ClientSyncResultState State { get; set; }
        public Exception Exception { get; set; }

        // >= Conflict

        public long LastChangelistId { get; set; }
        public ICollection<ChangeItem> Changes { get; set; }

        // >= UploadRequired

        public ICollection<string> UploadRequiredFor { get; set; }

        // >= Success

        public long NewChangelistId { get; set; }
    }
}