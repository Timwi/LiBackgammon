using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;
using RT.Util.Serialization;

namespace LiBackgammon
{
    sealed class PlayWebSocket : WebSocket
    {
        private string _gameId;
        private Player _player;
        private LiBackgammonPropellerModule _server;
        private IHttpUrl _url;

        public PlayWebSocket(LiBackgammonPropellerModule server, string gameId, Player player, IHttpUrl url)
        {
            _server = server;
            _gameId = gameId;
            _player = player;
            _url = url;
        }

        public override void OnBeginConnection()
        {
            lock (_server.ActiveSockets)
                _server.ActiveSockets.AddSafe(_gameId, this);
        }

        public override void OnEndConnection()
        {
            lock (_server.ActiveSockets)
                _server.ActiveSockets.RemoveSafe(_gameId, this);
        }

        private static string[] ValidKeys = new[] { "move", "roll", "double", "accept", "reject", "resign", "resync", "rematch", "acceptRematch", "cancelRematch" };

        public override void OnTextMessageReceived(string msg)
        {
            var json = JsonValue.Parse(msg);
            if (!(json is JsonDict) || json.Count != 1 || json.Keys.Any(key => !ValidKeys.Contains(key)))
                return;

            if (_player == Player.Spectator && !json.ContainsKey("resync"))
                return;

            var createMsg = Ut.Lambda((JsonValue value, Func<PlayWebSocket, bool> predicate) => new { Value = value, Predicate = predicate });
            var toSend = new[] { createMsg(null, null) }.ToList();
            toSend.Clear();

            using (var tr = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
            using (var db = new Db())
            {
                var game = db.Games.FirstOrDefault(g => g.PublicID == _gameId);
                if (game == null)
                    return;
                var pos = game.InitialPosition.ToPosition();
                var moves = game.Moves.ToMoves();

                if (json.ContainsKey("resync"))
                {
                    if (json["resync"]["moves"].GetInt() != moves.Count || json["resync"]["lastmovedone"].GetBool() != (moves.Last().SourceTongues != null))
                        SendMessage(new JsonDict { { "resync", 1 } });
                    return;
                }

                // Determine what the current game position is
                var whiteStarts = moves.Count > 0 && moves[0].Dice1 > moves[0].Dice2;
                for (int i = 0; i < moves.Count; i++)
                    pos = pos.ProcessMove(whiteStarts ? (i % 2 == 0) : (i % 2 != 0), moves[i]);
                var lastMove = moves.Last();
                var whiteToPlay = whiteStarts ? (moves.Count % 2 != 0) : (moves.Count % 2 == 0);

                var sendNextUrl = Ut.Lambda((string publicId, string whiteToken, string blackToken) =>
                {
                    toSend.Add(createMsg(new JsonDict { { "nextUrl", _url.WithParent("play/" + publicId + whiteToken).ToFull() } }, s => s._player == Player.White));
                    toSend.Add(createMsg(new JsonDict { { "nextUrl", _url.WithParent("play/" + publicId + blackToken).ToFull() } }, s => s._player == Player.Black));
                    toSend.Add(createMsg(new JsonDict { { "nextUrl", _url.WithParent("play/" + publicId).ToFull() } }, s => s._player == Player.Spectator));
                });

                var gameOver = Ut.Lambda((bool whiteWins, bool useMultiplier) =>
                {
                    var match = game.Match.NullOr(id => db.Matches.FirstOrDefault(m => m.ID == id));
                    var winScore = (pos.GameValue ?? 1) * (useMultiplier ? pos.GetWinMultiplier(whiteWins) : 1);
                    var dict = new JsonDict {
                        { "state", game.State.ToString() },
                        { "win", winScore }
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
                            bool isCrawford = false;
                            if (match.DoublingCubeRules == DoublingCubeRules.Crawford && whiteMatchScore == match.MaxScore - 1 || blackMatchScore == match.MaxScore - 1)
                            {
                                // Check if there has already been a Crawford game
                                doublingCube = db.Games.Any(g => g.Match == game.Match && g.IsCrawfordGame);
                                isCrawford = !doublingCube;
                            }
                            var result = db.CreateNewGame(CreateNewGameOption.RollAlready, doublingCube, game.Visibility, isCrawford, game.Match, game.GameInMatch + 1);
                            game.NextGame = result.PublicID;
                            sendNextUrl(result.PublicID, result.WhiteToken, result.BlackToken);
                        }
                    }
                    toSend.Add(createMsg(dict, null));
                });

                if (json.ContainsKey("double"))
                {
                    // Make sure it’s this player’s turn to choose whether to double or roll, and the game is played with a doubling cube
                    // Note “whiteToPlay” is currently off because the double doesn’t have an entry in “moves” yet — hence the not
                    if (pos.GameValue == null || !whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.White_ToRoll : GameState.Black_ToRoll))
                        return;
                    game.State = _player == Player.White ? GameState.Black_ToConfirmDouble : GameState.White_ToConfirmDouble;
                    toSend.Add(createMsg(new JsonDict { { "state", game.State.ToString() } }, null));
                }
                else if (json.ContainsKey("reject"))
                {
                    // Make sure it’s this player’s turn to respond to a double, and the game is played with a doubling cube
                    if (pos.GameValue == null || whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.White_ToConfirmDouble : GameState.Black_ToConfirmDouble))
                        return;
                    game.State = _player == Player.White ? GameState.Black_Won_RejectedDouble : GameState.White_Won_RejectedDouble;
                    gameOver(game.State == GameState.White_Won_RejectedDouble, false);
                }
                else if (json.ContainsKey("resign"))
                {
                    // You can’t resign if the game is already over
                    if (game.State == GameState.Black_Won_Finished || game.State == GameState.Black_Won_RejectedDouble || game.State == GameState.Black_Won_Resignation ||
                        game.State == GameState.White_Won_Finished || game.State == GameState.White_Won_RejectedDouble || game.State == GameState.White_Won_Resignation)
                        return;
                    game.State = _player == Player.White ? GameState.Black_Won_Resignation : GameState.White_Won_Resignation;
                    gameOver(game.State == GameState.White_Won_Resignation, true);
                }
                else if (json.ContainsKey("rematch"))
                {
                    if (
                        game.RematchOffer != RematchOffer.None &&
                        !(game.RematchOffer == RematchOffer.WhiteRejected && _player == Player.White) &&
                        !(game.RematchOffer == RematchOffer.BlackRejected && _player == Player.Black))
                        return;
                    game.RematchOffer = _player == Player.White ? RematchOffer.White : RematchOffer.Black;
                    toSend.Add(createMsg(new JsonDict { { "rematch", game.RematchOffer.ToString() } }, null));
                }
                else if (json.ContainsKey("acceptRematch"))
                {
                    if ((_player == Player.White && game.RematchOffer != RematchOffer.Black) ||
                        (_player == Player.Black && game.RematchOffer != RematchOffer.White))
                        return;
                    game.RematchOffer = RematchOffer.Accepted;
                    var match = game.Match.NullOr(mid => db.Matches.FirstOrDefault(m => m.ID == mid));
                    var result = match == null
                        ? db.CreateNewGame(CreateNewGameOption.RollAlready, pos.GameValue != null, game.Visibility)
                        : db.CreateNewMatch(CreateNewGameOption.RollAlready, match.MaxScore, match.DoublingCubeRules, game.Visibility);
                    game.NextGame = result.PublicID;
                    sendNextUrl(result.PublicID, result.WhiteToken, result.BlackToken);
                    toSend.Add(createMsg(new JsonDict { { "rematch", game.RematchOffer.ToString() } }, null));
                }
                else if (json.ContainsKey("cancelRematch"))
                {
                    if ((_player == Player.White && game.RematchOffer != RematchOffer.Black) ||
                        (_player == Player.Black && game.RematchOffer != RematchOffer.White))
                        return;
                    game.RematchOffer = _player == Player.White ? RematchOffer.WhiteRejected : RematchOffer.BlackRejected;
                    toSend.Add(createMsg(new JsonDict { { "rematch", game.RematchOffer.ToString() } }, null));
                }
                else
                {
                    var acceptedDouble = false;

                    if (json.ContainsKey("move"))
                    {
                        // Make sure it is actually this player’s turn
                        if (whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.White_ToMove : GameState.Black_ToMove))
                            return;

                        var sourceTongues = json["move"]["SourceTongues"].GetInts();
                        var targetTongues = json["move"]["TargetTongues"].GetInts();

                        // Find all valid moves and see if the provided move is one of them
                        var validMoves = pos.GetAllValidMoves(whiteToPlay, lastMove.Dice1, lastMove.Dice2);
                        var validMove = validMoves.FirstOrDefault(vm => vm.SourceTongues.SequenceEqual(sourceTongues) && vm.TargetTongues.SequenceEqual(targetTongues));
                        if (validMove == null)
                            return;

                        // The move is valid. 
                        lastMove.SourceTongues = sourceTongues;
                        lastMove.TargetTongues = targetTongues;
                        pos = pos.ProcessMove(whiteToPlay, lastMove);
                        toSend.Add(createMsg(json, s => s != this));
                    }
                    else if (json.ContainsKey("roll"))
                    {
                        // Make sure it is actually this player’s turn, and the game is played with a doubling cube (otherwise the players do not manually roll)
                        // Note “whiteToPlay” is currently off because the dice roll doesn’t have an entry in “moves” yet — hence the not
                        if (pos.GameValue == null || !whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.White_ToRoll : GameState.Black_ToRoll))
                            return;
                        // The actual dice rolling happens inside the following while loop.
                    }
                    else if (json.ContainsKey("accept"))
                    {
                        // Make sure it’s this player’s turn to respond to a double
                        if (pos.GameValue == null || whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.White_ToConfirmDouble : GameState.Black_ToConfirmDouble))
                            return;
                        acceptedDouble = true;
                        pos.GameValue = pos.GameValue.Value * 2;
                        toSend.Add(createMsg(new JsonDict { { "cube", new JsonDict { { "GameValue", pos.GameValue.Value }, { "WhiteOwnsCube", _player == Player.White } } } }, null));
                        // The game continues with a dice roll, which happens inside the following while loop.
                    }

                    // Keep generating new moves until a player has a choice
                    var firstIteration = true;
                    while (true)
                    {
                        if (pos.NumPiecesPerTongue[Tongues.WhiteHome] == 15 || pos.NumPiecesPerTongue[Tongues.BlackHome] == 15)
                        {
                            // The game is over.
                            game.State = pos.NumPiecesPerTongue[Tongues.WhiteHome] == 15 ? GameState.White_Won_Finished : GameState.Black_Won_Finished;
                            gameOver(game.State == GameState.White_Won_Finished, true);
                            break;
                        }

                        whiteToPlay = !whiteToPlay;

                        if (pos.GameValue != null && (pos.WhiteOwnsCube == null || pos.WhiteOwnsCube == whiteToPlay) &&
                            !(firstIteration && (json.ContainsKey("roll") || json.ContainsKey("accept"))))
                        {
                            // The player can choose to roll or double.
                            game.State = whiteToPlay ? GameState.White_ToRoll : GameState.Black_ToRoll;
                            toSend.Add(createMsg(new JsonDict { { "state", game.State.ToString() } }, null));
                            break;
                        }

                        // Roll the dice
                        lastMove = new Move { Dice1 = Rnd.Next(1, 7), Dice2 = Rnd.Next(1, 7), Doubled = acceptedDouble && firstIteration };
                        moves.Add(lastMove);
                        toSend.Add(createMsg(new JsonDict { { "dice", new JsonDict {
                            { "dice1", lastMove.Dice1 },
                            { "dice2", lastMove.Dice2 },
                            { "state", (whiteToPlay ? GameState.White_ToMove : GameState.Black_ToMove).ToString() } } } }, null));
                        firstIteration = false;

                        // Generate all possible moves
                        var validMoves = pos.GetAllValidMoves(whiteToPlay, lastMove.Dice1, lastMove.Dice2).GroupBy(move => move.EndPosition, new PossiblePosition.Comparer()).ToList();

                        if (validMoves.Count > 1)
                        {
                            // Player must make a move
                            game.State = whiteToPlay ? GameState.White_ToMove : GameState.Black_ToMove;
                            toSend.Add(createMsg(new JsonDict { { "state", game.State.ToString() } }, null));
                            break;
                        }

                        // Player either cannot move at all, or has only one possible move and thus no choice.
                        // Do not use the same int[] instance for the empty array because Classify then creates JSON that the JavaScript doesn’t cope with.
                        lastMove.SourceTongues = (validMoves.Count == 0) ? new int[0] : validMoves.First().First().SourceTongues;
                        lastMove.TargetTongues = (validMoves.Count == 0) ? new int[0] : validMoves.First().First().TargetTongues;
                        toSend.Add(createMsg(new JsonDict { { "move", new JsonDict { { "SourceTongues", lastMove.SourceTongues }, { "TargetTongues", lastMove.TargetTongues } } }, { "auto", validMoves.Count } }, null));
                        pos = pos.ProcessMove(whiteToPlay, lastMove);
                    }
                }

                game.Moves = ClassifyJson.Serialize(moves).ToString();
                db.SaveChanges();
                tr.Complete();
            }

            // Send all the WebSocket messages
            // (We do this at the end so that we don’t send /any/ messages if any part of the above code throws an exception)
            List<PlayWebSocket> sockets;
            lock (_server.ActiveSockets)
                if (toSend.Count > 0 && _server.ActiveSockets.TryGetValue(_gameId, out sockets))
                    foreach (var socket in sockets)
                        foreach (var sendMsg in toSend)
                            if (sendMsg.Predicate == null || sendMsg.Predicate(socket))
                                socket.SendMessage(sendMsg.Value);
        }
    }
}
