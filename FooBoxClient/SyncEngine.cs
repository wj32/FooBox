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
        private class State
        {
            public long ChangelistId { get; set; }
            public File Root { get; set; }
        }

        private string _rootDirectory;
        private long _changelistId;
        private File _root;
        private long _userId;
        private string _stateFileName = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\FooBoxClient\\state.json";

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
        }

        public long ChangelistId
        {
            get { return _changelistId; }
            set { _changelistId = value; }
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
                    if (newFile.IsFolder != oldFile.IsFolder ||
                        newFile.LastWriteTimeUtc != oldFile.LastWriteTimeUtc ||
                        newFile.Size != oldFile.Size)
                    {
                        add = true;
                    }
                    else if (newFile.DisplayName != oldFile.DisplayName)
                    {
                        changes.Add(new ClientChange
                        {
                            FullName = newFile.FullName,
                            Type = ChangeType.SetDisplayName,
                            DisplayName = newFile.DisplayName
                        });
                    }
                }
                else
                {
                    add = true;
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
                        Hash = Utilities.ComputeSha256Hash(GetLocalFullName(newFile.FullName))
                    });
                }

                if (newFile.IsFolder)
                    this.Compare(changes, (oldFile != null && oldFile.IsFolder) ? oldFile : null, newFile);
            }
        }

        public void Apply(ICollection<ClientChange> changes)
        {
            var clientChanges = changes.ToDictionary(change => change.FullName);
            var rootNode = ChangeNode.FromItems(changes);

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
                                    System.IO.Directory.Move(newFullDisplayName, newFullDisplayName);

                                file = new File
                                {
                                    FullName = node.FullName,
                                    Name = node.Name,
                                    DisplayName = newDisplayName,
                                    IsFolder = true
                                };
                                rootFolder.Files[file.Name] = file;

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
                                    System.IO.File.Move(newFullDisplayName, newFullDisplayName);
                                }

                                Requests.Download(clientChanges[node.FullName].Hash, newFullDisplayName);
                                FileInfo info = new FileInfo(newFullDisplayName);

                                file = new File
                                {
                                    FullName = node.FullName,
                                    Name = node.Name,
                                    DisplayName = newDisplayName,
                                    IsFolder = false,
                                    Size = info.Length,
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
                                if (file.IsFolder)
                                    System.IO.Directory.Move(newFullDisplayName, newFullDisplayName);
                                else
                                    System.IO.File.Move(newFullDisplayName, newFullDisplayName);

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
    }
}
