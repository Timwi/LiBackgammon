using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    public static class Tongues
    {
        // 0 – 23 are the tongues actually on the board
        public const int WhitePrison = 24;
        public const int BlackPrison = 25;
        public const int WhiteHome = 26;
        public const int BlackHome = 27;

        public const int NumTongues = 28;

        public static int Prison(bool white) { return white ? WhitePrison : BlackPrison; }
        public static int Home(bool white) { return white ? WhiteHome : BlackHome; }
    }
}
