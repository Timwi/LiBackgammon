using System;
using RT.Util;
using RT.Servers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RT.TagSoup;
using System.IO;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private HttpResponse page(HttpRequest req, Tag body, string extraJsPath, bool admin = false)
        {
            return page(req, body, new[] { extraJsPath }, admin);
        }

        private HttpResponse page(HttpRequest req, Tag body, string[] extraJsPaths, bool admin = false)
        {
            return HttpResponse.Html(
                new HTML(
                    new HEAD(
                        new TITLE("LiBackgammon"),

                        new LINK { rel = "shortcut icon", href = req.Url.WithParent("favicon").ToHref() },

                        // CSS must be above JS because the vh conversion might not trigger otherwise
                        new LINK { rel = "stylesheet", href = req.Url.WithParent("css").ToHref(), id = "main-css" },
                        admin ? new LINK { rel = "stylesheet", href = req.Url.WithParent("css/admin").ToHref() } : null,

                        new STYLE { id = "style-css" },
                        new STYLE { id = "converted-css" },
                        new STYLE { id = "converted-content" },
                        new STYLE { id = "translated-content" },
                        new STYLE { id = "translated-content-2" },

                        new SCRIPT { src = req.Url.WithParent("jquery").ToHref() },
                        new SCRIPT { src = req.Url.WithParent("js").ToHref() },

                        new META { name = "viewport", content = "width=device-width, user-scalable=no" },
                        extraJsPaths.NullOr(jsp => jsp.Select(p => new SCRIPT { src = req.Url.WithParent(p).ToHref() }))),
                    body.Data("ajax", req.Url.WithParent("ajax").ToHref())));
        }
    }
}
