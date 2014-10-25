using FooBox.Common;
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
            return Utilities.GenerateRandomString(Utilities.IdChars, Blob.KeyLength);
        }

        public string GetBlobFileName(string blobKey)
        {
            return BlobDataDirectory + "\\" + blobKey;
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

        public File FindFile(string relativeFullName)
        {
            string fullDisplayName;
            return FindFile(relativeFullName, null, out fullDisplayName);
        }

        public File FindFile(string relativeFullName, Folder root, out string relativeFullDisplayName, bool followInvitations = true)
        {
            string syncFullName;
            return FindFile(relativeFullName, root, out relativeFullDisplayName, out syncFullName, followInvitations);
        }

        public File FindFile(
            string relativeFullName,
            Folder root,
            out string relativeFullDisplayName,
            out string syncFullName,
            bool followInvitations = true,
            bool followEndInvitation = false)
        {
            File file = root ?? GetRootFolder();
            string[] names = relativeFullName.ToUpperInvariant().Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder relativeFullDisplayNameSb = new StringBuilder();
            StringBuilder syncFullNameSb = new StringBuilder();

            relativeFullDisplayName = null;
            syncFullName = null;

            syncFullNameSb.Append(GetFullName(root));

            foreach (var name in names)
            {
                if (!(file is Folder))
                    return null;

                Folder folder = (Folder)file;

                if (followInvitations && folder.InvitationId != null)
                {
                    long invitationId = folder.InvitationId.Value;
                    folder = folder.Invitation.Target;
                    syncFullNameSb.Clear();
                    syncFullNameSb.Append("/@");
                    syncFullNameSb.Append(invitationId);
                }

                file = folder.Files.AsQueryable().Where(subFile => subFile.Name == name).SingleOrDefault();

                if (file == null)
                    return null;

                relativeFullDisplayNameSb.Append('/');
                relativeFullDisplayNameSb.Append(file.DisplayName);

                syncFullNameSb.Append('/');
                syncFullNameSb.Append(file.Name);
            }

            if (file is Folder && followEndInvitation && ((Folder)file).InvitationId != null)
            {
                long invitationId = ((Folder)file).InvitationId.Value;
                file = ((Folder)file).Invitation.Target;
                syncFullNameSb.Clear();
                syncFullNameSb.Append("/@");
                syncFullNameSb.Append(invitationId);
            }

            relativeFullDisplayName = relativeFullDisplayNameSb.ToString();
            syncFullName = syncFullNameSb.ToString();

            return file;
        }

        public DocumentVersion GetLastDocumentVersion(Document document)
        {
            return (
                from version in document.DocumentVersions.AsQueryable()
                orderby version.TimeStamp descending
                select version
                ).FirstOrDefault();
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

        public DocumentVersion FindDocumentVersion(long documentVersionId)
        {
            return (from version in _context.DocumentVersions where version.Id == documentVersionId select version).SingleOrDefault();
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

        public void RecalculateQuotaCharge(User user)
        {
            user.QuotaCharged = (
                from document in _context.Documents
                where document.ParentFolder.OwnerId == user.Id && document.State == ObjectState.Normal
                select (
                    from version in document.DocumentVersions.AsQueryable()
                    orderby version.TimeStamp descending
                    select version.Blob.Size
                    ).FirstOrDefault()
                ).Sum();
        }

        #endregion

        #region Clients

        private string GenerateClientSecret()
        {
            return Utilities.GenerateRandomString(Utilities.IdChars, 128);
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

        #region Document links

        public Document FindDocumentFromKey(string key)
        {
            string fullDisplayName;
            var pair = (from doc in _context.DocumentLinks where doc.Key == key select new {doc.RelativeFullName, doc.User}).SingleOrDefault();
            if (pair != null)
            {
                File file = FindFile(pair.RelativeFullName, pair.User.RootFolder, out fullDisplayName);
                if (file is Document)
                {
                    return (Document)file;
                }
            }
            return null;
        }

        public string CreateShareLink(string relativeFullName, User user)
        {
            string fullDisplayName;

            // Check if the file exists.
            if (FindFile(relativeFullName, user.RootFolder, out fullDisplayName) == null)
                return null;

            string key = _context.DocumentLinks.Where(x => x.RelativeFullName == fullDisplayName).Select(x => x.Key).FirstOrDefault();

            if (key == null)
            {
                key = Utilities.GenerateRandomString(Utilities.LetterDigitChars, 8);
                var dl = new DocumentLink
                {
                    Key = key,
                    RelativeFullName = fullDisplayName,
                    User = user
                };

                try
                {
                    _context.DocumentLinks.Add(dl);
                    _context.SaveChanges();
                }
                catch
                {
                    return null;
                }
            }

            return key;
        }

        #endregion

        #region Invitations

        public Invitation FindInvitation(long id)
        {
            return (from invitation in _context.Invitations where invitation.Id == id select invitation).SingleOrDefault();
        }

        public Invitation GetInvitationForUser(Folder folder, long userId)
        {
            return (
                from invitation in folder.TargetOfInvitations.AsQueryable()
                where invitation.UserId == userId
                select invitation
                ).SingleOrDefault();
        }

        public bool IsInSharedFolder(File file)
        {
            Folder folder;

            if (file is Folder)
                folder = (Folder)file;
            else
                folder = file.ParentFolder;

            while (folder != null && folder.Name.Length != 0)
            {
                if (folder.InvitationId != null)
                    return true;
                if (folder.TargetOfInvitations.Any())
                    return true;

                folder = folder.ParentFolder;
            }

            return false;
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

        public void CleanClientUploadDirectory(long clientId)
        {
            DirectoryInfo directory = AccessClientUploadDirectory(clientId);

            foreach (var file in directory.EnumerateFiles())
            {
                try
                {
                    System.IO.File.Delete(file.FullName);
                }
                catch
                { }
            }
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
                var client = FindClient(clientData.ClientId);

                if (clientData.BaseChangelistId == 0)
                {
                    long changelistId = GetLastChangelistId();

                    return new ClientSyncResult
                    {
                        State = ClientSyncResultState.Success,
                        LastChangelistId = changelistId,
                        Changes = GetChangesForFolder(client.User.RootFolder),
                        NewChangelistId = changelistId
                    };
                }

                var translatedChanges = TranslateClientChangesIn(clientData.Changes, client);
                var clientNode = ChangeNode.FromItems(translatedChanges);
                var clientChangesByFullName = translatedChanges.ToDictionary(change => Utilities.NormalizeFullName(change.FullName).ToUpperInvariant());

                // Verify that the names are valid and the display names match the names.
                // Also create missing entries in clientChangesByFullName.
                foreach (var node in clientNode.RecursiveEnumerate())
                {
                    if (!string.IsNullOrEmpty(node.Name) && !Utilities.ValidateFileName(node.Name))
                        throw new Exception("Name '" + node.Name + "' is invalid");

                    if (node.Type == ChangeType.Add || node.Type == ChangeType.SetDisplayName || node.Type == ChangeType.Undelete)
                    {
                        if (clientChangesByFullName.ContainsKey(node.FullName))
                        {
                            string displayName = clientChangesByFullName[node.FullName].DisplayName;

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
                    select new { ChangelistId = changelist.Id, Changes = changes };
                var intermediateNodes =
                    from x in intermediateChanges.AsEnumerable()
                    select new
                    {
                        ChangelistId = x.ChangelistId,
                        Nodes = ChangeNode.FromItems(from change in x.Changes
                                                        select new ChangeItem
                                                            {
                                                                FullName = change.FullName,
                                                                Type = change.Type,
                                                                IsFolder = change.IsFolder
                                                            })
                    };

                var mergedNode = ChangeNode.CreateRoot();
                bool nextChangelistFound = false;
                long lastChangelistId = clientData.BaseChangelistId;

                foreach (var changelist in intermediateNodes)
                {
                    mergedNode.SequentialMerge(changelist.Nodes);
                    lastChangelistId = changelist.ChangelistId;

                    if (changelist.ChangelistId == clientData.BaseChangelistId + 1)
                        nextChangelistFound = true;
                }

                if (lastChangelistId != clientData.BaseChangelistId && !nextChangelistFound)
                    return new ClientSyncResult { State = ClientSyncResultState.TooOld };

                if (translatedChanges.Count == 0)
                {
                    return new ClientSyncResult
                    {
                        State = ClientSyncResultState.Success,
                        LastChangelistId = lastChangelistId,
                        Changes = GetChangesForNode(mergedNode, client),
                        NewChangelistId = lastChangelistId
                    };
                }

                // Check if the client's changes conflict with ours.

                if (mergedNode.PreservingConflicts(clientNode))
                {
                    return new ClientSyncResult
                    {
                        State = ClientSyncResultState.Conflict,
                        LastChangelistId = lastChangelistId,
                        Changes = GetChangesForNode(mergedNode, client),
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
                        Changes = GetChangesForNode(mergedNode, client),
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
                    // Lock the tables.
                    _context.Database.LockTableExclusive("dbo.Changelists");
                    _context.Database.LockTableExclusive("dbo.Files");

                    // Make sure no one changed anything since we started computing changes.
                    var moreChangelists = (from changelist in _context.Changelists
                                           where changelist.Id > lastChangelistId
                                           select changelist.Id).Any();

                    if (moreChangelists)
                        return new ClientSyncResult { State = ClientSyncResultState.Retry };

                    var newChangelist = ApplyClientChanges(client, clientNode, clientChangesByFullName, presentHashes);

                    _context.SaveChanges();
                    transaction.Commit();

                    return new ClientSyncResult
                    {
                        State = ClientSyncResultState.Success,
                        LastChangelistId = lastChangelistId,
                        Changes = GetChangesForNode(mergedNode, client),
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

        private void RenameAndDeleteConflictingFile(Folder parentFolder, File file, string reason, Dictionary<User, long> quotaCharge)
        {
            string newDisplayName = file.DisplayName + " (" + reason + " " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff") + ")";
            string newName = newDisplayName.ToUpperInvariant();

            if (parentFolder.Files.AsQueryable().Where(f => f.Name == newName).SingleOrDefault() != null)
            {
                // Our desired name conflicts with an existing file. Start generating random names.

                do
                {
                    newDisplayName = file.DisplayName + " (" + reason + " " + Utilities.GenerateRandomString(Utilities.IdChars, 16) + ")";
                    newName = newDisplayName.ToUpperInvariant();
                } while (parentFolder.Files.AsQueryable().Where(f => f.Name == newName).SingleOrDefault() != null);
            }

            file.Name = newName;
            file.DisplayName = newDisplayName;
            SetFileState(file, ObjectState.Deleted, quotaCharge);
        }

        private void AddQuotaCharge(IDictionary<User, long> quotaCharge, User user, long charge)
        {
            if (!quotaCharge.ContainsKey(user))
                quotaCharge.Add(user, 0);

            quotaCharge[user] += charge;
        }

        private void SetFileState(File file, ObjectState state, IDictionary<User, long> quotaCharge)
        {
            if (file is Document)
            {
                if (file.State != state)
                {
                    long size = GetLastDocumentVersion((Document)file).Blob.Size;

                    if (state == ObjectState.Deleted)
                        size = -size;

                    AddQuotaCharge(quotaCharge, file.ParentFolder.Owner, size);
                }
            }
            else if (file is Folder)
            {
                var folder = (Folder)file;

                if (state == ObjectState.Deleted)
                {
                    // Delete all invitations that target this folder.
                    _context.Invitations.RemoveRange(folder.TargetOfInvitations);

                    // Remove any linked invitation (but don't delete it).
                    folder.InvitationId = null;
                }

                foreach (var subFile in folder.Files)
                    SetFileState(subFile, state, quotaCharge);
            }

            file.State = state;
        }

        private Changelist ApplyClientChanges(
            Client client,
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
                ClientId = client.Id,
                TimeStamp = DateTime.UtcNow
            };
            var quotaCharge = new Dictionary<User, long>();

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
                        bool setInvitation = false;
                        bool setDisplayName = false;
                        bool undeleted = false;
                        bool replaced = false;

                        if (file == null)
                        {
                            if (node.IsFolder)
                            {
                                // Nothing -> Folder
                                // Create the folder.
                                createFolder = true;
                                setInvitation = true;
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
                                // Only a possible rename or invitation change is needed.
                                setInvitation = true;
                                setDisplayName = true;
                            }
                            else
                            {
                                // Folder -> Document
                                // The folder is implicitly being deleted.

                                RenameAndDeleteConflictingFile(parentFolder, file, "Deleted", quotaCharge);
                                replaced = true;
                                createDocument = true;
                            }
                        }
                        else if (file is Document)
                        {
                            if (node.IsFolder)
                            {
                                // Document -> Folder
                                // The document is implicitly being deleted.

                                RenameAndDeleteConflictingFile(parentFolder, file, "Deleted", quotaCharge);
                                replaced = true;
                                createFolder = true;
                                setInvitation = true;
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

                        if (file != null && !replaced)
                        {
                            // Undelete the file if it is deleted.

                            if (file.State != ObjectState.Normal)
                            {
                                file.State = ObjectState.Normal;
                                undeleted = true;
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
                                DisplayName = clientChangesByFullName[node.FullName].DisplayName ?? node.Name,
                                ParentFolder = parentFolder
                            });
                            createDocumentVersion = true;
                            createChange = true;
                        }

                        if (createDocumentVersion)
                        {
                            string hash = clientChangesByFullName[node.FullName].Hash.ToUpperInvariant();
                            bool identicalVersion = false;
                            long latestSize = 0;

                            if (!createDocument)
                            {
                                var latestBlob = GetLastDocumentVersion((Document)file).Blob;

                                latestSize = latestBlob.Size;

                                if (hash == latestBlob.Hash)
                                    identicalVersion = true;
                            }

                            if (!identicalVersion)
                            {
                                var blob = presentHashes[hash];

                                _context.DocumentVersions.Add(new DocumentVersion
                                {
                                    TimeStamp = DateTime.UtcNow,
                                    ClientId = client.Id,
                                    Document = (Document)file,
                                    Blob = blob
                                });
                                createChange = true;

                                AddQuotaCharge(quotaCharge, file.ParentFolder.Owner, (undeleted ? 0 : -latestSize) + blob.Size);
                            }
                            else if (undeleted)
                            {
                                AddQuotaCharge(quotaCharge, file.ParentFolder.Owner, latestSize);
                            }
                        }

                        if (setInvitation)
                        {
                            long? invitationId = clientChangesByFullName[node.FullName].InvitationId;

                            if (invitationId != null)
                            {
                                if (invitationId.Value != 0 && ((Folder)file).OwnerId == client.UserId)
                                {
                                    var invitation = (
                                        from i in client.User.Invitations.AsQueryable()
                                        where i.Id == invitationId.Value
                                        select i
                                        ).SingleOrDefault();

                                    if (invitation != null)
                                    {
                                        // Remove all other folders that link to this invitation.
                                        invitation.AcceptedFolders.Clear();
                                        invitation.AcceptedFolders.Add((Folder)file);
                                    }
                                }
                                else
                                {
                                    ((Folder)file).InvitationId = null;
                                }
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
                            SetFileState(file, ObjectState.Deleted, quotaCharge);
                            createChange = true;
                        }
                        break;

                    case ChangeType.Undelete:
                        if (file != null && file is Document && file.State != ObjectState.Normal)
                        {
                            SetFileState(file, ObjectState.Normal, quotaCharge);
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

            // Apply quotas.
            foreach (var pair in quotaCharge)
            {
                if (pair.Key.QuotaCharged + pair.Value > pair.Key.QuotaLimit)
                    throw new Exception("Quota exceeded for user '" + pair.Key.Name + "'");

                pair.Key.QuotaCharged += pair.Value;
            }

            if (changelist.Changes.Count != 0)
                _context.Changelists.Add(changelist);

            return changelist;
        }

        private void AddChangesForFolder(ICollection<ClientChange> changes, Folder folder, string fullName)
        {
            foreach (File file in folder.Files.AsQueryable().Where(f => f.State == ObjectState.Normal))
            {
                string newFullName = fullName + "/" + file.Name;
                long size = 0;
                string hash = "";

                if (file is Document)
                {
                    var blob = GetLastDocumentVersion((Document)file).Blob;
                    size = blob.Size;
                    hash = blob.Hash;
                }

                changes.Add(new ClientChange
                {
                    FullName = newFullName,
                    Type = ChangeType.Add,
                    IsFolder = file is Folder,
                    Size = size,
                    Hash = hash,
                    DisplayName = file.DisplayName,
                    InvitationId = (file is Folder) ? (((Folder)file).InvitationId ?? 0) : 0
                });

                if (file is Folder)
                {
                    var fileFolder = (Folder)file;

                    if (fileFolder.InvitationId.HasValue)
                        AddChangesForFolder(changes, fileFolder.Invitation.Target, "/@" + fileFolder.InvitationId.ToString());
                    else
                        AddChangesForFolder(changes, fileFolder, newFullName);
                }
            }
        }

        private List<ClientChange> GetChangesForFolder(Folder folder)
        {
            var changes = new List<ClientChange>();
            AddChangesForFolder(changes, folder, GetFullName(folder));
            return changes;
        }

        private List<ClientChange> GetChangesForNode(ChangeNode rootNode, Client client)
        {
            var changes = new List<ClientChange>();
            Dictionary<string, File> fileCache = new Dictionary<string, File>();
            Queue<ChangeNode> queue = new Queue<ChangeNode>();

            fileCache.Add(rootNode.FullName, GetRootFolder());

            foreach (var node in rootNode.Nodes.Values)
                queue.Enqueue(node);

            while (queue.Count != 0)
            {
                var node = queue.Dequeue();

                // There is no need to process files that haven't changed.
                if (node.Type == ChangeType.None && !node.IsFolder)
                    continue;

                var parentFolder = (Folder)fileCache[node.Parent.FullName];
                var file =
                    parentFolder.Files.AsQueryable()
                    .Where(f => f.Name == node.Name)
                    .FirstOrDefault();

                long size = 0;
                string hash = "";
                string displayName = null;
                long invitationId = 0;

                if (file != null)
                {
                    if (file is Document)
                    {
                        var blob = GetLastDocumentVersion((Document)file).Blob;
                        size = blob.Size;
                        hash = blob.Hash;
                    }
                    else if (file is Folder)
                    {
                        invitationId = ((Folder)file).InvitationId ?? 0;
                    }

                    displayName = file.DisplayName;

                    fileCache.Add(node.FullName, file);

                    if (node.Nodes != null)
                    {
                        foreach (var subNode in node.Nodes.Values)
                            queue.Enqueue(subNode);
                    }
                }

                changes.Add(new ClientChange
                {
                    FullName = node.FullName,
                    Type = node.Type,
                    IsFolder = node.IsFolder,
                    Size = size,
                    Hash = hash,
                    DisplayName = displayName ?? node.Name,
                    InvitationId = invitationId
                });
            }

            return TranslateClientChangesOut(changes, client);
        }

        private List<ClientChange> TranslateClientChangesIn(ICollection<ClientChange> changes, Client client)
        {
            var translated = new List<ClientChange>();
            var cache = new Dictionary<long, string>();
            string userPrefix = "/" + client.UserId.ToString() + "/";

            foreach (var change in changes)
            {
                if (change.Type == ChangeType.None)
                    continue;

                if (change.FullName.StartsWith(userPrefix))
                {
                    translated.Add(change);
                }
                else if (change.FullName.StartsWith("/@"))
                {
                    int indexOfSecondSlash = change.FullName.IndexOf('/', 2);

                    if (indexOfSecondSlash == -1)
                        continue;

                    long invitationId;

                    if (!long.TryParse(change.FullName.Substring(2, indexOfSecondSlash - 2), out invitationId))
                        continue;

                    string prefix = null;

                    if (!cache.TryGetValue(invitationId, out prefix))
                    {
                        var target = (
                            from i in client.User.Invitations.AsQueryable()
                            where i.Id == invitationId
                            select i.Target
                            ).SingleOrDefault();

                        if (target != null)
                            prefix = GetFullName(target);

                        cache.Add(invitationId, prefix);
                    }

                    if (prefix == null)
                        continue;

                    change.FullName = prefix + change.FullName.Substring(indexOfSecondSlash);
                    translated.Add(change);
                }
            }

            return translated;
        }

        private List<ClientChange> TranslateClientChangesOut(ICollection<ClientChange> changes, Client client)
        {
            var translated = new List<ClientChange>();
            string userPrefix = "/" + client.UserId.ToString() + "/";
            var map = (
                from invitation in client.User.Invitations.AsQueryable()
                where invitation.AcceptedFolders.Any()
                select new { Id = invitation.Id, Target = invitation.Target }
                ).AsEnumerable()
                .Select(x => Tuple.Create(GetFullName(x.Target) + "/", "/@" + x.Id.ToString() + "/"))
                .ToList();

            map.Add(Tuple.Create(userPrefix, userPrefix));

            foreach (var change in changes)
            {
                if (change.Type == ChangeType.None)
                    continue;

                foreach (var pair in map)
                {
                    if (change.FullName.StartsWith(pair.Item1))
                    {
                        if (pair.Item1 != pair.Item2)
                            change.FullName = pair.Item2 + change.FullName.Substring(pair.Item1.Length);
                        translated.Add(change);
                        break;
                    }
                }
            }

            return translated;
        }

        #endregion
    }
}
