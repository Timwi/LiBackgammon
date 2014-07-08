using System;
using RT.Servers;
using RT.Util;

namespace LiBackgammon
{
    public static class Helpers
    {
        public static string Path(this HttpRequest req, string path, Func<IHttpUrl, IHttpUrl> further = null)
        {
            return req.Url.WithPathParent().WithPathOnly(path).Apply(url => (further == null ? url : further(url)).ToHref());
        }
    }
}
