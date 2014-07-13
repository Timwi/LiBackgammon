using RT.Util;
using RT.Util.Serialization;

namespace LiBackgammon
{
    public sealed class Position
    {
        public int[] NumPiecesPerTongue;
        public bool[] IsWhitePerTongue;
        public int? GameValue;  // null = game is played without the doubling cube.
        public bool? WhiteOwnsCube; // null = nobody has doubled yet

        public new string ToJson() { return ClassifyJson.Serialize(this).ToString(); }

        public static readonly Position StandardInitialPosition = new Position
        {
            NumPiecesPerTongue = new[] { 2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 5, 5, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, 2, 0, 0, 0, 0 },
            IsWhitePerTongue = Ut.NewArray(Tongues.NumTongues, i => i == 0 || i == 11 || i == 16 || i == 18 || i == Tongues.WhiteHome || i == Tongues.WhitePrison),
            GameValue = 1,
            WhiteOwnsCube = null
        };
    }
}
