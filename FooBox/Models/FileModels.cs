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

        public string GenerateBlobId()
        {
            Random r = new Random();
            char[] c = new char[32];

            for (int i = 0; i < c.Length; i++)
                c[i] = IdChars[r.Next(0, IdChars.Length)];

            return new string(c);
        }

        public string GetBlobFileName(string blobId)
        {
            return BlobDataDirectory + "\\" + blobId;
        }

        public string CreateBlob(string hash, long size)
        {
            var blob = _context.Blobs.Add(new Blob { Id = GenerateBlobId(), Size = size, Hash = hash });
            _context.SaveChanges();

            return blob.Id;
        }

        public string FindBlob(string hash)
        {
            return (from blob in _context.Blobs where blob.Hash == hash select blob.Id).SingleOrDefault();
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

        public static readonly string ClientUploadDataDirectory = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Uploads");

        #endregion

        #region Synchronization

        private class ClientChangeNode
        {
            public string Name;
            public ClientChange Change;
            public File File;

            public Dictionary<string, ClientChangeNode> Nodes;
        }

        private ClientChangeNode CreateChangeNodes(IEnumerable<ClientChange> changes, Client client)
        {
            ClientChangeNode root = new ClientChangeNode();
            root.File = client.User.RootFolder;
            root.Nodes = new Dictionary<string, ClientChangeNode>();

            foreach (var change in changes)
            {
                ClientChangeNode currentNode = root;

                foreach (var name in change.FileName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var nameKey = name.ToUpperInvariant();

                    if (currentNode.Nodes != null && currentNode.Nodes.ContainsKey(nameKey))
                    {
                        currentNode = currentNode.Nodes[nameKey];
                    }
                    else
                    {
                        if (currentNode.Nodes == null)
                            currentNode.Nodes = new Dictionary<string, ClientChangeNode>();

                        // TODO: Add support for links (Link). This will allow shared folders to work properly.

                        ClientChangeNode newNode = new ClientChangeNode
                        {
                            Name = nameKey,
                            File = (currentNode.File is Folder) ? ((Folder)currentNode.File).Files.AsQueryable().Where(file => file.Name == nameKey).SingleOrDefault() : null
                        };

                        currentNode.Nodes.Add(newNode.Name, newNode);
                        currentNode = newNode;
                    }
                }

                if (currentNode != root)
                    currentNode.Change = change;
            }

            return root;
        }

        public void SyncClientChanges(ClientChangelist clientChangelist)
        {
            var client = FindClient(clientChangelist.ClientId);

            if (client == null)
                throw new Exception("Invalid client.");
            if (client.Secret != clientChangelist.Secret)
                throw new Exception("Incorrect client secret.");

            using (var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            using (var userManager = new UserManager(_context))
            {
                var changeNodes = CreateChangeNodes(clientChangelist.Changes, client);
                var intermediateChanges =
                    from changelist in _context.Changelists
                    where changelist.Id > clientChangelist.BaseChangelistId
                    join change in _context.Changes on changelist.Id equals change.ChangelistId
                    join file in _context.Files on change.FileId equals file.Id
                    orderby changelist.Id
                    select new { Type = change.Type, FileId = change.FileId, IsFolder = file is Folder };
                var intermediateChangesByFile = intermediateChanges.ToDictionary(x => x.FileId);
            }
        }

        #endregion
    }

    public enum ClientChangeType
    {
        None,
        Add,
        Delete,
        ModifyDisplayName,
        AddVersion
    }

    public class ClientChange
    {
        public string FileName { get; set; }
        public bool IsFolder { get; set; }
        public ClientChangeType Type { get; set; }

        public long Size { get; set; }
        public string UploadFileName { get; set; }
        public FileStream UploadStream { get; set; }
    }

    public class ClientChangelist
    {
        public ClientChangelist()
        {
            this.Changes = new HashSet<ClientChange>();
        }

        public long ClientId { get; set; }
        public string Secret { get; set; }

        /// <summary>
        /// The changelist that the client is currently synchronized to.
        /// </summary>
        public long BaseChangelistId { get; set; }

        public ICollection<ClientChange> Changes { get; set; }
    }
}