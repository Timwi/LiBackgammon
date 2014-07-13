using RT.Util.Json;
using RT.Util.Serialization;

namespace LiBackgammon
{
    public static class Helpers
    {
        public static Move[] ToMoves(this string json)
        {
            return ClassifyJson.Deserialize<Move[]>(JsonValue.Parse(json));
        }

        public static Position ToPosition(this string json)
        {
            return ClassifyJson.Deserialize<Position>(JsonValue.Parse(json));
        }
    }
}
