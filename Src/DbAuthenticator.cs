using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using RT.Servers;

namespace LiBackgammon
{
    public sealed class DbAuthenticator : Authenticator
    {
        public DbAuthenticator()
            : base(url => url.WithParents(2, "").ToFull(), "LiBackgammon")
        {
        }

        protected override bool getUser(ref string username, out string passwordHash, out bool canCreateUsers)
        {
            passwordHash = null;
            canCreateUsers = false;

            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var un = username;  // can’t use ref parameters in lambdas
                var user = db.Users.FirstOrDefault(u => u.Username == un);
                if (user == null)
                    return false;
                username = user.Username;
                passwordHash = user.PasswordHash;
                canCreateUsers = user.Flags.HasFlag(UserFlags.CanCreateUsers);
                return true;
            }
        }

        protected override bool changePassword(string username, string newPasswordHash, Func<string, bool> verifyOldPasswordHash)
        {
            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var user = db.Users.FirstOrDefault(u => u.Username == username);
                if (user == null || !verifyOldPasswordHash(user.PasswordHash))
                    return false;
                user.PasswordHash = newPasswordHash;
                db.SaveChanges();
                tr.Complete();
                return true;
            }
        }

        protected override bool createUser(string username, string passwordHash, bool canCreateUsers)
        {
            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var user = db.Users.FirstOrDefault(u => u.Username == username);
                if (user != null)
                    return false;
                db.Users.Add(new User { Username = username, PasswordHash = passwordHash, Flags = canCreateUsers ? UserFlags.CanCreateUsers : UserFlags.None });
                db.SaveChanges();
                tr.Complete();
                return true;
            }
        }
    }
}
