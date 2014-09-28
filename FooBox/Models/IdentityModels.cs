using System;
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
        public class UserIdentity : IIdentity
        {
            public UserIdentity(string authenticationType, string userName)
            {
                AuthenticationType = authenticationType;
                Name = userName;
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
        }

        private FooBoxContext _context;
        private bool _contextOwned;
        private System.Security.Cryptography.HashAlgorithm _hashAlgorithm = System.Security.Cryptography.SHA1.Create();

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
            _hashAlgorithm.Dispose();
        }

        public FooBoxContext Context
        {
            get { return _context; }
        }

        public ClaimsIdentity CreateIdentity(User user, string authenticationType)
        {
            return new ClaimsIdentity(new UserIdentity(authenticationType, user.Name), new Claim[]
                {
                    new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", user.Id.ToString()),
                    new Claim("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider", "None")
                });
        }

        private string ComputePasswordHash(string salt, string password)
        {
            return (new SoapBase64Binary(_hashAlgorithm.ComputeHash(System.Text.Encoding.ASCII.GetBytes(salt + password)))).ToString();
        }

        public async Task<bool> CreateAsync(User template, string password)
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
                await _context.SaveChangesAsync();
            }
            catch
            {
                return false;
            }

            return true;
        }

        public Task<User> FindAsync(string userName)
        {
            return (from user in _context.Users where user.Name == userName select user).SingleOrDefaultAsync();
        }

        public async Task<User> FindAsync(string userName, string password)
        {
            var user = await FindAsync(userName);

            if (user == null)
                return null;

            if (!string.Equals(user.PasswordHash, ComputePasswordHash(user.PasswordSalt, password), StringComparison.InvariantCultureIgnoreCase))
                return null;

            return user;
        }

        public async Task<bool> ChangePasswordAsync(string userName, string oldPassword, string newPassword)
        {
            var user = await FindAsync(userName, oldPassword);

            if (user == null)
                return false;

            user.PasswordHash = ComputePasswordHash(user.PasswordSalt, newPassword);
            await _context.SaveChangesAsync();

            return true;
        }

        public bool IsAdmin(string userName)
        {
            return _context.Users
                .Where(user => user.Name == userName)
                .SelectMany(user => user.Groups.AsQueryable()
                    .Where(groop => groop.IsAdmin)
                    .Select(groop => groop.Id))
                .Any();
        }
    }
}