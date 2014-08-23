using System;
using RT.Servers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RT.TagSoup;
using RT.Util;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private HttpResponse mainPage(HttpRequest req)
        {
            // Keyboard Shortcuts
            // ────────────
            // A
            // B = Play as Black
            // C = Crawford rules
            // D
            // E
            // F
            // G = Start game
            // H
            // I
            // J
            // K
            // L
            // M
            // N = No doubling cube
            // O
            // P
            // Q
            // R
            // S = Standard doubling cube rules
            // T
            // U
            // V
            // W = Play as White
            // X
            // Y
            // Z

            return page(req,
                new BODY(
                    new FORM { id = "newgame", action = req.Url.WithParent("new").ToHref(), method = method.post }._(
                        new DIV { id = "newgame-playas", class_ = "row" }._(
                            new INPUT { type = itype.radio, name = "playas", value = "white", id = "newgame-playas-white", checked_ = true },
                            new LABEL { class_ = "white", for_ = "newgame-playas-white", accesskey = "w" },
                            new INPUT { type = itype.radio, name = "playas", value = "black", id = "newgame-playas-black" },
                            new LABEL { class_ = "black", for_ = "newgame-playas-black", accesskey = "b" }),
                        new DIV { id = "newgame-playto", class_ = "row" }._(
                            Enumerable.Range(0, 5).Select(i => Ut.NewArray<object>(
                                new INPUT { type = itype.radio, name = "playto", id = "newgame-playto-" + i, value = (i + 1).ToString(), checked_ = i == 0 },
                                new LABEL { for_ = "newgame-playto-" + i, id = "newgame-playto-label-" + i, accesskey = (i + 1).ToString() }._(new SPAN { class_ = "text" }._(i + 1))))),
                        new DIV { id = "newgame-cube", class_ = "row" }._(
                            EnumStrong.GetValues<DoublingCubeRules>().Select((rule, i) => Ut.NewArray<object>(
                                new INPUT { type = itype.radio, name = "cube", id = "newgame-cube-" + rule, value = rule.ToString(), checked_ = i == 0 },
                                new LABEL { for_ = "newgame-cube-" + rule, id = "newgame-cube-label-" + rule, accesskey = rule.GetCustomAttribute<KeyboardShortcutAttribute>().Shortcut }))),
                        new DIV { id = "newgame-submit", class_ = "row" }._(
                            new BUTTON { type = btype.submit, id = "newgame-submit-btn", accesskey = "s" }))));
        }
    }
}
