using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RT.Util.Serialization;

namespace LiBackgammon
{
    public sealed class PossibleMove
    {
        public int[] DiceSequence;
        public int[] SourceTongues;
        public int[] TargetTongues;
        public PossiblePosition EndPosition;
    }
}
