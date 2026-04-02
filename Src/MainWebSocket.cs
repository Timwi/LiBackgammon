using RT.Json;
using RT.Servers;
using RT.Util.ExtensionMethods;

namespace LiBackgammon
{
    public sealed class MainWebSocket(LiBackgammonPropellerModule server) : WebSocket
    {
        protected override void OnBeginConnection()
        {
            using var tr = Program.NewTransaction();
            using var db = new Db();
            SendMessage(new JsonDict
            {
                ["add"] = db.Games
                    .Where(g => (g.State == GameState.White_Waiting || g.State == GameState.Black_Waiting || g.State == GameState.Random_Waiting) && g.Visibility == Visibility.Public)
                    .Select(g => new { Game = g, Match = g.Match == null ? null : db.Matches.FirstOrDefault(m => m.ID == g.Match) })
                    .ToJsonList(inf => getJson(inf.Game, inf.Match))
            });
            server.AddMainSocket(this);
        }

        protected override void OnEndConnection()
        {
            server.RemoveMainSocket(this);
        }

        public void AddGame(Game game, Match match)
        {
            SendMessage(new JsonDict { ["add"] = new JsonList { getJson(game, match) } });
        }

        private static JsonDict getJson(Game game, Match match)
        {
            return new JsonDict
            {
                ["id"] = game.PublicID,
                ["state"] = game.State.ToString(),
                ["cube"] = (match != null ? match.DoublingCubeRules : game.HasDoublingCube ? DoublingCubeRules.Standard : DoublingCubeRules.None).ToString(),
                ["maxscore"] = match == null ? 1 : match.MaxScore
            };
        }

        public void RemoveGame(string publicId)
        {
            SendMessage(new JsonDict { ["remove"] = publicId });
        }
    }
}
