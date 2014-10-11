using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FooBox.Models
{
    public class FileBrowseViewModel
    {
        public class FileEntry
        {
            public long Id { get; set; }
            public bool IsParent { get; set; }
            public string FullDisplayName { get; set; }
            public string DisplayName { get; set; }
            public bool IsFolder { get; set; }
            public long Size { get; set; }
            public DateTime TimeStamp { get; set; }
        }

        public string FullDisplayName { get; set; }

        public string DisplayName { get; set; }

        public List<FileEntry> Files { get; set; }

        public List<Tuple<string, string>> Parents { get; set; }

    }
}