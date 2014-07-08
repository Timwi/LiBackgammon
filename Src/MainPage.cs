using System;
using RT.Servers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RT.TagSoup;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private HttpResponse mainPage(HttpRequest req)
        {
            return page(req,
                new UL(
                    new LI(
                        "Start a new game as: ", new A { href = req.Path("/new/white") }._("white"), " | ", new A { href = req.Path("/new/black") }._("black"))));
        }
    }
}
