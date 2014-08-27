﻿using System;
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
            // G
            // H
            // I = Invite Only
            // J
            // K
            // L
            // M
            // N = No doubling cube
            // O
            // P = Public
            // Q
            // R = Play as Random
            // S = Start game
            // T = Standard doubling cube rules
            // U
            // V
            // W = Play as White
            // X
            // Y
            // Z

            return page(req,
                new BODY { id = "main-page" }._(
                    new FORM { id = "newgame", action = req.Url.WithParent("new").ToHref(), method = method.post }._(
                        new DIV { id = "newgame-playas", class_ = "row" }._(
                            new INPUT { type = itype.radio, name = "playas", value = "random", id = "newgame-playas-random", checked_ = true },
                            new LABEL { class_ = "random", for_ = "newgame-playas-random", accesskey = "r" },
                            new INPUT { type = itype.radio, name = "playas", value = "white", id = "newgame-playas-white" },
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
                        new DIV { id = "newgame-visibility", class_ = "row" }._(
                            EnumStrong.GetValues<Visibility>().Select((vis, i) => Ut.NewArray<object>(
                                new INPUT { type = itype.radio, name = "visibility", id = "newgame-visibility-" + vis, value = vis.ToString(), checked_ = i == 0 },
                                new LABEL { for_ = "newgame-visibility-" + vis, id = "newgame-visibility-label-" + vis, accesskey = vis.GetCustomAttribute<KeyboardShortcutAttribute>().Shortcut }))),
                        new DIV { id = "newgame-submit", class_ = "row" }._(
                            new BUTTON { type = btype.submit, id = "newgame-submit-btn", accesskey = "s" })),
                    new UL { id = "waiting-games" }._(
                        new LI(new DIV { class_ = "black piece" }, new DIV { class_ = "playto" }._(15), new DIV { class_ = "doubling-cube Crawford" }))));
        }
    }
}
