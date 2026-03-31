using RT.Servers;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private HttpResponse mainSocket(HttpRequest req)
        {
            return new MainWebSocket(this);
        }
    }
}
