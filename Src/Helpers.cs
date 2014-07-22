using RT.Util.Json;
using System.Linq;
using RT.Util.Serialization;
using System.Collections.Generic;

namespace LiBackgammon
{
    public static class Helpers
    {
        public static List<Move> ToMoves(this string json)
        {
            return ClassifyJson.Deserialize<List<Move>>(JsonValue.Parse(json));
        }

        public static Position ToPosition(this string json)
        {
            return ClassifyJson.Deserialize<Position>(JsonValue.Parse(json));
        }

        public static int[] GetInts(this JsonValue json)
        {
            return json.GetList().Select(v => v.GetInt()).ToArray();
        }
    }
}
