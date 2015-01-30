using System.Linq;
using System.Text.RegularExpressions;
using System.Transactions;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private HttpResponse play(HttpRequest req)
        {
            if (req.Url.Path.Length < 9)
                return HttpResponse.Redirect(req.Url.WithParent(""));

            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var stuff = req.Url.Path.Substring(1);
                var publicId = stuff.Substring(0, 8);
                var game = db.Games.FirstOrDefault(g => g.PublicID == publicId);
                if (game == null)
                    return HttpResponse.Redirect(req.Url.WithParent(""));
                var match = game.Match.NullOr(id => db.Matches.FirstOrDefault(m => m.ID == id));

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
                        pos = pos.ProcessMove(i % 2 == (whiteStarts ? 0 : 1), moves[i]);
                }
                var lastMove = moves.LastOrDefault();
                var points =
                    game.State == GameState.Black_Won_Finished || game.State == GameState.White_Won_Finished ? (pos.GameValue ?? 1) * pos.GetWinMultiplier(game.State == GameState.White_Won_Finished) :
                    game.State == GameState.Black_Won_RejectedDouble || game.State == GameState.White_Won_RejectedDouble ? pos.GameValue ?? 1 :
                    game.State == GameState.Black_Won_Resignation || game.State == GameState.White_Won_Resignation ? (pos.GameValue ?? 1) * pos.GetWinMultiplier(game.State == GameState.White_Won_Resignation) : 0;

                var pipsWhite = Enumerable.Range(0, 24).Sum(t => pos.IsWhitePerTongue[t] ? (24 - t) * pos.NumPiecesPerTongue[t] : 0) + 25 * pos.NumPiecesPerTongue[Tongues.WhitePrison];
                var pipsBlack = Enumerable.Range(0, 24).Sum(t => !pos.IsWhitePerTongue[t] ? (t + 1) * pos.NumPiecesPerTongue[t] : 0) + 25 * pos.NumPiecesPerTongue[Tongues.BlackPrison];

                var whiteMatchScore = game.Match.NullOr(m => db.Games.Where(g => g.Match == m && g.GameInMatch <= game.GameInMatch).Select(g => g.WhiteScore).DefaultIfEmpty().Sum());
                var blackMatchScore = game.Match.NullOr(m => db.Games.Where(g => g.Match == m && g.GameInMatch <= game.GameInMatch).Select(g => g.BlackScore).DefaultIfEmpty().Sum());

                var nextGame = game.NextGame.NullOr(ng => db.Games.FirstOrDefault(g => g.PublicID == ng));

                var history = match.NullOr(m => db.Games.Where(g => g.Match == m.ID).OrderBy(g => g.GameInMatch).ToList());
                var unreadChatMsgs = player == Player.Spectator ? 0 : db.ChatMessages.Where(ch => ch.GameID == publicId && ch.Player != player && !ch.Seen).Count();

                // Keyboard Shortcuts
                // ────────────
                // A = Accept double
                // B = Accept rematch
                // C = Commit
                // D = Double
                // E = Reject double
                // F = Reject rematch
                // G = Resign
                // H = Show/hide move helpers
                // I = Match info, rules and help
                // J = Join game
                // K = Back to game (leave history)
                // L = Language
                // M
                // N = Resign cancel
                // O = Offer a rematch
                // P
                // Q
                // R = Roll
                // S = Settings
                // T = Chat
                // U = Undo
                // V
                // W = Style
                // X = Go to next game
                // Y = Resign confirm
                // Z

                lock (ActivePlaySockets)
                    return page(req,
                        new BODY(
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
                                    + (game.State == GameState.Random_Waiting && player != Player.Spectator ? " player-random" : player == Player.White ? " player-white" : player == Player.Black ? " player-black" : " spectating")
                                    + match.NullOr(m => " in-match" + (whiteMatchScore >= match.MaxScore || blackMatchScore >= match.MaxScore ? " end-of-match" : null))
                                    + nextGame.NullOr(ng => " has-next-game")
                                    + (game.RematchOffer != RematchOffer.None ? " rematch-" + game.RematchOffer : null)
                                    + (player == Player.White || (ActivePlaySockets.ContainsKey(publicId) && ActivePlaySockets[publicId].Any(s => s.Player == Player.White)) ? " online-White" : null)
                                    + (player == Player.Black || (ActivePlaySockets.ContainsKey(publicId) && ActivePlaySockets[publicId].Any(s => s.Player == Player.Black)) ? " online-Black" : null)
#if DEBUG
 + " debug"
#endif
                            }
                                .Data("moves", game.Moves)
                                .Data("initial", game.InitialPosition)
                                .Data("player", player)
                                .Data("socket-url", Regex.Replace(req.Url.WithParent("socket/play/" + publicId + playerToken).ToFull(), @"^http", "ws"))
                                .Data("next-game", nextGame.NullOr(ng => req.Url.WithParent("play/" + ng.PublicID + (player == Player.White ? ng.WhiteToken : player == Player.Black ? ng.BlackToken : null)).ToFull()))
                                ._(
                                    new DIV { id = "board" }._(
                                        new[] { "left", "right" }.Select(loc => new DIV { class_ = "background main-area " + loc }),
                                        new[] { "white", "black" }.Select(col => new DIV { class_ = "background home " + col }),
                                        Enumerable.Range(0, 24).Select(i => new DIV { class_ = "tongue tongue-" + i + (i < 12 ? " bottom" : " top") + (i % 2 == 0 ? " light" : " dark") + " m3r" + (i % 3) + " m6r" + (i % 6) + " group" + (i / 6 + 1) }.Data("tongue", i)),
                                        new[] { "left", "right" }.Select(loc => new DIV { class_ = "shadow main-area " + loc }),
                                        new[] { "white", "black" }.Select(col => new DIV { class_ = "shadow home " + col }),
                                        new BUTTON { accesskey = "u", id = "undo" },
                                        new BUTTON { accesskey = "c", id = "commit" },
                                        new BUTTON { accesskey = "r", id = "roll" },
                                        new BUTTON { accesskey = "d", id = "double" },
                                        new BUTTON { accesskey = "a", id = "accept" },
                                        new BUTTON { accesskey = "e", id = "reject" },
                                        new BUTTON { accesskey = "y", id = "resign-confirm" },
                                        new BUTTON { accesskey = "n", id = "resign-cancel" },
                                        new BUTTON { accesskey = "k", id = "leave-history" },
                                        new DIV { id = "info-line" },
                                        Enumerable.Range(0, 4).Select(diceNum => new DIV { class_ = "dice" + (lastMove == null ? null : " val-" + (diceNum == 0 ? lastMove.Dice1 : lastMove.Dice2)), id = "dice-" + diceNum }._(
                                            new DIV { class_ = "razor" }._(
                                                new DIV { class_ = "face" },
                                                "nesw".Select(ch => new DIV { class_ = "side " + ch })),
                                            "abcdefg".Select(ch => new DIV { class_ = "pip " + ch }),
                                            new DIV { class_ = "cross" })),
                                        new DIV { id = "cube" }._(new DIV { id = "cube-text" }._(pos.GameValue)),
                                        Enumerable.Range(0, 15).Select(pieceNum => new[] { "white", "black" }.Select(color => new DIV { class_ = "piece " + color })),
                                        new DIV { class_ = "overlay", id = "overlay-bottom" },
                                        new DIV { class_ = "overlay", id = "overlay-right" }),
                                    new DIV { id = "infobar" }._(
                                        new DIV { class_ = "infobox", id = "info-player" }._(new DIV { class_ = "piece" }),
                                        new DIV { class_ = "infobox", id = "info-state" }._(new DIV { class_ = "piece" }),
                                        new DIV { class_ = "infobox", id = "info-pips" }._(
                                            new DIV { class_ = "infobox-inner infobox-white" }._(new DIV { class_ = "piece" }, new DIV { class_ = "number", id = "pipcount-white" }._(pipsWhite)),
                                            new DIV { class_ = "infobox-inner infobox-black" }._(new DIV { class_ = "piece" }, new DIV { class_ = "number", id = "pipcount-black" }._(pipsBlack))),
                                        game.Match == null ? null : new DIV { class_ = "infobox", id = "info-match-score" }._(
                                            new DIV { class_ = "infobox-inner infobox-white" }._(new DIV { class_ = "piece" }, new DIV { class_ = "number matchscore-white", id = "matchscore-white" }._(whiteMatchScore)),
                                            new DIV { class_ = "infobox-inner infobox-black" }._(new DIV { class_ = "piece" }, new DIV { class_ = "number matchscore-black", id = "matchscore-black" }._(blackMatchScore))),
                                        new A { href = "#", class_ = "mini-button", accesskey = "g", id = "btn-resign" },
                                        new A { href = "#", class_ = "mini-button", accesskey = "s", id = "btn-settings" },
                                        new A { href = "#", class_ = "mini-button", accesskey = "i", id = "btn-info" },
                                        new A { href = "#", class_ = "mini-button", accesskey = "t", id = "btn-chat" }._(
                                            new SPAN { class_ = "notification" + (unreadChatMsgs > 0 ? " shown" : "") }._(new SPAN { class_ = "notification-inner" }._(unreadChatMsgs)))),
                                    new DIV { id = "win", class_ = "dialog" }._(
                                        new DIV { class_ = "piece" },
                                        new DIV { class_ = "points" + (points == 1 ? " singular" : " plural") }._(
                                            new P { class_ = "number" }._(points),
                                            new P { class_ = "word" }),
                                        new P { class_ = "win" },
                                        new P { id = "next-game" }._(
                                            new SPAN { id = "next-game-text" },
                                            new BUTTON { id = "offer-rematch", accesskey = "o" },
                                            new BUTTON { id = "accept-rematch", accesskey = "b" },
                                            new BUTTON { id = "cancel-rematch", accesskey = "f" },
                                            new BUTTON { id = "goto-next-game", accesskey = "x" })),
                                    new DIV { id = "waiting", class_ = "dialog" }._(
                                        new P { id = "send-this-link" },
                                        new P { class_ = "link" }._(req.Url.WithPathOnly("/" + publicId).ToFull()),
                                        new P { id = "game-will-begin" }),
                                    new DIV { id = "joinable", class_ = "dialog" }._(
                                        new P { id = "player-waiting" },
                                        new FORM { action = req.Url.WithParent("join/" + publicId).ToHref(), method = method.post }._(
                                            new BUTTON { type = btype.submit, id = "join", accesskey = "j" })),
                                    new DIV { id = "connecting" },
                                    new DIV { id = "sidebar" }._(
                                        new DIV { id = "chat", class_ = "sidebar-tab" }._(
                                            new DIV { id = "chat-msgs-outer" }._(new DIV { id = "chat-msgs" }),
                                            new LABEL { for_ = "chat-msg", accesskey = "," },
                                            new INPUT { id = "chat-msg", type = itype.text }),
                                        new DIV { id = "info", class_ = "sidebar-tab" }._(
                                            new DIV { id = "info-game-history", class_ = "section" },
                                            new DIV { id = "info-match-history", class_ = "section match" }._(
                                                history.NullOr(h => Ut.NewArray<object>(
                                                    h.Select((g, i) => new A
                                                    {
                                                        class_ = "row game" + (g.HasDoublingCube ? " cube" : " no-cube") + (i == 0 ? " first" : "") + (i == history.Count - 1 ? " last" : ""),
                                                        href = g.PublicID == publicId ? null :
                                                            req.Url.WithParent("play/" + g.PublicID + (player == Player.White ? g.WhiteToken : player == Player.Black ? g.BlackToken : null)).ToFull()
                                                    }._(
                                                        new DIV { class_ = "piece white" }._(new DIV { class_ = "number" }._(g.WhiteScore == 0 ? null : g.WhiteScore.ToString())),
                                                        new DIV { class_ = "piece black" }._(new DIV { class_ = "number" }._(g.BlackScore == 0 ? null : g.BlackScore.ToString())))),
                                                    new HR(),
                                                    new DIV { class_ = "row totals" }._(
                                                        new DIV { class_ = "piece white" }._(new DIV { class_ = "number" }._(history.Sum(g => g.WhiteScore))),
                                                        new DIV { class_ = "piece black" }._(new DIV { class_ = "number" }._(history.Sum(g => g.BlackScore))))))),
                                            new DIV { id = "info-match-playto", class_ = "section match" }._(new DIV { class_ = "content" }._(match.NullOr(m => m.MaxScore))),
                                            new DIV { id = "info-match-cube", class_ = "section match" + match.NullOr(m => " " + m.DoublingCubeRules) }._(new DIV { class_ = "content" })),
                                        new DIV { id = "settings", class_ = "sidebar-tab" }._(
                                            new DIV { id = "settings-style", class_ = "section" }._(
                                                new LABEL { for_ = "settings-style-select", accesskey = "w" },
                                                new SELECT { id = "settings-style-select" }._(new OPTION("Loading..."))),
                                            new DIV { id = "settings-lang", class_ = "section" }._(
                                                new LABEL { for_ = "settings-lang-select", accesskey = "l" },
                                                new SELECT { id = "settings-lang-select" }._(new OPTION("Loading...")),
                                                new BUTTON { id = "settings-lang-custom" }),
                                            new DIV { id = "settings-helpers", class_ = "section" }._(
                                                new INPUT { id = "settings-helpers-select", type = itype.checkbox },
                                                new LABEL { for_ = "settings-helpers-select", accesskey = "h" },
                                                new INPUT { id = "settings-percentages-select", type = itype.checkbox },
                                                new LABEL { for_ = "settings-percentages-select" },
                                                new INPUT { id = "settings-autoroll-select", type = itype.checkbox },
                                                new LABEL { for_ = "settings-autoroll-select" })),
                                        new DIV { id = "translate", class_ = "sidebar-tab" }._(
                                            new DIV { id = "translate-existing", class_ = "section" }._(
                                                new SELECT { id = "translate-select" },
                                                new BUTTON { id = "translate-edit" }),
                                            new DIV { id = "translate-new", class_ = "section" }._(
                                                new DIV { id = "translate-error" },
                                                new LABEL { for_ = "translate-name", id = "translate-name-label" },
                                                new INPUT { id = "translate-name" },
                                                new LABEL { for_ = "translate-code", id = "translate-code-label" },
                                                new INPUT { id = "translate-code" },
                                                new BUTTON { id = "translate-create" })),
                                        new DIV { id = "translating", class_ = "sidebar-tab" },
                                        new DIV { id = "translating-link" }._(new A { href = "#" }._("back to translating →"))))),
                        "js/play");
            }
        }
    }
}
