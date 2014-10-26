using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
            public ObjectState State { get; set; }
            public bool HasInvitation { get; set; }
            public bool HasTargetInvitations { get; set; }
        }

        public string FullDisplayName { get; set; }
        public string DisplayName { get; set; }
        public ObjectState State { get; set; }
        public List<FileEntry> Files { get; set; }
        public List<Tuple<string, string>> Parents { get; set; }
        public bool SharedFolder { get; set; }
        public bool SharedWithMe { get; set; }
    }

    public class VersionHistoryViewModel
    {
        public class VersionEntry
        {
            public long Size { get; set; }
            public DateTime TimeStamp { get; set; }
            public long VersionId { get; set; }
            public string ClientName { get; set; }
            public long UserId { get; set; }
            public string UserName { get; set; }

        }

        public string DisplayName { get; set; }
        public string FullDisplayName { get; set; }
        public bool SharedFolder { get; set; }

        public List<VersionEntry> Versions { get; set; }
    }

    public class SharedLinkEntry
    {
        public long Id { get; set; }
        public string RelativeFullName { get; set; }
        public string Key { get; set; }
    }
}