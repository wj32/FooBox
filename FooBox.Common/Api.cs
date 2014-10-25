using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooBox.Common
{
    public class InvitationInfo
    {
        public class Entry
        {
            public long Id { get; set; }
            public DateTime TimeStamp { get; set; }
            public string TargetName { get; set; }
            public string TargetOwnerName { get; set; }
            public bool Accepted { get; set; }
        }

        public DateTime At;
        public List<Entry> Entries;
    }

    public class ClientChange : ChangeItem
    {
        public long Size { get; set; }
        public string Hash { get; set; }
        public string DisplayName { get; set; }
        public long? InvitationId { get; set; }
    }

    public class ClientSyncData
    {
        public ClientSyncData()
        {
            this.Changes = new HashSet<ClientChange>();
        }

        /// <summary>
        /// The client that is making the changes.
        /// </summary>
        public long ClientId { get; set; }

        /// <summary>
        /// The changelist that the client is currently synchronized to.
        /// </summary>
        public long BaseChangelistId { get; set; }

        public ICollection<ClientChange> Changes { get; set; }
    }

    public enum ClientSyncResultState
    {
        /// <summary>
        /// Another client made changes while applying these changes.
        /// Try again.
        /// </summary>
        Retry,

        /// <summary>
        /// One or more changelists since <see cref="BaseChangelistId"/> are no longer
        /// in the database. Request a complete list of files by setting
        /// <see cref="BaseChangelistId"/> to 0.
        /// </summary>
        TooOld,

        /// <summary>
        /// An error occurred. See <see cref="Exception"/> for the exception object.
        /// </summary>
        Error,

        /// <summary>
        /// The changes conflict with other changes made since
        /// <see cref="BaseChangelistId"/>. The list of other changes is in
        /// <see cref="Changes"/>.
        /// </summary>
        Conflict,

        /// <summary>
        /// One or more files need to be uploaded. The list of files is in
        /// <see cref="UploadRequiredFor"/>
        /// </summary>
        UploadRequired,

        /// <summary>
        /// The operation succeeded.
        /// </summary>
        Success
    }

    public class ClientSyncResult
    {
        public ClientSyncResultState State { get; set; }

        // >= Error

        public string ErrorMessage { get; set; }

        // >= Conflict

        public long LastChangelistId { get; set; }
        public List<ClientChange> Changes { get; set; }

        // >= UploadRequired

        public ICollection<string> UploadRequiredFor { get; set; }

        // >= Success

        public long NewChangelistId { get; set; }
    }

    public class ClientSyncPostData
    {
        public long Id { get; set; }
        public string Secret { get; set; }
        public ClientSyncData Data { get; set; }
    }

    public class ClientLoginResult
    {
        public long Id { get; set; }
        public string Secret { get; set; }
        public long UserId { get; set; }
    }
}
