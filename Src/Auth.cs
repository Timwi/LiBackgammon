using RT.Servers;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private DbAuthenticator _auth = new();

        private HttpResponse authenticate(HttpRequest req)
        {
            return Session.EnableManual<DbSession>(req, sess => _auth.Handle(req, sess));
        }
    }
}
