using FooBox;
using FooBox.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace FooBoxClient
{
    public class SyncEngine
    {
        public const string SpecialFolderName = ".FooBox";
        public const string StateFileName = "state.json";
        public const string InvitationFileName = ".FooBoxInvitation";
        public const string InvitationFileNameFull = ".FOOBOXINVITATION";

        public class State
        {
            public long UserId { get; set; }

            public string ClientName { get; set; }

            /// <summary>
            /// The changelist ID that we are at. Any local changes that were made to reach this changelist ID
            /// must have already been synchronized with the server to check for conflicts and retrieve
            /// the latest set of remote changes.
            /// </summary>
            public long ChangelistId { get; set; }

            /// <summary>
            /// The file system corresponding to <see cref="ChangelistId"/>.
            /// </summary>
            public File Root { get; set; }

            /// <summary>
            /// The changes that have not yet been applied.
            /// </summary>
            public List<ICollection<ClientChange>> PendingChanges { get; set; }

            public Dictionary<string, string> Invitations { get; set; }

            public Dictionary<string, string> NewInvitations { get; set; }
        }

        public static long ReadInvitationId(string directoryName)
        {
            try
            {
                string invitationFileName = directoryName + "\\" + InvitationFileName;

                if (!System.IO.File.Exists(invitationFileName))
                    return 0;

                return long.Parse(System.IO.File.ReadAllText(invitationFileName).Trim());
            }
            catch
            {
                return 0;
            }
        }

        public static void WriteInvitationId(string directoryName, long invitationId)
        {
            string fileName = directoryName + "\\" + InvitationFileName;

            if (invitationId != 0)
            {
                System.IO.File.WriteAllText(fileName, invitationId.ToString());
            }
            else
            {
                if (System.IO.File.Exists(fileName))
                    Utilities.DeleteFile(fileName);
            }
        }

        private string _rootDirectory;
        private State _state;
        private bool _stateLoaded;
        private CancellationToken _cancellationToken;

        private string _localSpecialFolder;
        private string _stateFileName;
        private string _blobFolder;
        private string _specialFolderFullName;

        public SyncEngine(string rootDirectory, long userId)
        {
            this.RootDirectory = rootDirectory;
            this.ResetState(userId, Environment.MachineName);

            _specialFolderFullName = "/" + userId.ToString() + "/" + SpecialFolderName.ToUpperInvariant();
        }

        public string RootDirectory
        {
            get { return _rootDirectory; }
            set
            {
                _rootDirectory = value;
                _localSpecialFolder = _rootDirectory + "\\" + SpecialFolderName;
                _stateFileName = _localSpecialFolder + "\\" + StateFileName;
                _blobFolder = _localSpecialFolder + "\\Blobs";
            }
        }

        public long ChangelistId
        {
            get { return _state.ChangelistId; }
        }

        private void ResetState(long userId, string clientName)
        {
            _state = new State
            {
                UserId = userId,
                ClientName = clientName,
                ChangelistId = 0,
                Root = File.CreateRoot(),
                PendingChanges = new List<ICollection<ClientChange>>(),
                Invitations = new Dictionary<string, string>(),
                NewInvitations = new Dictionary<string, string>()
            };
            var userRoot = new File
            {
                FullName = "/" + userId.ToString(),
                Name = userId.ToString(),
                DisplayName = userId.ToString(),
                IsFolder = true,
                Files = new Dictionary<string, File>()
            };
            _state.Root.Files.Add(userRoot.Name, userRoot);
        }

        public void LoadState()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            _state = serializer.Deserialize<State>(System.IO.File.ReadAllText(_stateFileName));
        }

        public void SaveState()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string directoryName = Path.GetDirectoryName(_stateFileName);

            if (!Directory.Exists(directoryName))
            {
                var di = Directory.CreateDirectory(directoryName);
                di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }

            System.IO.File.WriteAllText(_stateFileName, serializer.Serialize(_state));
        }

        private DirectoryInfo AccessBlobDirectory()
        {
            if (!Directory.Exists(_blobFolder))
                return Directory.CreateDirectory(_blobFolder);

            return new DirectoryInfo(_blobFolder);
        }

        public void CleanBlobDirectory()
        {
            DirectoryInfo directory = AccessBlobDirectory();

            foreach (var file in directory.EnumerateFiles())
            {
                try
                {
                    Utilities.DeleteFile(file.FullName);
                }
                catch
                { }
            }
        }

        private void SalvageAndDeleteDocument(File file, string localFullName = null)
        {
            var blobDirectory = AccessBlobDirectory();

            if (localFullName == null)
                localFullName = GetLocalFullName(file.FullName);

            if (string.IsNullOrEmpty(file.Hash) || System.IO.File.Exists(blobDirectory.FullName + "\\" + file.Hash))
            {
                Utilities.DeleteFile(localFullName);
                return;
            }

            try
            {
                MoveFileOrDirectory(localFullName, blobDirectory.FullName + "\\" + file.Hash);
            }
            catch
            {
                Utilities.DeleteFile(localFullName);
            }
        }

        private void SalvageAndDeleteFolder(File file, string localFullName = null)
        {
            foreach (var subFile in file.RecursiveEnumerate())
            {
                if (!subFile.IsFolder)
                    SalvageAndDeleteDocument(subFile);
            }

            Utilities.DeleteDirectoryRecursive(localFullName ?? GetLocalFullName(file.FullName));
        }

        private string GetFirstComponent(string fullName)
        {
            if (fullName.Length == 0 || fullName[0] != '/')
                return "";

            int indexOfSlash = fullName.IndexOf('/', 1);

            if (indexOfSlash == -1)
                indexOfSlash = fullName.Length;

            return fullName.Substring(1, indexOfSlash - 1);
        }

        public string GetLocalFullName(string fullName)
        {
            string userPrefix = "/" + _state.UserId.ToString();

            if (fullName.StartsWith(userPrefix))
                return _rootDirectory + fullName.Remove(0, userPrefix.Length).Replace('/', '\\');

            var firstComponent = GetFirstComponent(fullName);
            string newPrefixFullName;

            if (firstComponent.Length != 0 && firstComponent[0] == '@' &&
                _state.Invitations.TryGetValue(firstComponent.Substring(1), out newPrefixFullName))
            {
                return GetLocalFullName(newPrefixFullName) + fullName.Remove(0, 1 + firstComponent.Length).Remove('/', '\\');
            }

            throw new Exception("Invalid file name '" + fullName + "'");
        }

        public void IgnoreSpecialNames(List<ClientChange> changes)
        {
            changes.RemoveAll(change =>
                change.FullName == _specialFolderFullName ||
                change.FullName.StartsWith(_specialFolderFullName + "/") ||
                change.FullName.EndsWith("/" + InvitationFileNameFull)
                );
        }

        private void FilterTopLevelNames(List<ClientChange> changes, ISet<string> prefixes)
        {
            changes.RemoveAll(change => !prefixes.Contains(GetFirstComponent(change.FullName)));
        }

        public List<ClientChange> Compare(File newFolder)
        {
            return Compare(_state.Root, newFolder);
        }

        private List<ClientChange> Compare(File oldFolder, File newFolder)
        {
            var changes = new List<ClientChange>();
            Compare(changes, oldFolder, newFolder);
            return changes;
        }

        /// <summary>
        /// Compares two folders.
        /// Also updates the hashes in the new folder.
        /// </summary>
        private void Compare(List<ClientChange> changes, File oldFolder, File newFolder)
        {
            if (oldFolder != null)
            {
                if (oldFolder.FullName != newFolder.FullName)
                    throw new Exception("Compare called on different folders.");

                // Detect deleted files
                if (oldFolder.Files != null)
                {
                    foreach (File oldFile in oldFolder.Files.Values)
                    {
                        if (oldFile.InvitationId != 0)
                        {
                            _state.Invitations.Remove(oldFile.InvitationId.ToString());
                            _state.NewInvitations.Remove(oldFile.InvitationId.ToString());
                        }

                        if (newFolder.Files == null || !newFolder.Files.ContainsKey(oldFile.Name))
                        {
                            changes.Add(new ClientChange
                            {
                                FullName = oldFile.FullName,
                                Type = ChangeType.Delete
                            });
                        }
                    }
                }
            }

            if (newFolder.Files == null)
                return;

            // Detect added and modified files
            foreach (File newFile in newFolder.Files.Values)
            {
                File oldFile = null;
                bool add = false;

                if (oldFolder != null && oldFolder.Files != null &&
                    oldFolder.Files.TryGetValue(newFile.Name, out oldFile))
                {
                    if (newFile.Hash == null)
                        newFile.Hash = oldFile.Hash;

                    if (newFile.IsFolder != oldFile.IsFolder || (!newFile.IsFolder && (
                        newFile.LastWriteTimeUtc != oldFile.LastWriteTimeUtc ||
                        newFile.Size != oldFile.Size)))
                    {
                        add = true;
                    }
                }
                else
                {
                    add = true;
                }

                if (add)
                {
                    string newHash = "";

                    if (newFile.IsFolder)
                    {
                        if (newFile.InvitationId != 0)
                        {
                            if (_state.Invitations.ContainsKey(newFile.InvitationId.ToString()) ||
                                _state.NewInvitations.ContainsKey(newFile.InvitationId.ToString()))
                            {
                                // The user has probably copied a folder with an invitation. Delete
                                // this duplicate invitation.
                                WriteInvitationId(newFile.SourceLocalFileName, 0);
                                newFile.InvitationId = 0;
                            }
                            else
                            {
                                _state.NewInvitations[newFile.InvitationId.ToString()] = newFile.FullName;
                            }
                        }
                    }
                    else
                    {
                        newHash = Utilities.ComputeSha256Hash(newFile.SourceLocalFileName);
                        newFile.Hash = newHash;

                        if (oldFile != null && !oldFile.IsFolder && oldFile.Hash == newHash)
                            add = false; // The file hasn't really changed.
                    }

                    if (add)
                    {
                        changes.Add(new ClientChange
                        {
                            FullName = newFile.FullName,
                            Type = ChangeType.Add,
                            IsFolder = newFile.IsFolder,
                            DisplayName = newFile.DisplayName,
                            Size = newFile.Size,
                            Hash = newHash,
                            InvitationId = newFile.InvitationId
                        });
                    }
                }

                if (oldFile != null && newFile != null && !add)
                {
                    if (newFile.DisplayName != oldFile.DisplayName)
                    {
                        changes.Add(new ClientChange
                        {
                            FullName = newFile.FullName,
                            Type = ChangeType.SetDisplayName,
                            DisplayName = newFile.DisplayName
                        });
                    }
                }

                if (newFile.IsFolder)
                    this.Compare(changes, (oldFile != null && oldFile.IsFolder) ? oldFile : null, newFile);
            }
        }

        public void Apply(ICollection<ClientChange> changes)
        {
            var clientChanges = changes.ToDictionary(change => change.FullName);
            var rootNode = ChangeNode.FromItems(changes);

            if (rootNode.Nodes == null || rootNode.Nodes.Count == 0)
            {
                // This shouldn't happen.
                changes.Clear();
                return;
            }

            // Fix up first-level nodes.
            foreach (var node in rootNode.Nodes.Values)
                node.Type = ChangeType.None;

            Apply(_state.Root, rootNode, clientChanges);

            if (rootNode.Nodes.Count == 0)
            {
                // Signal that there are no more changes to apply.
                changes.Clear();
            }
        }

        private void Apply(File rootFolder, ChangeNode rootNode, Dictionary<string, ClientChange> clientChanges)
        {
            if (rootNode.Nodes == null)
                return;

            foreach (ChangeNode node in rootNode.Nodes.Values.ToList())
            {
                File file = null;

                if (rootFolder.Files != null)
                    rootFolder.Files.TryGetValue(node.Name, out file);

                switch (node.Type)
                {
                    case ChangeType.None:
                        {
                            // Process files in the folder.
                            if (node.Nodes != null && node.Nodes.Count != 0)
                                Apply(file, node, clientChanges);

                            // State management

                            if (node.Nodes == null || node.Nodes.Count == 0)
                            {
                                clientChanges.Remove(node.FullName); // If it exists
                                node.Parent.Nodes.Remove(node.Name);
                            }

                            if (_cancellationToken.IsCancellationRequested)
                                return;
                        }
                        break;
                    case ChangeType.Add:
                    case ChangeType.Undelete:
                        {
                            string newDisplayName = clientChanges[node.FullName].DisplayName;
                            string newFullDisplayName = GetLocalFullName(node.Parent.FullName) + "\\" + newDisplayName;

                            if (rootFolder.Files == null)
                                rootFolder.Files = new Dictionary<string, File>();

                            if (node.IsFolder)
                            {
                                // Add folder

                                if (file != null && !file.IsFolder)
                                {
                                    SalvageAndDeleteDocument(file, newFullDisplayName);
                                    file = null;
                                }

                                if (!System.IO.Directory.Exists(newFullDisplayName))
                                    System.IO.Directory.CreateDirectory(newFullDisplayName);
                                else if (file != null && file.DisplayName != newDisplayName)
                                    MoveFileOrDirectory(newFullDisplayName, newFullDisplayName);

                                long invitationId = clientChanges[node.FullName].InvitationId;
                                WriteInvitationId(newFullDisplayName, invitationId);

                                if (invitationId != 0 && (file == null || file.InvitationId != invitationId))
                                    _state.NewInvitations[invitationId.ToString()] = node.FullName;

                                if (file == null)
                                {
                                    file = new File
                                    {
                                        FullName = node.FullName,
                                        Name = node.Name,
                                        DisplayName = newDisplayName,
                                        IsFolder = true
                                    };
                                    rootFolder.Files[file.Name] = file;
                                }
                                else
                                {
                                    file.DisplayName = newDisplayName;
                                }

                                // Process files in the folder.
                                if (node.Nodes != null && node.Nodes.Count != 0)
                                    Apply(file, node, clientChanges);

                                // State management

                                if (node.Nodes == null || node.Nodes.Count == 0)
                                {
                                    clientChanges.Remove(node.FullName);
                                    node.Parent.Nodes.Remove(node.Name);
                                }

                                if (_cancellationToken.IsCancellationRequested)
                                    return;
                            }
                            else
                            {
                                // Add document

                                if (file != null && file.IsFolder)
                                {
                                    SalvageAndDeleteFolder(file, newFullDisplayName);
                                    rootFolder.Files.Remove(file.Name);
                                    file = null;
                                }

                                if (System.IO.File.Exists(newFullDisplayName) &&
                                    file != null &&
                                    file.DisplayName != newDisplayName)
                                {
                                    MoveFileOrDirectory(newFullDisplayName, newFullDisplayName);
                                }

                                string hash = clientChanges[node.FullName].Hash;
                                bool valid = true;

                                if (file == null || file.Hash != hash || !System.IO.File.Exists(newFullDisplayName))
                                {
                                    valid = DownloadDocument(hash, newFullDisplayName);
                                }

                                if (valid)
                                {
                                    FileInfo info = new FileInfo(newFullDisplayName);

                                    file = new File
                                    {
                                        FullName = node.FullName,
                                        Name = node.Name,
                                        DisplayName = newDisplayName,
                                        IsFolder = false,
                                        Size = info.Length,
                                        Hash = hash,
                                        LastWriteTimeUtc = info.LastWriteTimeUtc
                                    };
                                    rootFolder.Files[file.Name] = file;
                                }

                                // State management

                                clientChanges.Remove(node.FullName);
                                node.Parent.Nodes.Remove(node.Name);

                                if (_cancellationToken.IsCancellationRequested)
                                    return;
                            }
                        }
                        break;

                    case ChangeType.SetDisplayName:
                        {
                            string newDisplayName = clientChanges[node.FullName].DisplayName;
                            string newFullDisplayName = GetLocalFullName(node.Parent.FullName) + "\\" + newDisplayName;

                            if (file != null && file.DisplayName != newDisplayName)
                            {
                                MoveFileOrDirectory(newFullDisplayName, newFullDisplayName);
                                file.DisplayName = newDisplayName;
                            }

                            // State management

                            clientChanges.Remove(node.FullName);
                            node.Parent.Nodes.Remove(node.Name);

                            if (_cancellationToken.IsCancellationRequested)
                                return;
                        }
                        break;

                    case ChangeType.Delete:
                        {
                            if (file != null)
                            {
                                if (file.IsFolder)
                                    SalvageAndDeleteFolder(file, GetLocalFullName(file.FullName));
                                else
                                    SalvageAndDeleteDocument(file, GetLocalFullName(file.FullName));

                                if (file.InvitationId != 0)
                                    _state.Invitations.Remove(file.InvitationId.ToString());

                                rootFolder.Files.Remove(file.Name);
                            }

                            // State management

                            clientChanges.Remove(node.FullName);
                            node.Parent.Nodes.Remove(node.Name);

                            if (_cancellationToken.IsCancellationRequested)
                                return;
                        }
                        break;
                }
            }
        }

        private bool DownloadDocument(string hash, string destinationFileName)
        {
            var blobDirectory = AccessBlobDirectory();
            string blobFileName = blobDirectory.FullName + "\\" + hash;

            if (System.IO.File.Exists(blobFileName))
            {
                System.IO.File.Copy(blobFileName, destinationFileName, true);
            }
            else
            {
                int attempts = 0;

                while (true)
                {
                    try
                    {
                        Requests.Download(hash, blobFileName);
                        System.IO.File.SetAttributes(blobFileName, FileAttributes.Normal);
                        System.IO.File.Copy(blobFileName, destinationFileName);
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.ToLowerInvariant().Contains("not found"))
                        {
                            // The server doesn't have the file anymore.
                            return false;
                        }
                        else if (++attempts >= 4)
                        {
                            throw;
                        }
                    }
                }
            }

            System.IO.File.SetAttributes(destinationFileName, FileAttributes.Normal);
            System.IO.File.SetLastWriteTimeUtc(destinationFileName, DateTime.Now);

            return true;
        }

        private void ResolveConflicts(ICollection<ClientChange> localChanges, ICollection<ClientChange> remoteChanges)
        {
            var localByName = localChanges.ToDictionary(local => local.FullName);
            ChangeNode.FromItems(localChanges).PreservingConflicts(ChangeNode.FromItems(remoteChanges), (local, remote) =>
                {
                    if (local.Type != ChangeType.Add)
                        return;

                    if (localByName[local.FullName].InvitationId != 0)
                    {
                        // Cannot resolve a conflict on a shared folder. Just delete our copy.
                        Utilities.DeleteDirectoryRecursive(GetLocalFullName(local.FullName));
                        return;
                    }

                    var localClientChange = localByName[local.FullName];
                    string parentPrefix = GetLocalFullName(local.Parent.FullName) + "\\";
                    string fullDisplayName = parentPrefix + localClientChange.DisplayName;
                    string newFullDisplayName;

                    do
                    {
                        newFullDisplayName = parentPrefix + Utilities.GenerateNewName(
                            localClientChange.DisplayName,
                            !local.IsFolder,
                            _state.ClientName + "'s conflicted copy " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff")
                            );
                    } while (System.IO.File.Exists(newFullDisplayName) || System.IO.Directory.Exists(newFullDisplayName));

                    MoveFileOrDirectory(fullDisplayName, newFullDisplayName);
                });
        }

        public bool Run(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            if (!_stateLoaded)
            {
                try
                {
                    LoadState();
                    _stateLoaded = true;
                }
                catch
                { }
            }

            long baseChangelistId = _state.ChangelistId;
            var fsFolder = File.FromFileSystem(_rootDirectory, _state.UserId.ToString());
            var localChanges = this.Compare(fsFolder);
            IgnoreSpecialNames(localChanges);

            if (cancellationToken.IsCancellationRequested)
                return false;

            bool sync = true;

            // Sync with the server.
            while (sync)
            {
                var result = Requests.Sync(new ClientSyncData { BaseChangelistId = baseChangelistId, Changes = localChanges });

                if (cancellationToken.IsCancellationRequested)
                    return false;

                switch (result.State)
                {
                    case ClientSyncResultState.Retry:
                        continue;
                    case ClientSyncResultState.TooOld:
                        throw new NotImplementedException("TooOld");
                    case ClientSyncResultState.Error:
                        throw new Exception("Sync error");
                    case ClientSyncResultState.Conflict:
                        ResolveConflicts(localChanges, result.Changes);
                        return true;
                    case ClientSyncResultState.UploadRequired:
                        {
                            var filesByHash = new Dictionary<string, string>();

                            foreach (var change in localChanges)
                            {
                                if (change.Type == ChangeType.Add && !change.IsFolder &&
                                    !string.IsNullOrEmpty(change.Hash) && !filesByHash.ContainsKey(change.Hash))
                                {
                                    filesByHash.Add(change.Hash, change.FullName);
                                }
                            }

                            foreach (var hash in result.UploadRequiredFor)
                            {
                                string sourceFullName;

                                if (!filesByHash.TryGetValue(hash, out sourceFullName))
                                    throw new Exception("Upload required for a missing file.");

                                Requests.Upload(GetLocalFullName(sourceFullName));

                                if (cancellationToken.IsCancellationRequested)
                                    return false;
                            }
                        }
                        break;
                    case ClientSyncResultState.Success:
                        _state.ChangelistId = result.NewChangelistId;
                        _state.Root = fsFolder;

                        if (result.Changes.Count != 0)
                        {
                            IgnoreSpecialNames(result.Changes);

                            // Delete nodes corresponding to invitations we don't know about (yet).
                            var allowed = new HashSet<string>(_state.Invitations.Keys.Select(x => "@" + x.ToString()));
                            allowed.Add(_state.UserId.ToString());
                            FilterTopLevelNames(result.Changes, allowed);

                            // Make the remote changes sequential with respect to our local changes.
                            var remoteByName = result.Changes.ToDictionary(remote => remote.FullName);
                            ChangeNode.FromItems(localChanges).MakeSequentialByPreserving(ChangeNode.FromItems(result.Changes),
                                (local, remote) => remoteByName.Remove(remote.FullName));
                            var changes = remoteByName.Values.ToList();

                            _state.PendingChanges.Add(changes);
                        }

                        this.SaveState();
                        sync = false;

                        break;
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return false;

            // Process new invitations.
            if (_state.NewInvitations.Count != 0)
            {
                // Get a complete list of files in the shared folders so we can download everything.
                var result = Requests.Sync(new ClientSyncData { BaseChangelistId = 0 });

                IgnoreSpecialNames(result.Changes);
                FilterTopLevelNames(result.Changes, new HashSet<string>(_state.NewInvitations.Keys.Select(x => x.ToString())));

                _state.PendingChanges.Insert(0, result.Changes); // Insert at the front because this must applied first.

                foreach (var pair in _state.NewInvitations)
                    _state.Invitations[pair.Key] = pair.Value;
                _state.NewInvitations.Clear();

                this.SaveState();
            }

            if (cancellationToken.IsCancellationRequested)
                return false;

            // Process pending changes.
            while (_state.PendingChanges.Count != 0)
            {
                try
                {
                    this.Apply(_state.PendingChanges[0]);

                    if (_state.PendingChanges[0].Count == 0) // Has the changelist been fully applied?
                        _state.PendingChanges.RemoveAt(0);
                }
                finally
                {
                    this.SaveState();
                }

                if (_cancellationToken.IsCancellationRequested)
                    return false;
            }

            CleanBlobDirectory();

            return false;
        }

        public File FindFile(string fullName)
        {
            // TODO(WJ): FIX! THIS IS WRONG BUT WE DON'T NEED IT YET

            foreach (File f in _state.Root.RecursiveEnumerate()){
                string fileLocalName = f.FullName.Substring(f.FullName.IndexOf("/") + 2);
                fileLocalName = fileLocalName.Replace("/", "\\");
                fileLocalName = RootDirectory + fileLocalName;
                if (fileLocalName.ToLower() == fullName.ToLower())
                {
                    return f;
                }
            }
            return null;
        }

        private void MoveFileOrDirectory(string src, string dst)
        {
            if (!MoveFile(src, dst))
                throw new Exception("Unable to move '" + src + "': error " + System.Runtime.InteropServices.Marshal.GetLastWin32Error().ToString());
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", BestFitMapping = false, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern bool MoveFile(string src, string dst);
    }
}
