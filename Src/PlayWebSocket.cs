using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Transactions;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;
using RT.Util.Serialization;

namespace LiBackgammon
{
    public sealed class PlayWebSocket : WebSocket
    {
        public string GameId { get; private set; }
        public int? MatchId { get; private set; }
        private Player _player;
        private LiBackgammonPropellerModule _server;
        private IHttpUrl _url;

        public Player Player { get { return _player; } }

        public PlayWebSocket(LiBackgammonPropellerModule server, string gameId, int? matchId, Player player, IHttpUrl url)
        {
            _server = server;
            GameId = gameId;
            MatchId = matchId;
            _player = player;
            _url = url;
        }

        protected override void onBeginConnection()
        {
            _server.AddGameSocket(this);
            var sockets = _server.GetSocketsByGame(GameId);

            if (sockets != null)
            {
                foreach (var socket in sockets)
                {
                    if (_player != Player.Spectator)
                        socket.SendMessage(new JsonDict { { "on", _player.ToString() } });
                    if (socket.Player != Player.Spectator)
                        SendMessage(new JsonDict { { "on", socket.Player.ToString() } });
                }
            }

            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                IQueryable<ChatMessage> chatMsgs;
                if (MatchId != null)
                    chatMsgs = from cm in db.ChatMessages
                               join g in db.Games on cm.GameID equals g.PublicID
                               where g.Match != null && g.Match == MatchId.Value
                               select cm;
                else
                    chatMsgs = db.ChatMessages.Where(cm => cm.GameID == GameId);

                var chatList = chatMsgs.OrderBy(cm => cm.Time).AsEnumerable().Select(chatMessageJson).ToJsonList();
                if (chatList.Count > 0)
                    SendMessage(new JsonDict { { "chat", chatList } });
            }
        }

        protected override void onEndConnection()
        {
            _server.RemoveGameSocket(this);

            if (_player != Player.Spectator)
            {
                var sockets = _server.GetSocketsByGame(GameId);
                if (sockets != null && !sockets.Any(s => s.Player == _player))
                {
                    var info = new JsonDict { { "off", _player.ToString() } }.ToString().ToUtf8();
                    foreach (var socket in sockets)
                        socket.SendMessage(1, info);
                }
            }
        }

        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
        sealed class SocketMethodAttribute : Attribute
        {
            public bool SpectatorAllowed { get; private set; }
            public SocketMethodAttribute(bool spectatorAllowed = false)
            {
                SpectatorAllowed = spectatorAllowed;
            }
        }

        private sealed class MessageInfo
        {
            public JsonDict Message { get; private set; }
            public Func<PlayWebSocket, bool> Predicate { get; private set; }
            public bool SendToAllGamesInMatch { get; private set; }

            public MessageInfo(JsonDict message, Func<PlayWebSocket, bool> predicate = null, bool sendToAllGamesInMatch = false)
            {
                Message = message;
                Predicate = predicate;
                SendToAllGamesInMatch = sendToAllGamesInMatch;
            }
        }

        [SocketMethod]
        private MessageInfo[] move(int[] sourceTongues, int[] targetTongues)
        {
            return processGameState((db, game, pos, whiteToPlay, moves) =>
            {
                // Make sure it is actually this player’s turn
                if (whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.White_ToMove : GameState.Black_ToMove))
                    return null;

                // Find all valid moves and see if the provided move is one of them
                var lastMove = moves.Last();
                var validMoves = pos.GetAllValidMoves(whiteToPlay, lastMove.Dice1, lastMove.Dice2);
                var validMove = validMoves.FirstOrDefault(vm => vm.SourceTongues.SequenceEqual(sourceTongues) && vm.TargetTongues.SequenceEqual(targetTongues));
                if (validMove == null)
                    return null;

                // The move is valid. 
                lastMove.SourceTongues = sourceTongues;
                lastMove.TargetTongues = targetTongues;
                return new MessageInfo(new JsonDict { { "move", new JsonDict { { "sourceTongues", sourceTongues }, { "targetTongues", targetTongues } } } }, s => s != this)
                    .Concat(continueGame(db, pos.ProcessMove(whiteToPlay, lastMove), game, whiteToPlay, moves))
                    .ToArray();
            });
        }

        [SocketMethod]
        private MessageInfo[] roll()
        {
            return processGameState((db, game, pos, whiteToPlay, moves) =>
            {
                // Make sure it is actually this player’s turn, and the game is played with a doubling cube (otherwise the players do not manually roll)
                // Note “whiteToPlay” is currently off because the dice roll doesn’t have an entry in “moves” yet — hence the not
                if (pos.GameValue == null || !whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.White_ToRoll : GameState.Black_ToRoll))
                    return null;

                // The actual dice rolling happens inside of continueGame.
                return continueGame(db, pos, game, whiteToPlay, moves, rolled: true).ToArray();
            });
        }

        [SocketMethod]
        private MessageInfo[] @double()
        {
            return processGameState((db, game, pos, whiteToPlay, moves) =>
            {
                // Make sure it’s this player’s turn to choose whether to double or roll, and the game is played with a doubling cube
                // Note “whiteToPlay” is currently off because the double doesn’t have an entry in “moves” yet — hence the not
                if (pos.GameValue == null || !whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.White_ToRoll : GameState.Black_ToRoll))
                    return null;
                game.State = _player == Player.White ? GameState.Black_ToConfirmDouble : GameState.White_ToConfirmDouble;
                return new[] { new MessageInfo(new JsonDict { { "state", game.State.ToString() } }) };
            });
        }

        [SocketMethod]
        private MessageInfo[] accept()
        {
            return processGameState((db, game, pos, whiteToPlay, moves) =>
            {
                // Make sure it’s this player’s turn to respond to a double
                if (pos.GameValue == null || whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.White_ToConfirmDouble : GameState.Black_ToConfirmDouble))
                    return null;

                pos.GameValue = pos.GameValue.Value * 2;

                // The game continues with a dice roll, which happens inside of continueGame.
                return new MessageInfo(new JsonDict { { "cube", new JsonDict { { "gameValue", pos.GameValue.Value }, { "whiteOwnsCube", _player == Player.White } } } })
                    .Concat(continueGame(db, pos, game, whiteToPlay, moves, acceptedDouble: true))
                    .ToArray();
            });
        }

        [SocketMethod]
        private MessageInfo[] decline()
        {
            return processGameState((db, game, pos, whiteToPlay, moves) =>
            {
                // Make sure it’s this player’s turn to respond to a double, and the game is played with a doubling cube
                if (pos.GameValue == null || whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.White_ToConfirmDouble : GameState.Black_ToConfirmDouble))
                    return null;
                game.State = _player == Player.White ? GameState.Black_Won_DeclinedDouble : GameState.White_Won_DeclinedDouble;
                return gameOver(db, game, pos, game.State == GameState.White_Won_DeclinedDouble, useMultiplier: false).ToArray();
            });
        }

        [SocketMethod]
        private MessageInfo[] resign()
        {
            return processGameState((db, game, pos, whiteToPlay, moves) =>
            {
                // You can’t resign if the game is already over
                if (game.State == GameState.Black_Won_Finished || game.State == GameState.Black_Won_DeclinedDouble || game.State == GameState.Black_Won_Resignation ||
                    game.State == GameState.White_Won_Finished || game.State == GameState.White_Won_DeclinedDouble || game.State == GameState.White_Won_Resignation)
                    return null;
                game.State = _player == Player.White ? GameState.Black_Won_Resignation : GameState.White_Won_Resignation;
                return gameOver(db, game, pos, game.State == GameState.White_Won_Resignation, useMultiplier: true).ToArray();
            });
        }

        [SocketMethod]
        private MessageInfo[] rematch()
        {
            return processGameState((db, game, pos, whiteToPlay, moves) =>
            {
                if (game.RematchOffer != RematchOffer.None &&
                    !(game.RematchOffer == RematchOffer.WhiteDeclined && _player == Player.White) &&
                    !(game.RematchOffer == RematchOffer.BlackDeclined && _player == Player.Black))
                    return null;

                var isMatchOver = (
                        game.State == GameState.Black_Won_Finished ||
                        game.State == GameState.Black_Won_DeclinedDouble ||
                        game.State == GameState.Black_Won_Resignation ||
                        game.State == GameState.White_Won_Finished ||
                        game.State == GameState.White_Won_DeclinedDouble ||
                        game.State == GameState.White_Won_Resignation
                    ) && (
                        game.Match == null ||
                        db.Matches.Where(m => m.ID == game.Match && (
                            db.Games.Where(g => g.Match == m.ID && g.GameInMatch < game.GameInMatch).Select(g => g.WhiteScore).DefaultIfEmpty().Sum() + game.WhiteScore >= m.MaxScore ||
                            db.Games.Where(g => g.Match == m.ID && g.GameInMatch < game.GameInMatch).Select(g => g.BlackScore).DefaultIfEmpty().Sum() + game.BlackScore >= m.MaxScore)).Any());
                if (!isMatchOver)
                    return null;

                game.RematchOffer = _player == Player.White ? RematchOffer.White : RematchOffer.Black;
                return new[] { new MessageInfo(new JsonDict { { "rematch", game.RematchOffer.ToString() } }) };
            });
        }

        [SocketMethod]
        private MessageInfo[] acceptRematch()
        {
            return processGameState((db, game, pos, whiteToPlay, moves) =>
            {
                if ((_player == Player.White && game.RematchOffer != RematchOffer.Black) ||
                    (_player == Player.Black && game.RematchOffer != RematchOffer.White))
                    return null;
                game.RematchOffer = RematchOffer.Accepted;
                var match = game.Match.NullOr(mid => db.Matches.FirstOrDefault(m => m.ID == mid));
                var newGame = match == null
                    ? db.CreateNewGame(CreateNewGameOption.RollAlready, game.HasDoublingCube, game.Visibility)
                    : db.CreateNewMatch(CreateNewGameOption.RollAlready, match.MaxScore, match.DoublingCubeRules, game.Visibility).Game;
                game.NextGame = newGame.PublicID;
                return sendNextUrl(newGame.PublicID, newGame.WhiteToken, newGame.BlackToken)
                    .Concat(new MessageInfo(new JsonDict { { "rematch", game.RematchOffer.ToString() } }))
                    .ToArray();
            });
        }

        [SocketMethod]
        private MessageInfo[] cancelRematch()
        {
            return processGameState((db, game, pos, whiteToPlay, moves) =>
            {
                if ((_player == Player.White && game.RematchOffer != RematchOffer.Black) ||
                    (_player == Player.Black && game.RematchOffer != RematchOffer.White))
                    return null;
                game.RematchOffer = _player == Player.White ? RematchOffer.WhiteDeclined : RematchOffer.BlackDeclined;
                return new[] { new MessageInfo(new JsonDict { { "rematch", game.RematchOffer.ToString() } }) };
            });
        }

        [SocketMethod]
        private MessageInfo[] chat(string msg, long token)
        {
            msg = msg.Trim();
            if (msg.Length == 0)
                return null;

            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var chatMsgObj = new ChatMessage { GameID = GameId, Player = _player, Time = DateTime.UtcNow, Message = msg };
                db.ChatMessages.Add(chatMsgObj);
                db.SaveChanges();
                tr.Complete();
                SendMessage(new JsonDict { { "chatid", new JsonDict { { "token", token }, { "id", chatMsgObj.ID } } } });
                return new[] { new MessageInfo(new JsonDict { { "chat", chatMessageJson(chatMsgObj) } }, sendToAllGamesInMatch: true) };
            }
        }

        [SocketMethod]
        private MessageInfo[] chatSeen(List<int> ids)
        {
            if (ids == null || ids.Count == 0 || Player == Player.Spectator)
                return null;

            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var chatMsgs = db.ChatMessages.Where(ch => ids.Contains(ch.ID) && ch.Player != Player).ToArray();
                foreach (var msg in chatMsgs)
                    msg.Seen = true;
                db.SaveChanges();
                tr.Complete();
                return null;
            }
        }

        [SocketMethod(spectatorAllowed: true)]
        private MessageInfo[] settings()
        {
            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var styles = db.Styles
                    .Where(s => s.Approved)
                    .Select(s => new { s.Name, s.HashName })
                    .ToJsonDict(s => s.HashName, s => s.Name);
                var languages = db.Languages
                    .Where(s => s.Approved)
                    .Select(s => new { s.Name, s.HashName })
                    .ToJsonDict(s => s.HashName, s => s.Name);
                SendMessage(new JsonDict { { "settings", new JsonDict { { "style", styles }, { "lang", languages } } } });
                return null;
            }
        }

        [SocketMethod(spectatorAllowed: true)]
        private MessageInfo[] resync(int moveCount, bool? lastMoveDone = null)
        {
            return processGameState<MessageInfo[]>((db, game, pos, whiteToPlay, moves) =>
            {
                var needUpdate = moveCount != moves.Count ||
                    (lastMoveDone != null && moves.Count == 0) ||
                    (lastMoveDone != null && lastMoveDone.Value != (moves.Last().SourceTongues != null));
                SendMessage(new JsonDict { { "resync", needUpdate ? new JsonDict { { "moves", JsonValue.Parse(game.Moves) }, { "state", game.State.ToString() } } : null } });
                return null;
            });
        }

        [SocketMethod(spectatorAllowed: true)]
        private MessageInfo[] getLanguages()
        {
            using (var tr = Program.NewTransaction())
            using (var db = new Db())
                SendMessage(new JsonDict { { "languages", db.Languages.ToJsonList(l => new JsonDict {
                    { "name", l.Name },
                    { "hash", l.HashName },
                    { "isApproved", l.Approved }
                }) } });
            return null;
        }

        [SocketMethod(spectatorAllowed: true)]
        private MessageInfo[] createLanguage(string name, string hashName)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                SendMessage(new JsonDict { { "translateError", new JsonDict { { "error", "Please provide the name of your language." } } } });
                return null;
            }
            if (string.IsNullOrWhiteSpace(hashName))
            {
                SendMessage(new JsonDict { { "translateError", new JsonDict { { "error", "Please provide the language code for your language." } } } });
                return null;
            }
            name = name.Trim();
            hashName = hashName.Trim();

            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var already = db.Languages.Where(l => l.Name == name).FirstOrDefault();
                if (already != null)
                {
                    SendMessage(new JsonDict { { "translateError", new JsonDict {
                        { "error", "A language by this name already exists (and has been selected for you in the dropdown above). Please edit the existing language or choose a new name (e.g. “Français (Québec)” instead of just “Français”)." },
                        { "hash", already.HashName },
                        { "name", already.Name } } } });
                    return null;
                }
                already = db.Languages.Where(l => l.HashName == hashName).FirstOrDefault();
                if (already != null)
                {
                    SendMessage(new JsonDict { { "translateError", new JsonDict {
                        { "error", "A language with this ISO code already exists (and has been selected for you in the dropdown above). Please edit the existing language or use a different code. For regional variants, add the country code (e.g. “fr-CA” instead of just “fr”). For exotic languages, use “xx-” followed by the name, e.g. “xx-quenya”." },
                        { "hash", already.HashName },
                        { "name", already.Name } } } });
                    return null;
                }

                var token = Rnd.GenerateString(8);
                var data = new LanguageData();
                data.Suggestions[token] = new LanguageSuggestion { LastChange = DateTime.UtcNow };
                var newLang = new Language
                {
                    HashName = hashName,
                    Name = name,
                    Data = ClassifyJson.Serialize(data).ToString(),
                    LastChange = DateTime.UtcNow,
                    Approved = false
                };
                db.Languages.Add(newLang);
                db.SaveChanges();
                tr.Complete();
                SendMessage(new JsonDict { { "translate", new JsonDict { { "hash", newLang.HashName }, { "name", newLang.Name }, { "token", token } } } });
                return null;
            }
        }

        [SocketMethod(spectatorAllowed: true)]
        private MessageInfo[] translate(string hashName, string token = null)
        {
            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var language = db.Languages.Where(l => l.HashName == hashName).FirstOrDefault();
                if (language == null)
                {
                    SendMessage(new JsonDict { { "translateError", "The specified language no longer exists. It may have been deleted in the meantime." } });
                    return null;
                }
                var data = ClassifyJson.Deserialize<LanguageData>(JsonValue.Parse(language.Data));
                var strings = data.Translations.ToJsonDict(str => str.Key, str => str.Value);
                if (token == null)
                    token = Rnd.GenerateString(8);
                else if (data.Suggestions.ContainsKey(token))
                    foreach (var kvp in data.Suggestions[token].Translations)
                        strings[kvp.Key] = kvp.Value;
                SendMessage(new JsonDict { { "translate", new JsonDict { { "hash", language.HashName }, { "name", language.Name }, { "token", token }, { "strings", strings } } } });
            }
            return null;
        }

        [SocketMethod(spectatorAllowed: true)]
        private MessageInfo[] translateSubmit(string hashName, string token, string sel, string trans)
        {
            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var language = db.Languages.Where(l => l.HashName == hashName).FirstOrDefault();
                if (language == null)
                {
                    SendMessage(new JsonDict { { "translateError", "The specified language no longer exists. It may have been deleted in the meantime." } });
                    return null;
                }
                var data = ClassifyJson.Deserialize<LanguageData>(JsonValue.Parse(language.Data));
                if (!data.Suggestions.ContainsKey(token))
                    data.Suggestions[token] = new LanguageSuggestion();
                data.Suggestions[token].LastChange = DateTime.UtcNow;
                var removed = string.IsNullOrWhiteSpace(trans);
                if (removed)
                {
                    data.Suggestions[token].Translations.Remove(sel);
                    if (data.Suggestions[token].Translations.Count == 0)
                        data.Suggestions.Remove(token);
                }
                else
                    data.Suggestions[token].Translations[sel] = trans;
                language.Data = ClassifyJson.Serialize(data).ToString();
                language.LastChange = DateTime.UtcNow;
                db.SaveChanges();
                tr.Complete();
                SendMessage(new JsonDict { { "translationSaved", new JsonDict { { "sel", sel }, { "removed", removed } } } });
            }
            return null;
        }

        protected override void onTextMessageReceived(string msg)
        {
            var json = JsonValue.Parse(msg);
            if (!(json is JsonDict) || json.Count != 1)
                return;

            var key = json.Keys.First();
            var method = typeof(PlayWebSocket).GetMethod(key, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                return;

            var attr = method.GetCustomAttribute<SocketMethodAttribute>();
            if (attr == null || (_player == Player.Spectator && !attr.SpectatorAllowed))
                return;

            var arguments = json.Values.First();
            var toSend = (MessageInfo[]) method.Invoke(this, method.GetParameters().Select(p =>
            {
                JsonValue arg;
                var has = arguments.TryGetValue(p.Name, out arg);
                if (!p.IsOptional && !has)
                    throw new InvalidOperationException("Expected parameter {0} missing.".Fmt(p.Name));
                return has ? ClassifyJson.Deserialize(p.ParameterType, arg) : p.DefaultValue;
            }).ToArray());

            // Send all the WebSocket messages
            // (We do this at the end so that we don’t send /any/ messages if any part of the above code throws an exception)
            if (toSend != null && toSend.Length > 0)
            {
                var gameSockets = _server.GetSocketsByGame(GameId);
                if (gameSockets != null)
                    foreach (var socket in gameSockets)
                        foreach (var sendMsg in toSend)
                            if ((MatchId == null || !sendMsg.SendToAllGamesInMatch) && (sendMsg.Predicate == null || sendMsg.Predicate(socket)))
                                socket.SendMessage(sendMsg.Message);

                if (MatchId != null && toSend.Any(m => m.SendToAllGamesInMatch))
                {
                    var matchSockets = _server.GetSocketsByMatch(MatchId.Value);
                    if (matchSockets != null)
                        foreach (var socket in matchSockets)
                            foreach (var sendMsg in toSend)
                                if (sendMsg.SendToAllGamesInMatch && (sendMsg.Predicate == null || sendMsg.Predicate(socket)))
                                    socket.SendMessage(sendMsg.Message);
                }
            }
        }

        private T processGameState<T>(Func<Db, Game, Position, bool, List<Move>, T> func)
        {
            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var game = db.Games.FirstOrDefault(g => g.PublicID == GameId);
                if (game == null)
                    throw new InvalidOperationException("game is null");

                var pos = game.InitialPosition.ToPosition();
                var moves = game.Moves.ToMoves();

                // Determine what the current game position is
                var whiteStarts = moves.Count > 0 && moves[0].Dice1 > moves[0].Dice2;
                for (int i = 0; i < moves.Count; i++)
                    pos = pos.ProcessMove(whiteStarts ? (i % 2 == 0) : (i % 2 != 0), moves[i]);
                var whiteToPlay = whiteStarts ? (moves.Count % 2 != 0) : (moves.Count % 2 == 0);

                var result = func(db, game, pos, whiteToPlay, moves);
                db.SaveChanges();
                tr.Complete();
                return result;
            }
        }

        private IEnumerable<MessageInfo> sendNextUrl(string publicId, string whiteToken, string blackToken)
        {
            yield return new MessageInfo(new JsonDict { { "nextUrl", _url.WithParent("play/" + publicId + whiteToken).ToFull() } }, s => s.Player == Player.White);
            yield return new MessageInfo(new JsonDict { { "nextUrl", _url.WithParent("play/" + publicId + blackToken).ToFull() } }, s => s.Player == Player.Black);
            yield return new MessageInfo(new JsonDict { { "nextUrl", _url.WithParent("play/" + publicId).ToFull() } }, s => s.Player == Player.Spectator);
        }

        private IEnumerable<MessageInfo> gameOver(Db db, Game game, Position pos, bool whiteWins, bool useMultiplier)
        {
            var match = game.Match.NullOr(id => db.Matches.FirstOrDefault(m => m.ID == id));
            var winScore = (pos.GameValue ?? 1) * (useMultiplier ? pos.GetWinMultiplier(whiteWins) : 1);
            var dict = new JsonDict {
                { "state", game.State.ToString() },
                { "score", winScore }
            };

            if (whiteWins)
                game.WhiteScore = winScore;
            else
                game.BlackScore = winScore;

            if (game.Match != null)
            {
                var whiteMatchScore = db.Games.Where(g => g.Match == game.Match && g.GameInMatch < game.GameInMatch).Select(g => g.WhiteScore).DefaultIfEmpty().Sum() + game.WhiteScore;
                var blackMatchScore = db.Games.Where(g => g.Match == game.Match && g.GameInMatch < game.GameInMatch).Select(g => g.BlackScore).DefaultIfEmpty().Sum() + game.BlackScore;
                dict["whiteMatchScore"] = whiteMatchScore;
                dict["blackMatchScore"] = blackMatchScore;
                if (whiteMatchScore >= match.MaxScore || blackMatchScore >= match.MaxScore)
                    dict["matchOver"] = 1;
                else
                {
                    bool doublingCube = match.DoublingCubeRules != DoublingCubeRules.None;
                    if (match.DoublingCubeRules == DoublingCubeRules.Crawford && (whiteMatchScore == match.MaxScore - 1 || blackMatchScore == match.MaxScore - 1))
                    {
                        // Check if there has already been a Crawford game
                        doublingCube = db.Games.Any(g => g.Match == game.Match && !g.HasDoublingCube);
                    }
                    var result = db.CreateNewGame(CreateNewGameOption.RollAlready, doublingCube, game.Visibility, game.Match, game.GameInMatch + 1);
                    game.NextGame = result.PublicID;
                    foreach (var msg in sendNextUrl(result.PublicID, result.WhiteToken, result.BlackToken))
                        yield return msg;
                    dict["nextGame"] = new JsonDict { { "cube", result.HasDoublingCube } };
                }
            }
            yield return new MessageInfo(new JsonDict { { "win", dict } });
        }

        private IEnumerable<MessageInfo> continueGame(Db db, Position pos, Game game, bool whiteToPlay, List<Move> moves, bool rolled = false, bool acceptedDouble = false)
        {
            // Keep generating new moves until a player has a choice
            var firstIteration = true;
            while (true)
            {
                if (pos.NumPiecesPerTongue[Tongues.WhiteHome] == 15 || pos.NumPiecesPerTongue[Tongues.BlackHome] == 15)
                {
                    // The game is over.
                    game.State = pos.NumPiecesPerTongue[Tongues.WhiteHome] == 15 ? GameState.White_Won_Finished : GameState.Black_Won_Finished;
                    foreach (var msg in gameOver(db, game, pos, whiteWins: game.State == GameState.White_Won_Finished, useMultiplier: true))
                        yield return msg;
                    break;
                }

                whiteToPlay = !whiteToPlay;

                if (pos.GameValue != null && (pos.WhiteOwnsCube == null || pos.WhiteOwnsCube == whiteToPlay) &&
                    !(firstIteration && (rolled || acceptedDouble)))
                {
                    // The player can choose to roll or double.
                    game.State = whiteToPlay ? GameState.White_ToRoll : GameState.Black_ToRoll;
                    yield return new MessageInfo(new JsonDict { { "state", game.State.ToString() } });
                    break;
                }

                // Roll the dice
                var newMove = new Move { Dice1 = Rnd.Next(1, 7), Dice2 = Rnd.Next(1, 7), Doubled = acceptedDouble && firstIteration };
                moves.Add(newMove);
                firstIteration = false;

                // Generate all possible moves
                var validMoves = pos.GetAllValidMoves(whiteToPlay, newMove.Dice1, newMove.Dice2).GroupBy(move => move.EndPosition, new PossiblePosition.Comparer()).ToList();

                yield return new MessageInfo(new JsonDict { { "dice", new JsonDict {
                    { "dice1", newMove.Dice1 },
                    { "dice2", newMove.Dice2 },
                    { "doubled", newMove.Doubled },
                    { "skipHighlight", validMoves.Count < 2 },
                    { "state", (whiteToPlay ? GameState.White_ToMove : GameState.Black_ToMove).ToString() } } } });

                if (validMoves.Count > 1)
                {
                    // Player must make a choice
                    game.State = whiteToPlay ? GameState.White_ToMove : GameState.Black_ToMove;
                    break;
                }

                // Player either cannot move at all, or has only one possible move and thus no choice.
                // Do not use the same int[] instance for the empty array because Classify then creates JSON that the JavaScript doesn’t cope with.
                newMove.SourceTongues = (validMoves.Count == 0) ? new int[0] : validMoves.First().First().SourceTongues;
                newMove.TargetTongues = (validMoves.Count == 0) ? new int[0] : validMoves.First().First().TargetTongues;
                yield return new MessageInfo(new JsonDict { { "move", new JsonDict { { "sourceTongues", newMove.SourceTongues }, { "targetTongues", newMove.TargetTongues }, { "auto", validMoves.Count } } } });
                pos = pos.ProcessMove(whiteToPlay, newMove);
            }

            game.Moves = ClassifyJson.Serialize(moves).ToString();
        }

        private JsonDict chatMessageJson(ChatMessage msg)
        {
            return new JsonDict
            {
                { "id", msg.ID },
                { "game", msg.GameID },
                { "msg", msg.Message },
                { "player", msg.Player.ToString() },
                { "time", msg.Time.ToIsoString(format: IsoDateFormat.Iso8601) + "Z" },
                { "seen", msg.Seen }
            };
        }
    }
}
