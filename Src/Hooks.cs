using System;
using System.IO;
using System.Text.RegularExpressions;
using RT.Servers;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private UrlResolver makeResolver()
        {
            var auth = new DbAuthenticator();
            var ajax = new AjaxHandler<LiBackgammonAjax>(
#if DEBUG
AjaxHandlerOptions.PropagateExceptions
#else
AjaxHandlerOptions.ReturnExceptionsWithoutMessages
#endif
);
            var ajaxObj = new LiBackgammonAjax();

            return new UrlResolver(

#if DEBUG
                //
                new UrlMapping(path: "/jquery", specificPath: true, handler: getFileResourceHandler(@"Resources\JQuery.js", "text/javascript", HttpResponse.Redirect(@"https://ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js"))),
#endif
                //
                new UrlMapping(path: "/", specificPath: true, handler: mainPage),
                new UrlMapping(path: "/socket/main", handler: mainSocket),
                new UrlMapping(path: "/new", handler: newGame),
                new UrlMapping(path: "/play", handler: play),
                new UrlMapping(path: "/socket/play", handler: playSocket),
                new UrlMapping(path: "/join", handler: join),
                new UrlMapping(path: "/admin", handler: admin),
                new UrlMapping(path: "/auth", handler: authenticate),
                new UrlMapping(path: "/css", specificPath: true, handler: getFileResourceHandler(@"Resources\Backgammon.css", "text/css", HttpResponse.Css(Regex.Replace(Resources.Css, @"\s+", " ", RegexOptions.Singleline).ToUtf8()))),
                new UrlMapping(path: "/css/admin", specificPath: true, handler: getFileResourceHandler(@"Resources\Admin.css", "text/css", HttpResponse.Css(Regex.Replace(Resources.CssAdmin, @"\s+", " ", RegexOptions.Singleline).ToUtf8()))),
                new UrlMapping(path: "/js", specificPath: true, handler: getFileResourceHandler(@"Resources\Backgammon.js", "text/javascript", HttpResponse.JavaScript(JsonValue.Fmt(Resources.Js).ToUtf8()))),
                new UrlMapping(path: "/js/play", specificPath: true, handler: getFileResourceHandler(@"Resources\Play.js", "text/javascript", HttpResponse.JavaScript(JsonValue.Fmt(Resources.JsPlay).ToUtf8()))),
                new UrlMapping(path: "/js/main", specificPath: true, handler: getFileResourceHandler(@"Resources\Main.js", "text/javascript", HttpResponse.JavaScript(JsonValue.Fmt(Resources.JsMain).ToUtf8()))),
                new UrlMapping(path: "/js/admin", specificPath: true, handler: getFileResourceHandler(@"Resources\Admin.js", "text/javascript", HttpResponse.JavaScript(JsonValue.Fmt(Resources.JsAdmin).ToUtf8()))),
                new UrlMapping(path: "/ajax", handler: req => ajax.Handle(req, ajaxObj)),

#if DEBUG
                //
                new UrlMapping(path: "/favicon", specificPath: true, handler: getFileResourceHandler(@"Resources\FaviconDebug.png", "image/png", HttpResponse.Create(Resources.FaviconDebug, "image/png")))
#else
                //
                new UrlMapping(path: "/favicon", specificPath: true, handler: getFileResourceHandler(@"Resources\Favicon.png", "image/png", HttpResponse.Create(Resources.Favicon, "image/png")))
#endif
                //
            );
        }

        private static Func<HttpRequest, HttpResponse> getFileResourceHandler(string path, string contentType, HttpResponse releaseResponse)
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
