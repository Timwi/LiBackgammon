﻿using System;
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
            var js = jsmin(Resources.MainJs);

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
                new UrlMapping(path: "/css", specificPath: true, handler: req => Program.IsDebug ? HttpResponse.File(Path.Combine(Program.SourceDir, @"Resources\Main.css"), "text/css") : HttpResponse.Css(Resources.MainCss)),
                new UrlMapping(path: "/js", specificPath: true, handler: req => Program.IsDebug ? HttpResponse.File(Path.Combine(Program.SourceDir, @"Resources\Main.js"), "text/javascript") : HttpResponse.JavaScript(js)),
                new UrlMapping(path: "/socket", handler: socket)
            );
        }

        private static byte[] jsmin(string js)
        {
            return JsonValue.Fmt(js).ToUtf8();
        }
    }
}
