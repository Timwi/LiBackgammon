using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RT.Servers;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private DbAuthenticator _auth = new DbAuthenticator();

        private HttpResponse authenticate(HttpRequest req)
        {
            return Session.EnableManual<DbSession>(req, sess => _auth.Handle(req, sess));
        }
    }
}
