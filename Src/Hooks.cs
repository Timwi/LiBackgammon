using System;
using RT.Util.ExtensionMethods;
using RT.Servers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RT.Util.Json;
using System.IO;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private UrlResolver makeResolver()
        {
            return new UrlResolver(

#if DEBUG
                //
                new UrlMapping(path: "/jquery", specificPath: true, handler: req => HttpResponse.File(@"D:\c\users\timwi\KIA\Files\jquery.min.js", "text/javascript")),
#endif
                //
                new UrlMapping(path: "/", specificPath: true, handler: mainPage),
                new UrlMapping(path: "/new", handler: newGame),
                new UrlMapping(path: "/play", handler: play),
                new UrlMapping(path: "/join", handler: join),
                new UrlMapping(path: "/css", specificPath: true, handler: getFileResourceHandler(@"Resources\Main.css", "text/css", HttpResponse.Css(Resources.MainCss))),
                new UrlMapping(path: "/js", specificPath: true, handler: getFileResourceHandler(@"Resources\Main.js", "text/javascript", HttpResponse.JavaScript(JsonValue.Fmt(Resources.MainJs).ToUtf8()))),
                new UrlMapping(path: "/socket", handler: socket)
            );
        }

        public static Func<HttpRequest, HttpResponse> getFileResourceHandler(string path, string contentType, HttpResponse releaseResponse)
        {
#if DEBUG
            if (Program.SourceDir != null)
                return req => HttpResponse.File(Path.Combine(Program.SourceDir, path), contentType);
            return req => releaseResponse;
#else
            return req => releaseResponse;
#endif
        }
    }
}
