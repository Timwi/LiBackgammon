using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
