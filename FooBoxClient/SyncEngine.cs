using FooBox;
using FooBox.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace FooBoxClient
{
    public class SyncEngine
    {
        private const string SpecialFolderName = ".FooBox";

        private class State
        {
            public long ChangelistId { get; set; }
            public File Root { get; set; }
        }

        private string _rootDirectory;
        private long _changelistId;
        private File _root;
        private long _userId;
        private string _configDirectory;
        private string _stateFileName;

        public SyncEngine(string rootDirectory)
        {
            _rootDirectory = rootDirectory;
            _root = File.CreateRoot();
            _userId = Properties.Settings.Default.UserID;

            var userRoot = new File
            {
                FullName = "/" + _userId.ToString(),
                Name = _userId.ToString(),
                DisplayName = _userId.ToString(),
                IsFolder = true,
                Files = new Dictionary<string, File>()
            };
            _root.Files.Add(userRoot.Name, userRoot);

            _configDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\FooBoxClient";
            _stateFileName = _configDirectory + "\\state.json";
        }

        public long ChangelistId
        {
            get { return _changelistId; }
        }

        public void LoadState()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            var state = serializer.Deserialize<State>(System.IO.File.ReadAllText(_stateFileName));

            _changelistId = state.ChangelistId;
            _root = state.Root;
        }

        public void SaveState()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string directoryName = Path.GetDirectoryName(_stateFileName);

            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);

            System.IO.File.WriteAllText(_stateFileName, serializer.Serialize(new State
            {
                ChangelistId = _changelistId,
                Root = _root
            }));
        }

        public string GetLocalFullName(string fullName)
        {
            string prefix = "/" + _userId.ToString();

            if (!fullName.StartsWith(prefix))
                throw new Exception("Invalid file name");

            return _rootDirectory + fullName.Remove(0, prefix.Length).Replace('/', '\\');
        }

        public ICollection<ClientChange> Compare(File newFolder)
        {
            return Compare(_root, newFolder);
        }

        private ICollection<ClientChange> Compare(File oldFolder, File newFolder)
        {
            var changes = new List<ClientChange>();
            Compare(changes, oldFolder, newFolder);
            return changes;
        }

        /// <summary>
        /// Compares two folders.
        /// Also updates the hashes in the new folder.
        /// </summary>
        private void Compare(ICollection<ClientChange> changes, File oldFolder, File newFolder)
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
                return;

            Apply(_root.Files[_userId.ToString()], rootNode.Nodes[_userId.ToString()], clientChanges);
        }

        private void Apply(File rootFolder, ChangeNode rootNode, Dictionary<string, ClientChange> clientChanges)
        {
            if (rootNode.Nodes == null)
                return;

            foreach (ChangeNode node in rootNode.Nodes.Values)
            {
                File file = null;

                if (rootFolder.Files != null)
                    rootFolder.Files.TryGetValue(node.Name, out file);

                switch (node.Type)
                {
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
                            }
                            else
                            {
                                // Add document

                                if (file != null && file.IsFolder)
                                {
                                    System.IO.Directory.Delete(GetLocalFullName(file.FullName), true);
                                    file = null;
                                }

                                if (System.IO.File.Exists(newFullDisplayName) &&
                                    file != null &&
                                    file.DisplayName != newDisplayName)
                                {
                                    MoveFileOrDirectory(newFullDisplayName, newFullDisplayName);
                                }

                                string hash = clientChanges[node.FullName].Hash;

                                if (file == null || file.Hash != hash || !System.IO.File.Exists(newFullDisplayName))
                                    Requests.Download(hash, newFullDisplayName);

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
                        }
                        break;
                }
            }
        }

        public void Replace(ICollection<ClientChange> changes)
        {
            var clientChanges = changes.ToDictionary(change => change.FullName);
            var rootNode = ChangeNode.FromItems(changes);

            _root.Files[_userId.ToString()].Files.Clear();

            if (rootNode.Nodes == null || rootNode.Nodes.Count == 0)
            {
                Directory.Delete(_rootDirectory, true);
                Directory.CreateDirectory(_rootDirectory);
                return;
            }

            Replace(_root.Files[_userId.ToString()], rootNode.Nodes[_userId.ToString()], clientChanges);
        }

        private void Replace(File rootFolder, ChangeNode rootNode, Dictionary<string, ClientChange> clientChanges)
        {
            if (rootNode.Nodes == null)
                return;

            // Delete any files not in the node.

            foreach (var info in (new DirectoryInfo(GetLocalFullName(rootFolder.FullName))).EnumerateFileSystemInfos())
            {
                ChangeNode node;
                bool delete = false;

                if (rootNode.Nodes.TryGetValue(info.Name.ToUpperInvariant(), out node))
                {
                    if (node.IsFolder != ((info.Attributes & FileAttributes.Directory) != 0))
                    {
                        delete = true;
                    }
                }
                else
                {
                    delete = true;
                }

                if (delete)
                {
                    if ((info.Attributes & FileAttributes.Directory) != 0)
                        Directory.Delete(info.FullName, true);
                    else
                        System.IO.File.Delete(info.FullName);
                }
            }

            foreach (ChangeNode node in rootNode.Nodes.Values)
            {
                if (node.Type != ChangeType.Add)
                    throw new Exception("Non-add change in Replace.");

                string newDisplayName = clientChanges[node.FullName].DisplayName;
                string newFullDisplayName = GetLocalFullName(node.Parent.FullName) + "\\" + newDisplayName;

                if (rootFolder.Files == null)
                    rootFolder.Files = new Dictionary<string, File>();

                if (node.IsFolder)
                {
                    // Add folder

                    if (!System.IO.Directory.Exists(newFullDisplayName))
                        System.IO.Directory.CreateDirectory(newFullDisplayName);
                    else
                        MoveFileOrDirectory(newFullDisplayName, newFullDisplayName);

                    var file = new File
                    {
                        FullName = node.FullName,
                        Name = node.Name,
                        DisplayName = newDisplayName,
                        IsFolder = true
                    };
                    rootFolder.Files[file.Name] = file;

                    // Process files in the folder.
                    if (node.Nodes != null && node.Nodes.Count != 0)
                        Replace(file, node, clientChanges);
                }
                else
                {
                    // Add document

                    string hash = clientChanges[node.FullName].Hash;
                    string oldHash = "";

                    if (System.IO.File.Exists(newFullDisplayName))
                    {
                        MoveFileOrDirectory(newFullDisplayName, newFullDisplayName);
                        oldHash = Utilities.ComputeSha256Hash(newFullDisplayName);
                    }

                    if (oldHash != hash)
                        Requests.Download(hash, newFullDisplayName);

                    FileInfo info = new FileInfo(newFullDisplayName);

                    var file = new File
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
            }
        }

        public void Sync()
        {
            long baseChangelistId = _changelistId;
            var fsFolder = File.FromFileSystem(_rootDirectory, _userId.ToString());
            var changes = this.Compare(fsFolder);

            while (true)
            {
                var result = Requests.Sync(new ClientSyncData { BaseChangelistId = baseChangelistId, Changes = changes });

                switch (result.State)
                {
                    case ClientSyncResultState.Retry:
                        continue;
                    case ClientSyncResultState.TooOld:
                        throw new NotImplementedException("TooOld");
                    case ClientSyncResultState.Error:
                        return;
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
                            }
                        }
                        break;
                    case ClientSyncResultState.Success:
                        _root = fsFolder;
                        this.Apply(result.Changes);
                        _changelistId = result.NewChangelistId;
                        this.SaveState();
                        return;
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
