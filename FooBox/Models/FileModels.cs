using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace FooBox.Models
{
    public class FileManager : IDisposable
    {
        public const string RootFolderTag = "Root";
        public const string InternalClientTag = "Internal";
        public const string IdChars = "abcdefghijklmnopqrstuvwxyz0123456789!@^_-";

        public static bool IsFooBoxSetUp()
        {
            using (var context = new FooBoxContext())
                return context.Files.Any();
        }

        private FooBoxContext _context;
        private bool _contextOwned;

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
                if (_context.Files.Any())
                    throw new Exception("The database is already set up.");

                // Create the root folder.

                var rootFolder = _context.Folders.Add(new Folder
                {
                    Name = "",
                    DisplayName = "",
                    Tag = RootFolderTag,
                    Owner = userManager.GetDefaultUser()
                });

                // Create the default internal client for the default user.

                var internalClient = CreateClient(userManager.GetDefaultUser().Id, "Internal", FileManager.InternalClientTag);

                // Create the base changelist.

                var baseChangelist = _context.Changelists.Add(new Changelist
                {
                    TimeStamp = DateTime.UtcNow,
                    Client = internalClient
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

        public System.Security.Cryptography.HashAlgorithm CreateBlobHashAlgorithm()
        {
            return System.Security.Cryptography.SHA256.Create();
        }

        public string GenerateBlobKey()
        {
            return Utilities.GenerateRandomString(IdChars, Blob.KeyLength);
        }

        public string GetBlobFileName(string blobKey)
        {
            return BlobDataDirectory + "\\" + blobKey;
        }

        public string FindBlob(string hash)
        {
            var upperHash = hash.ToUpperInvariant();
            return (from blob in _context.Blobs where blob.Hash == upperHash select blob.Key).FirstOrDefault();
        }

        private Blob CreateBlob(long size, string hash, string fileName)
        {
            var blobKey = GenerateBlobKey();

            try
            {
                AccessBlobDataDirectory();
                System.IO.File.Copy(fileName, GetBlobFileName(blobKey));
            }
            catch
            {
                try
                {
                    System.IO.File.Delete(GetBlobFileName(blobKey));
                }
                catch
                { }

                throw;
            }

            return _context.Blobs.Add(new Blob { Key = blobKey, Size = size, Hash = hash });
        }

        #endregion

        #region Files

        public File FindFile(long fileId)
        {
            return (from file in _context.Files where file.Id == fileId select file).SingleOrDefault();
        }

        public File FindFile(string fullName)
        {
            string fullDisplayName;
            return FindFile(fullName, null, out fullDisplayName);
        }

        public File FindFile(string fullName, Folder root, out string fullDisplayName)
        {
            File file = root ?? GetRootFolder();
            string[] names = fullName.ToUpperInvariant().Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder fullDisplayNameSb = new StringBuilder();

            foreach (var name in names)
            {
                if (!(file is Folder))
                {
                    fullDisplayName = null;
                    return null;
                }

                file = ((Folder)file).Files.AsQueryable().Where(subFile => subFile.Name == name).SingleOrDefault();

                if (file == null)
                {
                    fullDisplayName = null;
                    return null;
                }

                fullDisplayNameSb.Append('/');
                fullDisplayNameSb.Append(file.DisplayName);
            }

            fullDisplayName = fullDisplayNameSb.ToString();

            return file;
        }

        public string GetFullDisplayName(File file, Folder root = null)
        {
            List<string> displayNames = new List<string>();
            StringBuilder fullDisplayNameSb = new StringBuilder();
            Folder parentFolder;

            if (file.Name.Length == 0 || file == root)
                return "";

            displayNames.Add(file.DisplayName);
            parentFolder = file.ParentFolder;

            while (parentFolder != null)
            {
                if (parentFolder.Name.Length == 0 || parentFolder == root)
                    break;

                displayNames.Add(parentFolder.DisplayName);
                parentFolder = parentFolder.ParentFolder;
            }

            for (int i = displayNames.Count - 1; i >= 0; i--)
            {
                fullDisplayNameSb.Append('/');
                fullDisplayNameSb.Append(displayNames[i]);
            }

            return fullDisplayNameSb.ToString();
        }

        public string GetFullName(File file, Folder root = null)
        {
            return GetFullDisplayName(file, root).ToUpperInvariant();
        }

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
                DisplayName = user.Id.ToString(),
                Owner = user
            });
            user.RootFolder = userRootFolder;
            GetRootFolder().Files.Add(userRootFolder);

            _context.SaveChanges();

            return userRootFolder;
        }

        public Folder GetUserRootFolder(long userId)
        {
            using (var userManager = new UserManager(_context))
                return userManager.FindUser(userId).RootFolder;
        }

        #endregion

        #region Clients

        private string GenerateClientSecret()
        {
            return Utilities.GenerateRandomString(IdChars, 128);
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

        public DirectoryInfo AccessClientUploadDirectory(long clientId)
        {
            string path = ClientUploadDirectory + "\\" + clientId.ToString();

            if (!Directory.Exists(path))
                return Directory.CreateDirectory(path);

            return new DirectoryInfo(path);
        }

        #endregion

        #region Synchronization

        public long GetLastChangelistId()
        {
            return (from changelist in _context.Changelists
                    orderby changelist.Id descending
                    select changelist.Id).FirstOrDefault();
        }

        public ClientSyncResult SyncClientChanges(ClientSyncData clientData)
        {
            try
            {
                var clientNode = ChangeNode.FromItems(clientData.Changes);
                var clientChangesByFullName = clientData.Changes.ToDictionary(change => Utilities.NormalizeFullName(change.FullName).ToUpperInvariant());

                // Verify that the display names match the names, and create missing entries in clientChangesByFullName.
                foreach (var node in clientNode.RecursiveEnumerate())
                {
                    if (node.Type == ChangeType.Add || node.Type == ChangeType.SetDisplayName || node.Type == ChangeType.Undelete)
                    {
                        if (clientChangesByFullName.ContainsKey(node.FullName))
                        {
                            string displayName = clientChangesByFullName[node.FullName].DisplayName;

                            if (node.Type != ChangeType.Undelete && string.IsNullOrEmpty(displayName))
                                throw new Exception("Missing display name for " + node.Type.ToString() + " operation on '" + node.FullName + "'");
                            if (!string.IsNullOrEmpty(displayName) && displayName.ToUpperInvariant() != node.Name)
                                throw new Exception("Invalid display name '" + displayName + "' for '" + node.FullName + "'");
                        }
                        else
                        {
                            // This node must be a folder with no display name specified.
                            // Create a dummy entry.
                            clientChangesByFullName[node.FullName] = new ClientChange
                            {
                                DisplayName = null
                            };
                        }
                    }
                }

                // Construct the list of changes that have occurred from the client's base changelist ID up to now.

                var intermediateChanges =
                    from changelist in _context.Changelists
                    where changelist.Id > clientData.BaseChangelistId
                    join change in _context.Changes on changelist.Id equals change.ChangelistId into changes
                    orderby changelist.Id
                    select new { ChangeListId = changelist.Id, Changes = changes };
                var intermediateNodes =
                    from x in intermediateChanges.AsEnumerable()
                    select new
                    {
                        ChangelistId = x.ChangeListId,
                        Nodes = ChangeNode.FromItems(from change in x.Changes
                                                        select new ChangeItem
                                                            {
                                                                FullName = change.FullName,
                                                                Type = change.Type,
                                                                IsFolder = change.IsFolder
                                                            })
                    };

                var mergedNode = ChangeNode.CreateRoot();
                long lastChangelistId = clientData.BaseChangelistId;

                foreach (var changelist in intermediateNodes)
                {
                    mergedNode.SequentialMerge(changelist.Nodes);
                    lastChangelistId = changelist.ChangelistId;
                }

                // Check if the client's changes conflict with ours.

                if (mergedNode.PreservingConflicts(clientNode))
                {
                    return new ClientSyncResult
                    {
                        State = ClientSyncResultState.Conflict,
                        LastChangelistId = lastChangelistId,
                        Changes = mergedNode.ToItems(),
                        UploadRequiredFor = new HashSet<string>()
                    };
                }

                // The client's changes don't conflict, so turn it into a sequential changelist.

                mergedNode.MakeSequentialByPreserving(clientNode);

                // Check for all required data.

                var uploadDirectory = AccessClientUploadDirectory(clientData.ClientId);
                var presentHashes = new Dictionary<string, Blob>();
                var missingHashes = new HashSet<string>();
                var missingBlobHashes = new Dictionary<string, ClientChange>();

                foreach (var addNode in clientNode.RecursiveEnumerate())
                {
                    if (addNode.Type != ChangeType.Add || addNode.IsFolder)
                        continue;

                    var clientChange = clientChangesByFullName[addNode.FullName];
                    var hash = clientChange.Hash.ToUpperInvariant();

                    if (presentHashes.ContainsKey(hash) || missingHashes.Contains(hash))
                        continue;

                    var blobForHash = (from blob in _context.Blobs where blob.Hash == hash select blob).FirstOrDefault();

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
                        Changes = mergedNode.ToItems(),
                        UploadRequiredFor = missingHashes
                    };
                }

                // Create blobs for hashes with no associated blobs.

                foreach (var hash in missingBlobHashes.Keys)
                {
                    var clientChange = missingBlobHashes[hash];
                    presentHashes[hash] = CreateBlob(clientChange.Size, hash, uploadDirectory.FullName + "\\" + hash);
                }

                if (missingBlobHashes.Count != 0)
                    _context.SaveChanges();

                // Apply the changes to the database.

                using (var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    // Lock the *entire* changelist table.
                    _context.Database.ExecuteSqlCommand("SELECT TOP 1 Id FROM dbo.Changelists WITH (TABLOCKX, HOLDLOCK)");

                    // Make sure no one changed anything since we started computing changes.
                    var moreChangelists = (from changelist in _context.Changelists
                                           where changelist.Id > lastChangelistId
                                           select changelist.Id).Any();

                    if (moreChangelists)
                        return new ClientSyncResult { State = ClientSyncResultState.Retry };

                    var newChangelist = ApplyClientChanges(clientData.ClientId, clientNode, clientChangesByFullName, presentHashes);

                    _context.SaveChanges();
                    transaction.Commit();

                    return new ClientSyncResult
                    {
                        State = ClientSyncResultState.Success,
                        LastChangelistId = lastChangelistId,
                        Changes = mergedNode.ToItems(),
                        NewChangelistId = newChangelist.Id
                    };
                }
            }
            catch (Exception ex)
            {
                return new ClientSyncResult
                {
                    State = ClientSyncResultState.Error,
                    Exception = ex
                };
            }
        }

        private void RenameAndDeleteConflictingFile(Folder parentFolder, File file, string reason)
        {
            string newDisplayName = file.DisplayName + " (" + reason + " " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff") + ")";
            string newName = newDisplayName.ToString();

            if (parentFolder.Files.AsQueryable().Where(f => f.Name == newName).SingleOrDefault() != null)
            {
                // Our desired name conflicts with an existing file. Start generating random names.

                do
                {
                    newDisplayName = file.DisplayName + " (" + reason + " " + Utilities.GenerateRandomString(IdChars, 16) + ")";
                    newName = newDisplayName.ToString();
                } while (parentFolder.Files.AsQueryable().Where(f => f.Name == newName).SingleOrDefault() != null);
            }

            file.Name = newName;
            file.DisplayName = newDisplayName;
            file.State = ObjectState.Deleted;
        }

        private void SetFileState(File file, ObjectState state)
        {
            file.State = state;

            if (file is Folder)
            {
                foreach (var subFile in ((Folder)file).Files)
                    SetFileState(subFile, state);
            }
        }

        private Changelist ApplyClientChanges(
            long clientId,
            ChangeNode clientNode,
            IDictionary<string, ClientChange> clientChangesByFullName,
            IDictionary<string, Blob> presentHashes
            )
        {
            Dictionary<string, File> fileCache = new Dictionary<string, File>();
            Queue<ChangeNode> queue = new Queue<ChangeNode>();

            fileCache.Add(clientNode.FullName, GetRootFolder());

            foreach (var node in clientNode.Nodes.Values)
                queue.Enqueue(node);

            Changelist changelist = new Changelist
            {
                ClientId = clientId,
                TimeStamp = DateTime.UtcNow
            };

            while (queue.Count != 0)
            {
                var node = queue.Dequeue();

                // There is no need to process files that haven't changed.
                if (node.Type == ChangeType.None && !node.IsFolder)
                    continue;

                // Find the associated database file object.
                var parentFolder = (Folder)fileCache[node.Parent.FullName];
                var file =
                    parentFolder.Files.AsQueryable()
                    .Where(f => f.Name == node.Name)
                    .FirstOrDefault();
                bool createChange = false;

                switch (node.Type)
                {
                    case ChangeType.Add:
                        bool createFolder = false;
                        bool createDocument = false;
                        bool createDocumentVersion = false;
                        bool setDisplayName = false;

                        if (file == null)
                        {
                            if (node.IsFolder)
                            {
                                // Nothing -> Folder
                                // Create the folder.
                                createFolder = true;
                            }
                            else
                            {
                                // Nothing -> Document
                                // Create the document and the first version.
                                createDocument = true;
                            }
                        }
                        else if (file is Folder)
                        {
                            if (node.IsFolder)
                            {
                                // Folder -> Folder
                                // Only a possible rename is needed.
                                setDisplayName = true;
                            }
                            else
                            {
                                // Folder -> Document
                                // The folder is implicitly being deleted.

                                RenameAndDeleteConflictingFile(parentFolder, file, "Deleted");
                                createDocument = true;
                            }
                        }
                        else if (file is Document)
                        {
                            if (node.IsFolder)
                            {
                                // Document -> Folder
                                // The document is implicitly being deleted.

                                RenameAndDeleteConflictingFile(parentFolder, file, "Deleted");
                                createFolder = true;
                            }
                            else
                            {
                                // Document -> Document
                                // Add a version.
                                createDocumentVersion = true;
                                setDisplayName = true;
                            }
                        }

                        // Apply the required changes.

                        if (file != null)
                        {
                            // Undelete the file if it is deleted.

                            if (file.State != ObjectState.Normal)
                            {
                                file.State = ObjectState.Normal;
                                createChange = true;
                            }
                        }

                        if (createFolder)
                        {
                            file = _context.Folders.Add(new Folder
                            {
                                Name = node.Name,
                                DisplayName = clientChangesByFullName[node.FullName].DisplayName ?? node.Name,
                                ParentFolder = parentFolder,
                                Owner = parentFolder.Owner
                            });
                            createChange = true;
                        }
                        else if (createDocument)
                        {
                            file = _context.Documents.Add(new Document
                            {
                                Name = node.Name,
                                DisplayName = clientChangesByFullName[node.FullName].DisplayName,
                                ParentFolder = parentFolder
                            });
                            createDocumentVersion = true;
                            createChange = true;
                        }

                        if (createDocumentVersion)
                        {
                            string hash = clientChangesByFullName[node.FullName].Hash.ToUpperInvariant();
                            bool identicalVersion = false;

                            if (!createDocument)
                            {
                                string lastestHash = (
                                    from version in ((Document)file).DocumentVersions.AsQueryable()
                                    orderby version.TimeStamp descending
                                    select version.Blob.Hash
                                    ).First();

                                if (hash == lastestHash)
                                    identicalVersion = true;
                            }

                            if (!identicalVersion)
                            {
                                _context.DocumentVersions.Add(new DocumentVersion
                                {
                                    TimeStamp = DateTime.UtcNow,
                                    ClientId = clientId,
                                    Document = (Document)file,
                                    Blob = presentHashes[hash]
                                });
                                createChange = true;
                            }
                        }

                        if (setDisplayName && !string.IsNullOrEmpty(clientChangesByFullName[node.FullName].DisplayName))
                        {
                            if (file.DisplayName != clientChangesByFullName[node.FullName].DisplayName)
                            {
                                file.DisplayName = clientChangesByFullName[node.FullName].DisplayName;
                                createChange = true;
                            }
                        }

                        break;

                    case ChangeType.SetDisplayName:
                        if (file != null && !string.IsNullOrEmpty(clientChangesByFullName[node.FullName].DisplayName))
                        {
                            if (file.DisplayName != clientChangesByFullName[node.FullName].DisplayName)
                            {
                                file.DisplayName = clientChangesByFullName[node.FullName].DisplayName;
                                createChange = true;
                            }
                        }

                        break;

                    case ChangeType.Delete:
                        if (file != null && file.State != ObjectState.Deleted)
                        {
                            SetFileState(file, ObjectState.Deleted);
                            createChange = true;
                        }
                        break;

                    case ChangeType.Undelete:
                        if (file != null && file.State != ObjectState.Normal)
                        {
                            SetFileState(file, ObjectState.Normal);
                            createChange = true;
                        }
                        break;
                }

                if (createChange)
                {
                    // Create the associated change object.
                    changelist.Changes.Add(new Change
                    {
                        Type = node.Type,
                        FullName = node.FullName,
                        IsFolder = node.IsFolder
                    });
                }

                if (file != null)
                {
                    fileCache.Add(node.FullName, file);

                    if (node.Nodes != null)
                    {
                        foreach (var subNode in node.Nodes.Values)
                            queue.Enqueue(subNode);
                    }
                }
            }

            if (changelist.Changes.Count != 0)
                _context.Changelists.Add(changelist);

            return changelist;
        }

        #endregion
    }

    public class ClientChange : ChangeItem
    {
        public long Size { get; set; }
        public string Hash { get; set; }
        public string DisplayName { get; set; }
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
        Retry,
        Error,
        Conflict,
        UploadRequired,
        Success
    }

    public class ClientSyncResult
    {
        public ClientSyncResultState State { get; set; }

        // >= Error

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