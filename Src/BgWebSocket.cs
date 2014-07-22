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
    sealed class BgWebSocket : WebSocket
    {
        private string _gameId;
        private Player _player;
        private LiBackgammonPropellerModule _server;

        public BgWebSocket(LiBackgammonPropellerModule server, string gameId, Player player)
        {
            _server = server;
            _gameId = gameId;
            _player = player;
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

        private static int[] EmptyIntArray = new int[0];
        private static string[] ValidKeys = new[] { "move", "roll", "double", "accept", "reject" };

        public override void OnTextMessageReceived(string msg)
        {
            if (_player == Player.Spectator)
                return;

            var json = JsonValue.Parse(msg);
            if (!(json is JsonDict) || json.Count != 1 || ValidKeys.All(key => !json.ContainsKey(key)))
                return;

            var sendBoth = new List<JsonValue>();
            var sendElsewhere = new List<JsonValue>();

            using (var tr = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
            using (var db = new Db())
            {
                var game = db.Games.FirstOrDefault(g => g.PublicID == _gameId);
                if (game == null)
                    return;

                // Determine what the current game position is
                var pos = game.InitialPosition.ToPosition();
                var moves = game.Moves.ToMoves();
                var whiteStarts = moves.Count > 0 && moves[0].Dice1 > moves[0].Dice2;
                for (int i = 0; i < moves.Count; i++)
                    pos = pos.ProcessMove(whiteStarts ? (i % 2 == 0) : (i % 2 != 0), moves[i]);
                var lastMove = moves.Last();
                var whiteToPlay = whiteStarts ? (moves.Count % 2 != 0) : (moves.Count % 2 == 0);

                if (json.ContainsKey("double"))
                {
                    // Make sure it’s this player’s turn to choose whether to double or roll, and the game is played with a doubling cube
                    // Note “whiteToPlay” is currently off because the double doesn’t have an entry in “moves” yet — hence the not
                    if (pos.GameValue == null || !whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.WhiteToRoll : GameState.BlackToRoll))
                        return;
                    game.State = _player == Player.White ? GameState.BlackToConfirmDouble : GameState.WhiteToConfirmDouble;
                    sendBoth.Add(new JsonDict { { "state", (int) game.State } });
                }
                else if (json.ContainsKey("reject"))
                {
                    // Make sure it’s this player’s turn to respond to a double, and the game is played with a doubling cube
                    if (pos.GameValue == null || whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.WhiteToConfirmDouble : GameState.BlackToConfirmDouble))
                        return;
                    game.State = _player == Player.White ? GameState.BlackDoubledWhiteRejected : GameState.WhiteDoubledBlackRejected;
                    sendBoth.Add(new JsonDict { { "state", (int) game.State } });
                }
                else
                {
                    var acceptedDouble = false;

                    if (json.ContainsKey("move"))
                    {
                        // Make sure it is actually this player’s turn
                        if (whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.WhiteToMove : GameState.BlackToMove))
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
                        sendElsewhere.Add(json);
                    }
                    else if (json.ContainsKey("roll"))
                    {
                        // Make sure it is actually this player’s turn, and the game is played with a doubling cube (otherwise the players do not manually roll)
                        // Note “whiteToPlay” is currently off because the dice roll doesn’t have an entry in “moves” yet — hence the not
                        if (pos.GameValue == null || !whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.WhiteToRoll : GameState.BlackToRoll))
                            return;
                        // The actual dice rolling happens inside the following while loop.
                    }
                    else if (json.ContainsKey("accept"))
                    {
                        // Make sure it’s this player’s turn to respond to a double
                        if (pos.GameValue == null || whiteToPlay != (_player == Player.White) || game.State != (_player == Player.White ? GameState.WhiteToConfirmDouble : GameState.BlackToConfirmDouble))
                            return;
                        acceptedDouble = true;
                        pos.GameValue = pos.GameValue.Value * 2;
                        sendBoth.Add(new JsonDict { { "cube", new JsonList { pos.GameValue.Value, _player == Player.Black } } });
                        // The game continues with a dice roll, which happens inside the following while loop.
                    }

                    // Keep generating new moves until a player has a choice
                    var firstIteration = true;
                    while (true)
                    {
                        if (pos.NumPiecesPerTongue[Tongues.WhiteHome] == 15 || pos.NumPiecesPerTongue[Tongues.BlackHome] == 15)
                        {
                            // The game is over.
                            game.State = pos.NumPiecesPerTongue[Tongues.WhiteHome] == 15 ? GameState.WhiteFinished : GameState.BlackFinished;
                            sendBoth.Add(new JsonDict { { "state", (int) game.State } });
                            break;
                        }

                        whiteToPlay = !whiteToPlay;

                        if (pos.GameValue != null && (pos.WhiteOwnsCube == null || pos.WhiteOwnsCube == whiteToPlay) &&
                            !(firstIteration && (json.ContainsKey("roll") || json.ContainsKey("accept"))))
                        {
                            // The player can choose to roll or double.
                            game.State = whiteToPlay ? GameState.WhiteToRoll : GameState.BlackToRoll;
                            sendBoth.Add(new JsonDict { { "state", (int) game.State } });
                            break;
                        }

                        // Roll the dice
                        lastMove = new Move { Dice1 = Rnd.Next(1, 7), Dice2 = Rnd.Next(1, 7), Doubled = acceptedDouble && firstIteration };
                        moves.Add(lastMove);
                        sendBoth.Add(new JsonDict { { "dice", new JsonList { lastMove.Dice1, lastMove.Dice2 } } });
                        firstIteration = false;

                        // Generate all possible moves
                        var validMoves = pos.GetAllValidMoves(whiteToPlay, lastMove.Dice1, lastMove.Dice2);

                        if (validMoves.Count > 1)
                        {
                            // Player must make a move
                            game.State = whiteToPlay ? GameState.WhiteToMove : GameState.BlackToMove;
                            sendBoth.Add(new JsonDict { { "state", (int) game.State } });
                            break;
                        }

                        // Player either cannot move at all, or has only one possible move and thus no choice.
                        lastMove.SourceTongues = (validMoves.Count == 0) ? EmptyIntArray : validMoves[0].SourceTongues;
                        lastMove.TargetTongues = (validMoves.Count == 0) ? EmptyIntArray : validMoves[0].TargetTongues;
                        sendBoth.Add(new JsonDict { { "move", new JsonDict { { "SourceTongues", lastMove.SourceTongues }, { "TargetTongues", lastMove.TargetTongues } } } });
                    }
                }

                game.Moves = ClassifyJson.Serialize(moves).ToString();
                db.SaveChanges();
                tr.Complete();
            }

            // Notify all the other WebSockets
            List<BgWebSocket> sockets;
            lock (_server.ActiveSockets)
                if (sendElsewhere.Count + sendBoth.Count > 0 && _server.ActiveSockets.TryGetValue(_gameId, out sockets))
                {
                    var sendHere = new JsonList(sendBoth).ToString().ToUtf8();
                    var sendOthers = new JsonList(Enumerable.Concat(sendElsewhere, sendBoth)).ToString().ToUtf8();
                    foreach (var socket in sockets)
                        if (socket != this || sendBoth.Count > 0)
                            socket.SendMessage(1, socket == this ? sendHere : sendOthers);
                }
        }
    }
}
