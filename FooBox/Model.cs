using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace FooBox
{
    #region Enumerations

    public enum ObjectState : int
    {
        Normal = 0,
        Deleted = 1
    }

    #endregion

    #region Entities

    public class Blob
    {
        public const int KeyLength = 32;
        public const int HashBits = 512;

        public Blob()
        {
            this.DocumentVersions = new HashSet<DocumentVersion>();
        }

        public long Id { get; set; }
        [StringLength(KeyLength)]
        [Index(IsUnique = true)]
        public string Key { get; set; }
        public long Size { get; set; }
        [MaxLength(HashBits / 4)]
        [Index]
        public string Hash { get; set; }

        public virtual ICollection<DocumentVersion> DocumentVersions { get; set; }
    }

    public class Change
    {
        public long Id { get; set; }
        public FooBox.Common.ChangeType Type { get; set; }
        public string FullName { get; set; }
        public bool IsFolder { get; set; }
        public long ChangelistId { get; set; }

        public virtual Changelist Changelist { get; set; }
    }

    public class Changelist
    {
        public Changelist()
        {
            this.Changes = new HashSet<Change>();
        }

        public long Id { get; set; }
        public long ClientId { get; set; }
        public DateTime TimeStamp { get; set; }

        public virtual Client Client { get; set; }
        public virtual ICollection<Change> Changes { get; set; }
    }

    public class Client
    {
        public const int NameMaxLength = 128;

        public long Id { get; set; }
        [MaxLength(NameMaxLength)]
        [Index]
        public string Name { get; set; }
        public ObjectState State { get; set; }
        [MaxLength(16)]
        [Index]
        public string Tag { get; set; }
        public long UserId { get; set; }
        public string Secret { get; set; }
        public DateTime AccessTime { get; set; }

        public virtual User User { get; set; }
    }

    public class Document : File
    {
        public Document()
        {
            this.DocumentVersions = new HashSet<DocumentVersion>();
        }

        public virtual ICollection<DocumentVersion> DocumentVersions { get; set; }
    }

    public class DocumentLink
    {
        public const int KeyLength = 32;

        public long Id { get; set; }
        [MaxLength(KeyLength)]
        [Index(IsUnique = true)]
        public string Key { get; set; }
        public string RelativeFullName { get; set; }

        public virtual User User { get; set; }
    }

    public class DocumentVersion
    {
        public long Id { get; set; }
        [Index]
        public DateTime TimeStamp { get; set; }
        public long DocumentId { get; set; }
        public long ClientId { get; set; }

        public virtual Document Document { get; set; }
        public virtual Blob Blob { get; set; }
        public virtual Client Client { get; set; }
    }

    public abstract class File
    {
        public const int NameMaxLength = 512;

        public long Id { get; set; }
        [MaxLength(NameMaxLength)]
        [Index("IX_FileName")]
        [Index("IX_FileParentFolderName", 2, IsUnique = true)]
        public string Name { get; set; }
        [MaxLength(NameMaxLength)]
        public string DisplayName { get; set; }
        [Index]
        public ObjectState State { get; set; }
        [MaxLength(16)]
        [Index]
        public string Tag { get; set; }
        [Index("IX_FileParentFolderName", 1, IsUnique = true)]
        public long? ParentFolderId { get; set; }

        public virtual Folder ParentFolder { get; set; }
    }

    public class Folder : File
    {
        public Folder()
        {
            this.Files = new HashSet<File>();
            this.ShareSubFolders = new HashSet<Folder>();
            this.RootOfUsers = new HashSet<User>();
            this.TargetOfLinks = new HashSet<Link>();
        }

        public long OwnerId { get; set; }

        public virtual User Owner { get; set; }
        public virtual ICollection<File> Files { get; set; }
        public virtual Folder ShareFolder { get; set; }
        public virtual ICollection<Folder> ShareSubFolders { get; set; }
        public virtual ICollection<User> RootOfUsers { get; set; }
        public virtual ICollection<Link> TargetOfLinks { get; set; }
    }

    public class Group : Identity
    {
        public Group()
        {
            this.Users = new HashSet<User>();
        }

        public string Description { get; set; }
        public bool IsAdmin { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }

    public abstract class Identity
    {
        public const int NameMaxLength = 64;

        public long Id { get; set; }

        [DisplayName("Name")]
        [MaxLength(NameMaxLength)]
        [Index("IX_Name", IsUnique = true)]
        [Index("IX_IdentityNameState", 1)]
        public string Name { get; set; }

        [Index("IX_IdentityNameState", 2)]
        public ObjectState State { get; set; }
    }

    public class Link : File
    {
        public long? TargetId { get; set; }

        public virtual Folder Target { get; set; }
    }

    public class User : Identity
    {
        public User()
        {
            this.Groups = new HashSet<Group>();
            this.Clients = new HashSet<Client>();
            this.DocumentLinks = new HashSet<DocumentLink>();
        }

        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }

        [DisplayName("First Name")]
        public string FirstName { get; set; }

        [DisplayName("Last Name")]
        public string LastName { get; set; }

        [DisplayName("Quota Limit")]
        public long QuotaLimit { get; set; }

        [DisplayName("Quota Charged")]
        public long QuotaCharged { get; set; }

        public virtual ICollection<Group> Groups { get; set; }
        public virtual ICollection<Client> Clients { get; set; }
        public virtual Folder RootFolder { get; set; }
        public virtual ICollection<DocumentLink> DocumentLinks { get; set; }
    }

    #endregion

    #region Context

    public class FooBoxContext : DbContext
    {
        public FooBoxContext()
            : base("name=DefaultConnection")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DocumentVersion>().HasRequired(t => t.Client).WithMany().HasForeignKey(t => t.ClientId);
            modelBuilder.Entity<Folder>().HasMany(t => t.Files).WithOptional(t => t.ParentFolder).HasForeignKey(t => t.ParentFolderId);
            modelBuilder.Entity<Folder>().HasRequired(t => t.Owner).WithMany().HasForeignKey(t => t.OwnerId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Folder>().HasOptional(t => t.ShareFolder).WithMany(t => t.ShareSubFolders);
            modelBuilder.Entity<Link>().HasOptional(t => t.Target).WithMany(t => t.TargetOfLinks).HasForeignKey(t => t.TargetId);
            modelBuilder.Entity<User>().HasOptional(t => t.RootFolder).WithMany(t => t.RootOfUsers);
        }

        public virtual DbSet<Blob> Blobs { get; set; }
        public virtual DbSet<Change> Changes { get; set; }
        public virtual DbSet<Changelist> Changelists { get; set; }
        public virtual DbSet<Client> Clients { get; set; }
        public virtual DbSet<Document> Documents { get; set; }
        public virtual DbSet<DocumentLink> DocumentLinks { get; set; }
        public virtual DbSet<DocumentVersion> DocumentVersions { get; set; }
        public virtual DbSet<File> Files { get; set; }
        public virtual DbSet<Folder> Folders { get; set; }
        public virtual DbSet<Group> Groups { get; set; }
        public virtual DbSet<Identity> Identities { get; set; }
        public virtual DbSet<Link> Links { get; set; }
        public virtual DbSet<User> Users { get; set; }
    }

#endregion
}
