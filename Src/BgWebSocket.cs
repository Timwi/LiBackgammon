using System;
using RT.Util.ExtensionMethods;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RT.Servers;

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

        public override void BeginConnection()
        {
            _server.ActiveSockets.AddSafe(_gameId, this);
        }

        public override void EndConnection()
        {
            _server.ActiveSockets.RemoveSafe(_gameId, this);
        }
    }
}
