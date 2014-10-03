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
        private HttpResponse page(HttpRequest req, Tag body, params string[] jsPaths)
        {
#if DEBUG
            var jquery = req.Url.WithParent("jquery").ToHref();
#else
            var jquery = "//ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js";
#endif
            return HttpResponse.Html(
                new HTML(
                    new HEAD(
                        new TITLE("LiBackgammon"),

                        new LINK { rel = "shortcut icon", href = req.Url.WithParent("favicon").ToHref() },

                        // CSS must be above JS because the vh conversion might not trigger otherwise
                        new LINK { rel = "stylesheet", href = req.Url.WithParent("css").ToHref() },

                        new STYLE { id = "converted-css" },
                        new STYLE { id = "converted-content" },
                        new STYLE { id = "translated-content" },

                        new SCRIPT { src = jquery },
                        new SCRIPT { src = req.Url.WithParent("js").ToHref() },

                        new META { name = "viewport", content = "width=device-width, user-scalable=no" },
                        jsPaths.NullOr(jsp => jsp.Select(p => new SCRIPT { src = req.Url.WithParent(p).ToHref() }))),
                    body));
        }
    }
}
