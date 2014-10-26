using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RT.Servers;
using RT.TagSoup;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private DbAuthenticator _authenticator = new DbAuthenticator();

        private HttpResponse admin(HttpRequest req)
        {
            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                return Session.EnableManual<DbSession>(req, sess =>
                {
                    if (sess.LoggedInUserId == null)
                        throw new HttpException(HttpStatusCode._401_Unauthorized);

                    var user = db.Users.FirstOrDefault(u => u.UserID == sess.LoggedInUserId.Value);
                    if (user == null || (user.Flags & (UserFlags.CanApproveStyles | UserFlags.CanApproveTranslations)) == 0)
                        throw new HttpException(HttpStatusCode._401_Unauthorized);

                    return page(req,
                        new BODY()
                    );

                    var styles = db.Styles.ToArray();
                    var languages = db.Languages.ToArray();
                });
            }
        }
    }
}
