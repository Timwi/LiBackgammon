using System.Linq;
using RT.Util;
using RT.Util.Serialization;

namespace LiBackgammon
{
    public sealed class Position : PossiblePosition
    {
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

        public Position ProcessMove(bool whitePlayer, Move move)
        {
            var poss = move.SourceTongues == null ? this : Clone().ProcessMove(whitePlayer, move.SourceTongues, move.TargetTongues);
            return new Position
            {
                GameValue = move.Doubled ? GameValue.Value * 2 : GameValue,
                WhiteOwnsCube = move.Doubled ? !whitePlayer : WhiteOwnsCube,
                NumPiecesPerTongue = poss.NumPiecesPerTongue,
                IsWhitePerTongue = poss.IsWhitePerTongue
            };
        }

        public bool IsWon { get { return NumPiecesPerTongue[Tongues.WhiteHome] == 15 || NumPiecesPerTongue[Tongues.BlackHome] == 15; } }

        public int WinMultiplier
        {
            get
            {
                var whiteWon = NumPiecesPerTongue[Tongues.WhiteHome] == 15;

                // Backgammon
                if (Enumerable.Range(whiteWon ? 18 : 0, 6).Any(i => NumPiecesPerTongue[i] > 0 && IsWhitePerTongue[i] == !whiteWon))
                    return 3;

                // Gammon
                if (NumPiecesPerTongue[whiteWon ? Tongues.BlackHome : Tongues.WhiteHome] == 0)
                    return 2;

                // Single
                return 1;
            }
        }
    }
}
