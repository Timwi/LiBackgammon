using System.Linq;
using RT.Util.ExtensionMethods;
using RT.Util.Serialization;

namespace LiBackgammon
{
    public sealed class Move
    {
        public bool Doubled;
        public int Dice1;
        public int Dice2;
        [ClassifyIgnoreIfDefault]
        public int[] SourceTongues;
        [ClassifyIgnoreIfDefault]
        public int[] TargetTongues;

        public override string ToString()
        {
            return $"[{Dice1}, {Dice2}]{(Doubled ? " D" : null)}{(SourceTongues == null ? null : $" ({SourceTongues.Select((s, i) => $"{s} → {TargetTongues[i]}").JoinString(", ")})")}";
        }
    }
}
