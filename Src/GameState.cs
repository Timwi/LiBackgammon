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
        WhiteFinished = 8,
        BlackFinished = 9,
        WhiteDoubledBlackRejected = 10,
        BlackDoubledWhiteRejected = 11,
        WhiteResigned = 12,
        BlackResigned = 13
    }
}
