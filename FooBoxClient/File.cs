using FooBox.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooBoxClient
{
    public class File
    {
        public string FullName { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool IsFolder { get; set; }

        // For files
        public long Size { get; set; }
        public string Hash { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }

        // For folders
        public long InvitationId { get; set; }

        public Dictionary<string, File> Files { get; set; }

        public static File CreateRoot()
        {
            return new File
            {
                FullName = "",
                Name = "",
                DisplayName = "",
                IsFolder = true,
                Files = new Dictionary<string, File>()
            };
        }

        public static File FromFileSystem(string rootDirectory, string defaultSubRoot = null)
        {
            File rootFolder = CreateRoot();
            File subRoot = rootFolder;

            if (defaultSubRoot != null)
            {
                subRoot = new File
                {
                    FullName = "/" + defaultSubRoot,
                    Name = defaultSubRoot.ToUpperInvariant(),
                    DisplayName = defaultSubRoot,
                    IsFolder = true,
                    Files = new Dictionary<string, File>()
                };
                rootFolder.Files.Add(subRoot.Name, subRoot);
            }

            FromFileSystem(rootFolder, subRoot, rootDirectory);

            return rootFolder;
        }

        private static void FromFileSystem(File absoluteRoot, File rootFolder, string rootDirectory)
        {
            DirectoryInfo di = new DirectoryInfo(rootDirectory);

            foreach (var info in di.EnumerateFileSystemInfos())
            {
                File file = new File
                {
                    FullName = rootFolder.FullName + "/" + info.Name.ToUpperInvariant(),
                    Name = info.Name.ToUpperInvariant(),
                    DisplayName = info.Name,
                    IsFolder = (info.Attributes & FileAttributes.Directory) != 0,
                    LastWriteTimeUtc = info.LastWriteTimeUtc
                };

                if (file.IsFolder)
                    file.InvitationId = SyncEngine.ReadInvitationId(info.FullName);
                else
                    file.Size = ((FileInfo)info).Length;

                if (rootFolder.Files == null)
                    rootFolder.Files = new Dictionary<string, File>();

                rootFolder.Files.Add(file.Name, file);

                if (file.IsFolder)
                {
                    if (file.InvitationId != 0)
                    {
                        string invitationName = "@" + file.InvitationId.ToString();

                        if (!absoluteRoot.Files.ContainsKey(invitationName))
                        {
                            var invitationRoot = new File
                            {
                                FullName = "/" + invitationName,
                                Name = invitationName,
                                DisplayName = invitationName,
                                IsFolder = true
                            };
                            absoluteRoot.Files.Add(invitationName, invitationRoot);
                            FromFileSystem(absoluteRoot, invitationRoot, info.FullName);
                        }
                    }
                    else
                    {
                        FromFileSystem(absoluteRoot, file, info.FullName);
                    }
                }
            }
        }
    }
}
