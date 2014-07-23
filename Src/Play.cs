using System.Linq;
using System.Text.RegularExpressions;
using System.Transactions;
using RT.Servers;
using RT.TagSoup;
using RT.Util.ExtensionMethods;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private HttpResponse play(HttpRequest req)
        {
            if (req.Url.Path.Length < 9)
                return HttpResponse.Redirect(req.Url.WithParent(""));

            using (var tr = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
            using (var db = new Db())
            {
                var stuff = req.Url.Path.Substring(1);
                var publicId = stuff.Substring(0, 8);
                var game = db.Games.FirstOrDefault(g => g.PublicID == publicId);
                if (game == null)
                    return HttpResponse.Redirect(req.Url.WithParent(""));

                var playerToken = stuff.Substring(8);
                if (playerToken != "" && playerToken != game.WhiteToken && playerToken != game.BlackToken)
                    return HttpResponse.Redirect(req.Url.WithParent("play/" + game.PublicID));
                var player = playerToken == game.WhiteToken ? Player.White : playerToken == game.BlackToken ? Player.Black : Player.Spectator;

                // Determine what the current game position is
                var pos = game.InitialPosition.ToPosition();
                var moves = game.Moves.ToMoves();
                if (moves.Count > 0)
                {
                    var whiteStarts = moves.Count > 0 && moves[0].Dice1 > moves[0].Dice2;
                    for (int i = 0; i < moves.Count; i++)
                        pos = pos.ProcessMove(whiteStarts ? (i % 2 == 0) : (i % 2 != 0), moves[i]);
                }
                var lastMove = moves.LastOrDefault();

                return page(req,
                    new DIV
                    {
                        id = "board",
                        class_ =
                            (
                                (game.State == GameState.BlackWaiting && player == Player.Black) || (game.State == GameState.WhiteWaiting && player == Player.White) ? "waiting" :
                                game.State == GameState.BlackWaiting || game.State == GameState.WhiteWaiting ? "joinable" :
                                (game.State == GameState.BlackToRoll && player == Player.Black) || (game.State == GameState.WhiteToRoll && player == Player.White) ? "roll-or-double" :
                                game.State == GameState.BlackToRoll || game.State == GameState.WhiteToRoll ? "waiting-to-roll-or-double" :
                                (game.State == GameState.BlackToConfirmDouble && player == Player.Black) || (game.State == GameState.WhiteToConfirmDouble && player == Player.White) ? "confirm-double" :
                                game.State == GameState.BlackToConfirmDouble || game.State == GameState.WhiteToConfirmDouble ? "waiting-to-confirm-double" :
                                (game.State == GameState.BlackToMove && player == Player.Black) || (game.State == GameState.WhiteToMove && player == Player.White) ? "to-move" :
                                game.State == GameState.BlackDoubledWhiteRejected ? "doubled white-wins" :
                                game.State == GameState.WhiteDoubledBlackRejected ? "doubled black-wins" :
                                game.State == GameState.BlackFinished || game.State == GameState.WhiteResigned ? "black-wins" :
                                game.State == GameState.WhiteFinished || game.State == GameState.BlackResigned ? "white-wins" :
                                game.State == GameState.BlackResigned ? "resigned white-wins" :
                                game.State == GameState.WhiteResigned ? "resigned black-wins" :
                                null
                            ) + (
                                (game.State == GameState.BlackToMove || game.State == GameState.WhiteToMove)
                                    ? (moves.Count == 1 ? " dice-start" + (moves[0].Dice1 > moves[0].Dice2 ? " white-starts" : " black-starts") : "")
                                        + (lastMove.Dice1 == lastMove.Dice2 ? " dice-4" : " dice-2")
                                    : ""
                            ) + (
                                pos.GameValue == null ? " no-cube" : pos.WhiteOwnsCube == null ? "" : pos.WhiteOwnsCube.Value ? " cube-white" : " cube-black"
                            )
                    }
                        .Data("moves", game.Moves)
                        .Data("initial", game.InitialPosition)
                        .Data("state", (int) game.State)
                        .Data("player", player)
                        .Data("socket-url", Regex.Replace(req.Url.WithParent("socket/" + publicId + playerToken).ToFull(), @"^http", "ws"))
                    ._(
                        new[] { "left", "right" }.Select(loc => new DIV { class_ = "background main-area " + loc }),
                        new[] { "white", "black" }.Select(col => new DIV { class_ = "background home " + col }),
                        Enumerable.Range(0, 24).Select(i => new DIV { class_ = "tongue tongue-" + i + (i < 12 ? " bottom" : " top") + (i % 2 == 0 ? " light" : " dark") + " m3r" + (i % 3) + " m6r" + (i % 6) + " group" + (i / 4 + 1) }.Data("tongue", i)),
                        new[] { "left", "right" }.Select(loc => new DIV { class_ = "shadow main-area " + loc }),
                        new[] { "white", "black" }.Select(col => new DIV { class_ = "shadow home " + col }),
                        new BUTTON { id = "undo" }._("Undo"),
                        new BUTTON { id = "commit" }._("Commit"),
                        new BUTTON { id = "roll" }._("Roll"),
                        new BUTTON { id = "double" }._("Double"),
                        new BUTTON { id = "accept" }._("Accept"),
                        new BUTTON { id = "reject" }._("Reject"),
                        Enumerable.Range(0, 4).Select(diceNum => new DIV { class_ = "dice" + (lastMove == null ? null : " val-" + (diceNum == 0 ? lastMove.Dice1 : lastMove.Dice2)), id = "dice-" + diceNum }._(
                            new DIV { class_ = "razor" }._(
                                new DIV { class_ = "face" },
                                "nesw".Select(ch => new DIV { class_ = "side " + ch })),
                            "abcdefg".Select(ch => new DIV { class_ = "pip " + ch }),
                            new DIV { class_ = "cross" })),
                        new DIV { id = "cube" }._(new DIV { class_ = "inner" }._(new DIV { id = "cube-text" })),
                        Enumerable.Range(0, 15).Select(pieceNum => new[] { "white", "black" }.Select(color => new DIV { class_ = "piece " + color, id = color + "-" + pieceNum })),
                        new DIV { class_ = "overlay", id = "overlay-bottom" },
                        new DIV { class_ = "overlay", id = "overlay-right" },
                        new DIV { class_ = "dice-back", id = "dice-back-white" },
                        new DIV { class_ = "dice-back", id = "dice-back-black" },
                        new DIV { id = "win", class_ = "dialog" }._(
                            new DIV { class_ = "white piece" },
                            new DIV { class_ = "black piece" },
                            new DIV { class_ = "points" }._(
                                new P { class_ = "number" },
                                new P { class_ = "word" }
                            ),
                            new P { class_ = "win white" }._("White wins"),
                            new P { class_ = "win black" }._("Black wins"),
                            new P { class_ = "rejected white" }._("because black rejected a double from white."),
                            new P { class_ = "rejected black" }._("because white rejected a double from black.")),
                        new DIV { id = "waiting", class_ = "dialog" }._(
                            new P("Send the following link to your friend to allow them to join the game:"),
                            new P { class_ = "link" }._(req.Url.WithPathOnly("/" + publicId).ToFull()),
                            new P("The game will begin when the other player joins the game.")),
                        new DIV { id = "joinable", class_ = "dialog" }._(
                            new P("The player is waiting for an opponent to play with."),
                            new FORM { action = req.Url.WithParent("join/" + publicId).ToHref(), method = method.post }._(
                                new BUTTON { type = btype.submit }._("Join game"))),
                        new DIV { id = "connecting" }._("Reconnecting...")));
            }
        }
    }
}
