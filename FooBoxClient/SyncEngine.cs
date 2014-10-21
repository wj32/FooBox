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

        public class State
        {
            public long UserId { get; set; }

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
        }

        private string _rootDirectory;
        private State _state;
        private bool _stateLoaded;
        private CancellationToken _cancellationToken;

        private string _localSpecialFolder;
        private string _stateFileName;
        private string _specialFolderFullName;

        public SyncEngine(string rootDirectory, long userId)
        {
            _rootDirectory = rootDirectory;

            this.ResetState(userId);

            _localSpecialFolder = _rootDirectory + "\\" + SpecialFolderName;
            _stateFileName = _localSpecialFolder + "\\" + StateFileName;
            _specialFolderFullName = "/" + userId.ToString() + "/" + SpecialFolderName.ToUpperInvariant();
        }

        public long ChangelistId
        {
            get { return _state.ChangelistId; }
        }

        private void ResetState(long userId)
        {
            _state = new State
            {
                UserId = userId,
                ChangelistId = 0,
                Root = File.CreateRoot(),
                PendingChanges = new List<ICollection<ClientChange>>()
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

        public string GetLocalFullName(string fullName)
        {
            string prefix = "/" + _state.UserId.ToString();

            if (!fullName.StartsWith(prefix))
                throw new Exception("Invalid file name");

            return _rootDirectory + fullName.Remove(0, prefix.Length).Replace('/', '\\');
        }

        public void IgnoreSpecialFolder(List<ClientChange> changes)
        {
            changes.RemoveAll(change => change.FullName == _specialFolderFullName || change.FullName.StartsWith(_specialFolderFullName + "/"));
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

                    if (!newFile.IsFolder)
                    {
                        newHash = Utilities.ComputeSha256Hash(GetLocalFullName(newFile.FullName));
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
                            Hash = newHash
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

            Apply(_state.Root.Files[_state.UserId.ToString()], rootNode.Nodes[_state.UserId.ToString()], clientChanges);

            if (!rootNode.Nodes.ContainsKey(_state.UserId.ToString()) ||
                rootNode.Nodes[_state.UserId.ToString()].Nodes == null ||
                rootNode.Nodes[_state.UserId.ToString()].Nodes.Count == 0)
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
                                    System.IO.File.Delete(GetLocalFullName(file.FullName));
                                    file = null;
                                }

                                if (!System.IO.Directory.Exists(newFullDisplayName))
                                    System.IO.Directory.CreateDirectory(newFullDisplayName);
                                else if (file != null && file.DisplayName != newDisplayName)
                                    MoveFileOrDirectory(newFullDisplayName, newFullDisplayName);

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
                                    System.IO.Directory.Delete(GetLocalFullName(file.FullName), true);
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
                                    string tempFileName = newFullDisplayName + "." + Utilities.GenerateRandomString(Utilities.IdChars, 8);

                                    valid = false;
                                    int attempts = 0;

                                    while (true)
                                    {
                                        try
                                        {
                                            Requests.Download(hash, tempFileName);

                                            if (System.IO.File.Exists(newFullDisplayName))
                                                System.IO.File.Delete(newFullDisplayName);
                                            MoveFileOrDirectory(tempFileName, newFullDisplayName);

                                            break;
                                        }
                                        catch (Exception ex)
                                        {
                                            if (ex.Message.ToLowerInvariant().Contains("not found"))
                                            {
                                                // The server doesn't have the file anymore.
                                                valid = false;
                                                break;
                                            }
                                            else if (++attempts >= 4)
                                            {
                                                throw;
                                            }
                                        }
                                    }
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
                                    System.IO.Directory.Delete(GetLocalFullName(file.FullName), true);
                                else
                                    System.IO.File.Delete(GetLocalFullName(file.FullName));

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

        public void Run(CancellationToken cancellationToken)
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
            var changes = this.Compare(fsFolder);
            IgnoreSpecialFolder(changes);

            if (cancellationToken.IsCancellationRequested)
                return;

            bool sync = true;

            // Sync with the server.
            while (sync)
            {
                var result = Requests.Sync(new ClientSyncData { BaseChangelistId = baseChangelistId, Changes = changes });

                if (cancellationToken.IsCancellationRequested)
                    return;

                switch (result.State)
                {
                    case ClientSyncResultState.Retry:
                        continue;
                    case ClientSyncResultState.TooOld:
                        throw new NotImplementedException("TooOld");
                    case ClientSyncResultState.Error:
                        throw new Exception("Sync error");
                    case ClientSyncResultState.Conflict:
                        // TODO
                        break;
                    case ClientSyncResultState.UploadRequired:
                        {
                            var filesByHash = new Dictionary<string, string>();

                            foreach (var change in changes)
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
                                    return;
                            }
                        }
                        break;
                    case ClientSyncResultState.Success:
                        _state.ChangelistId = result.NewChangelistId;
                        _state.Root = fsFolder;

                        if (result.Changes.Count != 0)
                        {
                            IgnoreSpecialFolder(result.Changes);
                            _state.PendingChanges.Add(result.Changes);
                        }

                        this.SaveState();
                        sync = false;

                        break;
                }
            }

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
            }
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
