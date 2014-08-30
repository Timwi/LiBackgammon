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
    sealed class MainWebSocket : WebSocket
    {
        private LiBackgammonPropellerModule _server;
        private IHttpUrl _url;

        public MainWebSocket(LiBackgammonPropellerModule server, IHttpUrl url)
        {
            _server = server;
            _url = url;
        }

        public override void OnBeginConnection()
        {
            using (var tr = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
            using (var db = new Db())
            {
                SendMessage(new JsonDict
                {
                    { "add", db.Games
                        .Where(g => (g.State == GameState.White_Waiting || g.State == GameState.Black_Waiting || g.State == GameState.Random_Waiting) && g.Visibility == Visibility.Public)
                        .Select(g => new { Game = g, Match = g.Match == null ? null : db.Matches.FirstOrDefault(m => m.ID == g.Match) })
                        .ToJsonList(inf => getJson(inf.Game, inf.Match))
                    }
                });

                lock (_server.ActiveMainSockets)
                    _server.ActiveMainSockets.Add(this);
            }
        }

        public override void OnEndConnection()
        {
            lock (_server.ActiveMainSockets)
                _server.ActiveMainSockets.Remove(this);
        }

        public void AddGame(Game game, Match match)
        {
            SendMessage(new JsonDict { { "add", new JsonList { getJson(game, match) } } });
        }

        private static JsonDict getJson(Game game, Match match)
        {
            return new JsonDict
            {
                { "id", game.PublicID },
                { "state", game.State.ToString() },
                { "cube", (match != null ? match.DoublingCubeRules : game.HasDoublingCube ? DoublingCubeRules.Standard : DoublingCubeRules.None).ToString() },
                { "maxscore", match == null ? 1 : match.MaxScore }
            };
        }

        public void RemoveGame(string publicId)
        {
            SendMessage(new JsonDict { { "remove", publicId } });
        }
    }
}
