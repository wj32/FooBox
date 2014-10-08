using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace FooBox.Models
{
    public class UserManager : IDisposable
    {
        public const string AdministratorsGroupName = "Administrators";
        public const string DefaultUserName = "__DEFAULT__";

        public class UserIdentity : IIdentity
        {
            public UserIdentity(string authenticationType, long userId)
            {
                AuthenticationType = authenticationType;
                Name = userId.ToString();
                UserId = userId;
            }

            public string AuthenticationType
            {
                get;
                private set;
            }

            public bool IsAuthenticated
            {
                get { return true; }
            }

            public string Name
            {
                get;
                private set;
            }

            public long UserId
            {
                get;
                private set;
            }
        }

        private FooBoxContext _context;
        private bool _contextOwned;
        private System.Security.Cryptography.HashAlgorithm _sha1Algorithm = System.Security.Cryptography.SHA1.Create();

        #region Class

        public UserManager()
            : this(new FooBoxContext(), true)
        { }

        public UserManager(FooBoxContext context)
            : this(context, false)
        { }

        public UserManager(FooBoxContext context, bool contextOwned)
        {
            _context = context;
            _contextOwned = contextOwned;
        }

        public void Dispose()
        {
            if (_contextOwned)
                _context.Dispose();
            _sha1Algorithm.Dispose();
        }

        public FooBoxContext Context
        {
            get { return _context; }
        }

        #endregion

        #region Setup

        public void InitialSetup()
        {
            if (_context.Identities.Any())
                throw new Exception("The database is already set up.");

            var adminGroup = new Group { Name = "Administrators", Description = "Administrators", IsAdmin = true };
            _context.Groups.Add(adminGroup);

            var defaultUser = new User { Name = DefaultUserName, PasswordHash = "", PasswordSalt = "", QuotaLimit = long.MaxValue };
            defaultUser.Groups.Add(adminGroup);
            _context.Users.Add(defaultUser);

            _context.SaveChanges();
        }

        #endregion

        #region Users

        public ClaimsIdentity CreateIdentity(User user, string authenticationType)
        {
            return new ClaimsIdentity(new UserIdentity(authenticationType, user.Id), new Claim[]
                {
                    new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", user.Id.ToString()),
                    new Claim("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider", "None")
                });
        }

        private string ComputePasswordHash(string salt, string password)
        {
            return (new SoapBase64Binary(_sha1Algorithm.ComputeHash(System.Text.Encoding.ASCII.GetBytes(salt + password)))).ToString();
        }

        public User CreateUser(User template, string password)
        {
            byte[] saltBytes = new byte[16];
            (new Random()).NextBytes(saltBytes);
            string salt = (new SoapBase64Binary(saltBytes)).ToString();
            User newUser = new User
            {
                Name = template.Name,
                PasswordHash = ComputePasswordHash(salt, password),
                PasswordSalt = salt,
                FirstName = template.FirstName,
                LastName = template.LastName,
                QuotaLimit = template.QuotaLimit
            };

            try
            {
                _context.Users.Add(newUser);
                _context.SaveChanges();

                using (var fileManager = new FileManager(_context))
                {
                    // Create the user's root folder.
                    fileManager.CreateUserRootFolder(newUser);
                    // Create the user's default internal client.
                    fileManager.CreateClient(newUser.Id, "Internal", FileManager.InternalClientTag);
                }
            }
            catch
            {
                return null;
            }

            return newUser;
        }

        public User FindUser(long userId)
        {
            return (from user in _context.Users where user.Id == userId select user).SingleOrDefault();
        }

        public User FindUser(string userName)
        {
            return (from user in _context.Users where user.Name == userName && user.State == ObjectState.Normal select user).SingleOrDefault();
        }

        public User FindUser(string userName, string password)
        {
            var user = FindUser(userName);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                return null;

            if (!string.Equals(user.PasswordHash, ComputePasswordHash(user.PasswordSalt, password), StringComparison.InvariantCultureIgnoreCase))
                return null;

            return user;
        }

        public User GetDefaultUser()
        {
            return FindUser(DefaultUserName);
        }

        public bool DeleteUser(long userId)
        {
            User user = FindUser(userId);

            if (user == null)
                return false;

            user.State = ObjectState.Deleted;

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return false;
            }

            return true;
        }

        public long GetUserId(string userName)
        {
            return (from user in _context.Users where user.Name == userName && user.State == ObjectState.Normal select user.Id).SingleOrDefault();
        }

        public string GetUserName(long userId)
        {
            return (from user in _context.Users where user.Id == userId select user.Name).SingleOrDefault();
        }

        public bool ChangeUserPassword(long userId, string oldPassword, string newPassword)
        {
            var user = FindUser(userId);

            if (user == null)
                return false;

            if (!string.Equals(user.PasswordHash, ComputePasswordHash(user.PasswordSalt, oldPassword), StringComparison.InvariantCultureIgnoreCase))
                return false;

            user.PasswordHash = ComputePasswordHash(user.PasswordSalt, newPassword);

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool IsUserAdmin(long userId)
        {
            return _context.Users
                .Where(user => user.Id == userId)
                .SelectMany(user => user.Groups.AsQueryable()
                    .Where(groop => groop.IsAdmin)
                    .Select(groop => groop.Id))
                .Any();
        }

        #endregion

        #region Groups

        public Group CreateGroup(Group template)
        {
            Group newGroup = new Group
            {
                Name = template.Name,
                Description = template.Description,
                IsAdmin = template.IsAdmin,
                Users = template.Users
            };

            try
            {
                _context.Groups.Add(newGroup);
                _context.SaveChanges();
            }
            catch
            {
                return null;
            }

            return newGroup;
        }

        public Group FindGroup(long groupId)
        {
            return (from groop in _context.Groups where groop.Id == groupId select groop).SingleOrDefault();
        }

        public Group FindGroup(string groupName)
        {
            return (from groop in _context.Groups where groop.Name == groupName && groop.State == ObjectState.Normal select groop).SingleOrDefault();
        }

        public bool DeleteGroup(long groupId)
        {
            Group group = FindGroup(groupId);

            if (group == null)
                return false;

            group.State = ObjectState.Deleted;

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                return false;
            }

            return true;
        }

        public long GetGroupId(string groupName)
        {
            return (from groop in _context.Groups where groop.Name == groupName && groop.State == ObjectState.Normal select groop.Id).SingleOrDefault();
        }

        public string GetGroupName(long groupId)
        {
            return (from groop in _context.Groups where groop.Id == groupId select groop.Name).SingleOrDefault();
        }

        #endregion
    }
    public static class IdentityExtensions
    {
        public static long GetUserId(this IIdentity identity)
        {
            return long.Parse(identity.Name);
        }
    }
}