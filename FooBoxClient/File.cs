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

        public static File FromFileSystem(string rootDirectory, string subRoot = null)
        {
            File rootFolder = CreateRoot();
            File realRoot = rootFolder;

            if (subRoot != null)
            {
                realRoot = new File
                {
                    FullName = "/" + subRoot,
                    Name = subRoot.ToUpperInvariant(),
                    DisplayName = subRoot,
                    IsFolder = true,
                    Files = new Dictionary<string, File>()
                };
                rootFolder.Files.Add(realRoot.Name, realRoot);
            }

            FromFileSystem(realRoot, rootDirectory);

            return rootFolder;
        }

        private static void FromFileSystem(File rootFolder, string rootDirectory)
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

                if (!file.IsFolder)
                    file.Size = ((FileInfo)info).Length;

                if (rootFolder.Files == null)
                    rootFolder.Files = new Dictionary<string, File>();

                rootFolder.Files.Add(file.Name, file);

                if (file.IsFolder)
                    FromFileSystem(file, info.FullName);
            }
        }
    }
}
