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
        private HttpResponse page(HttpRequest req, object body, string[] jsPaths = null, string[] cssPaths = null)
        {
#if DEBUG
            var jquery = "/jquery";
#else
            var jquery = "//ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js";
#endif
            return HttpResponse.Html(
                new HTML(
                    new HEAD(
                        new TITLE("LiBackgammon"),
                        new SCRIPT { src = jquery },
                        new SCRIPT { src = req.Path("/js") },
                        jsPaths.NullOr(jsp => jsp.Select(p => new SCRIPT { src = req.Path(p) })),
                        new LINK { rel = "stylesheet", href = req.Path("/css") },
                        cssPaths.NullOr(cp => cp.Select(p => new LINK { rel = "stylesheet", href = req.Path(p) }))),
                    new BODY(body)));
        }
    }
}
