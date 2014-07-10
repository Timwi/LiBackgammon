using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private HttpResponse play(HttpRequest req)
        {
            if (req.Url.Path.Length < 9)
                return HttpResponse.Redirect(req.Path("/"));

            using (var tr = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
            using (var db = new Db())
            {
                var stuff = req.Url.Path.Substring(1);
                var publicId = stuff.Substring(0, 8);
                var game = db.Games.FirstOrDefault(g => g.PublicID == publicId);
                if (game == null)
                    return HttpResponse.Redirect(req.Path("/"));

                var playerToken = stuff.Substring(8);
                if (playerToken != "" && playerToken != game.WhiteToken && playerToken != game.BlackToken)
                    return HttpResponse.Redirect(req.Path("/play/" + game.PublicID));
                var player = playerToken == game.WhiteToken ? Player.White : playerToken == game.BlackToken ? Player.Black : Player.Spectator;

                return page(req,
                    new DIV { id = "board" }.Data("initial", game.InitialPosition).Data("moves", game.Moves).Data("state", (int) game.State).Data("player", player).Data("token", playerToken)._(
                        new[] { "left", "right" }.Select(pos => new DIV { class_ = "background main-area " + pos }),
                        new[] { "white", "black" }.Select(col => new DIV { class_ = "background home " + col }),
                        Enumerable.Range(0, 24).Select(i => new DIV { class_ = "tongue tongue-" + i + (i < 12 ? " bottom" : " top") + (i % 2 == 0 ? " light" : " dark") }.Data("tongue", i)),
                        new[] { "left", "right" }.Select(pos => new DIV { class_ = "shadow main-area " + pos }),
                        new[] { "white", "black" }.Select(col => new DIV { class_ = "shadow home " + col }),
                        Enumerable.Range(0, 15).Select(pieceNum => new[] { "white", "black" }.Select(color => new DIV { class_ = "piece " + color, id = color + "-" + pieceNum })),
                        new DIV { class_ = "overlay", id = "overlay-bottom" },
                        new DIV { class_ = "overlay", id = "overlay-right" },
                        new DIV { id = "cube" }._(new DIV { class_ = "inner" }._(new DIV { id = "cube-text" })),
                        new DIV { class_ = "dice-back", id = "dice-back-white" },
                        new DIV { class_ = "dice-back", id = "dice-back-black" },
                        Enumerable.Range(0, 4).Select(diceNum => new DIV { class_ = "dice", id = "dice-" + diceNum }._(
                            new DIV { class_ = "face" },
                            "nesw".Select(ch => new DIV { class_ = "side " + ch }),
                            "abcdefg".Select(ch => new DIV { class_ = "pip " + ch }),
                            new DIV { class_ = "cross" })),
                        new DIV { class_ = "waiting" }._(
                            new P("Send the following link to your friend to allow them to join the game:"),
                            new P { class_ = "link" }._(req.Url.WithPathOnly("/" + publicId).ToFull()),
                            new P("The game will begin when the other player joins the game.")
                        ),
                        new DIV { class_ = "joinable" }._(
                            new P("The player is waiting for an opponent to play with."),
                            new FORM { action = req.Url.WithPathOnly("/join/" + publicId).ToFull(), method = method.post }._(
                                new BUTTON { type = btype.submit }._("Join game")
                            )
                        )));
            }
        }
    }
}
