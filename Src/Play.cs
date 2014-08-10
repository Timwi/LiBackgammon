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
                var points =
                    game.State == GameState.Black_Won_Finished || game.State == GameState.White_Won_Finished ? (pos.GameValue ?? 1) * pos.GetWinMultiplier(game.State == GameState.White_Won_Finished) :
                    game.State == GameState.Black_Won_RejectedDouble || game.State == GameState.White_Won_RejectedDouble ? pos.GameValue ?? 1 :
                    game.State == GameState.Black_Won_Resignation || game.State == GameState.White_Won_Resignation ? (pos.GameValue ?? 1) * pos.GetWinMultiplier(game.State == GameState.White_Won_Resignation) : 0;

                var pipsWhite = Enumerable.Range(0, 24).Sum(t => pos.IsWhitePerTongue[t] ? (24 - t) * pos.NumPiecesPerTongue[t] : 0) + 25 * pos.NumPiecesPerTongue[Tongues.WhitePrison];
                var pipsBlack = Enumerable.Range(0, 24).Sum(t => !pos.IsWhitePerTongue[t] ? (t + 1) * pos.NumPiecesPerTongue[t] : 0) + 25 * pos.NumPiecesPerTongue[Tongues.BlackPrison];

                return page(req,
                    new DIV
                    {
                        id = "main",
                        class_ =
                            game.State.ToString().Split('_').Select(cl => "state-" + cl).JoinString(" ")
                            + ((game.State == GameState.Black_ToMove || game.State == GameState.White_ToMove)
                                    ? (moves.Count == 1 ? " dice-start" + (moves[0].Dice1 > moves[0].Dice2 ? " white-starts" : " black-starts") : "")
                                        + (lastMove.Dice1 == lastMove.Dice2 ? " dice-4" : " dice-2")
                                    : "")
                            + (pos.GameValue == null ? " no-cube" : pos.WhiteOwnsCube == null ? "" : pos.WhiteOwnsCube.Value ? " cube-white" : " cube-black")
                            + (player == Player.White ? " player-white" : player == Player.Black ? " player-black" : " spectating")
                    }
                        .Data("moves", game.Moves)
                        .Data("initial", game.InitialPosition)
                        .Data("player", player)
                        .Data("socket-url", Regex.Replace(req.Url.WithParent("socket/" + publicId + playerToken).ToFull(), @"^http", "ws"))
                        ._(
                            new DIV { id = "board" }._(
                                new[] { "left", "right" }.Select(loc => new DIV { class_ = "background main-area " + loc }),
                                new[] { "white", "black" }.Select(col => new DIV { class_ = "background home " + col }),
                                Enumerable.Range(0, 24).Select(i => new DIV { class_ = "tongue tongue-" + i + (i < 12 ? " bottom" : " top") + (i % 2 == 0 ? " light" : " dark") + " m3r" + (i % 3) + " m6r" + (i % 6) + " group" + (i / 6 + 1) }.Data("tongue", i)),
                                new[] { "left", "right" }.Select(loc => new DIV { class_ = "shadow main-area " + loc }),
                                new[] { "white", "black" }.Select(col => new DIV { class_ = "shadow home " + col }),
                                new BUTTON { id = "undo" },
                                new BUTTON { id = "commit" },
                                new BUTTON { id = "roll" },
                                new BUTTON { id = "double" },
                                new BUTTON { id = "accept" },
                                new BUTTON { id = "reject" },
                                new BUTTON { id = "resign-confirm" },
                                new BUTTON { id = "resign-cancel" },
                                new DIV { id = "info" },
                                new DIV { class_ = "dice-back", id = "dice-back-white" },
                                new DIV { class_ = "dice-back", id = "dice-back-black" },
                                Enumerable.Range(0, 4).Select(diceNum => new DIV { class_ = "dice" + (lastMove == null ? null : " val-" + (diceNum == 0 ? lastMove.Dice1 : lastMove.Dice2)), id = "dice-" + diceNum }._(
                                    new DIV { class_ = "razor" }._(
                                        new DIV { class_ = "face" },
                                        "nesw".Select(ch => new DIV { class_ = "side " + ch })),
                                    "abcdefg".Select(ch => new DIV { class_ = "pip " + ch }),
                                    new DIV { class_ = "cross" })),
                                new DIV { id = "cube" }._(new DIV { class_ = "inner" }._(new DIV { id = "cube-text" }._(pos.GameValue))),
                                Enumerable.Range(0, 15).Select(pieceNum => new[] { "white", "black" }.Select(color => new DIV { class_ = "piece " + color })),
                                new DIV { class_ = "overlay", id = "overlay-bottom" },
                                new DIV { class_ = "overlay", id = "overlay-right" }),
                            new DIV { id = "infobar" }._(
                                new DIV { class_ = "infobox", id = "info-player" }._(new DIV { class_ = "piece" }),
                                new DIV { class_ = "infobox", id = "info-state" }._(new DIV { class_ = "piece" }),
                                new DIV { class_ = "infobox", id = "info-pips" }._(
                                    new DIV { class_ = "infobox-inner infobox-white" }._(new DIV { class_ = "piece" }, new DIV { class_ = "number", id = "pipcount-white" }._(pipsWhite)),
                                    new DIV { class_ = "infobox-inner infobox-black" }._(new DIV { class_ = "piece" }, new DIV { class_ = "number", id = "pipcount-black" }._(pipsBlack))),
                                new DIV { class_ = "infobox", id = "info-match" }._(
                                    new DIV { class_ = "infobox-inner infobox-white" }._(new DIV { class_ = "piece" }, new DIV { class_ = "number", id = "matchscore-white" }._(0)),
                                    new DIV { class_ = "infobox-inner infobox-black" }._(new DIV { class_ = "piece" }, new DIV { class_ = "number", id = "matchscore-black" }._(0))),
                                new DIV { class_ = "mini-button", id = "resign" },
                                new DIV { class_ = "mini-button", id = "settings" },
                                new DIV { class_ = "mini-button", id = "help" },
                                new DIV { class_ = "mini-button", id = "chat" }),
                            new DIV { id = "win", class_ = "dialog" }._(
                                new DIV { class_ = "piece" },
                                new DIV { class_ = "points" + (points == 1 ? " singular" : " plural") }._(
                                    new P { class_ = "number" }._(points),
                                    new P { class_ = "word" }),
                                new P { class_ = "win" }),
                            new DIV { id = "waiting", class_ = "dialog" }._(
                                new P { id = "send-this-link" },
                                new P { class_ = "link" }._(req.Url.WithPathOnly("/" + publicId).ToFull()),
                                new P { id = "game-will-begin" }),
                            new DIV { id = "joinable", class_ = "dialog" }._(
                                new P { id = "player-waiting" },
                                new FORM { action = req.Url.WithParent("join/" + publicId).ToHref(), method = method.post }._(
                                    new BUTTON { type = btype.submit, id = "join" })),
                            new DIV { id = "connecting" }));
            }
        }
    }
}
