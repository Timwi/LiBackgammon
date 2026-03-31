using RT.Util.ExtensionMethods;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private HashSet<MainWebSocket> _activeMainSockets = [];

        public void AddMainSocket(MainWebSocket socket)
        {
            lock (_activeMainSockets)
                _activeMainSockets.Add(socket);
        }

        public void RemoveMainSocket(MainWebSocket socket)
        {
            lock (_activeMainSockets)
                _activeMainSockets.Remove(socket);
        }

        public MainWebSocket[] GetMainSockets()
        {
            lock (_activeMainSockets)
                return _activeMainSockets.ToArray();
        }

        public void NotifySocketsAddGame(Game game, Match match)
        {
            lock (_activeMainSockets)
                foreach (var socket in _activeMainSockets)
                    socket.AddGame(game, match);
        }

        public void NotifySocketsRemoveGame(Game game)
        {
            lock (_activeMainSockets)
                foreach (var socket in _activeMainSockets)
                    socket.RemoveGame(game.PublicID);
        }

        private readonly Dictionary<string, List<PlayWebSocket>> _activePlaySocketsByGame = [];
        private readonly Dictionary<int, List<PlayWebSocket>> _activePlaySocketsByMatch = [];

        public void AddGameSocket(PlayWebSocket socket)
        {
            lock (_activePlaySocketsByGame)
                _activePlaySocketsByGame.AddSafe(socket.GameId, socket);
            if (socket.MatchId != null)
                lock (_activePlaySocketsByMatch)
                    _activePlaySocketsByMatch.AddSafe(socket.MatchId.Value, socket);
        }

        public void RemoveGameSocket(PlayWebSocket socket)
        {
            lock (_activePlaySocketsByGame)
                _activePlaySocketsByGame.RemoveSafe(socket.GameId, socket);
            if (socket.MatchId != null)
                lock (_activePlaySocketsByMatch)
                    _activePlaySocketsByMatch.RemoveSafe(socket.MatchId.Value, socket);
        }

        public PlayWebSocket[] GetSocketsByGame(string gameId)
        {
            lock (_activePlaySocketsByGame)
                if (_activePlaySocketsByGame.TryGetValue(gameId, out var sockets))
                    return sockets.ToArray();
            return null;
        }

        public PlayWebSocket[] GetSocketsByMatch(int matchId)
        {
            lock (_activePlaySocketsByMatch)
                if (_activePlaySocketsByMatch.TryGetValue(matchId, out var sockets))
                    return sockets.ToArray();
            return null;
        }
    }
}
