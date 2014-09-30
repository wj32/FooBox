using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace FooBox.Models
{
    public class FileManager : IDisposable
    {
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
                    Tag = "Root",
                    Owner = userManager.GetDefaultUser()
                });

                _context.SaveChanges();
            }
        }

        #endregion

        #region Blobs

        private static string _blobDataDirectory = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Blobs");
        private static object _blobLock = new object();

        private DirectoryInfo AccessBlobDataDirectory()
        {
            if (!Directory.Exists(_blobDataDirectory))
                return Directory.CreateDirectory(_blobDataDirectory);

            return new DirectoryInfo(_blobDataDirectory);
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
            return _blobDataDirectory + "\\" + blobId;
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
            return (from folder in _context.Folders where folder.Tag == "Root" select folder).Single();
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

        public Client CreateClient(long userId, string name)
        {
            var client = _context.Clients.Add(new Client
            {
                Name = name,
                UserId = userId,
                Secret = GenerateClientSecret()
            });
            _context.SaveChanges();

            return client;
        }

        public Client FindClient(long clientId)
        {
            return (from client in _context.Clients where client.Id == clientId select client).SingleOrDefault();
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

        #region Synchronization

        #endregion
    }
}