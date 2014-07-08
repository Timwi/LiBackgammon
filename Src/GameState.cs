using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    public enum GameState
    {
        WhiteWaiting = 0,
        BlackWaiting = 1,
        WhiteToRoll = 2,
        BlackToRoll = 3,
        WhiteToConfirmDouble = 4,
        BlackToConfirmDouble = 5,
        WhiteToMove = 6,
        BlackToMove = 7,
        WhiteWon = 8,
        BlackWon = 9
    }
}
