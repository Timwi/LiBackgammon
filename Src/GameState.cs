using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    public enum GameState
    {
        White_Waiting = 0,
        Black_Waiting = 1,
        White_ToRoll = 2,
        Black_ToRoll = 3,
        White_ToConfirmDouble = 4,
        Black_ToConfirmDouble = 5,
        White_ToMove = 6,
        Black_ToMove = 7,
        White_Won_Finished = 8,
        Black_Won_Finished = 9,
        White_Won_DeclinedDouble = 10,
        Black_Won_DeclinedDouble = 11,
        White_Won_Resignation = 12,
        Black_Won_Resignation = 13,
        Random_Waiting = 14
    }
}
