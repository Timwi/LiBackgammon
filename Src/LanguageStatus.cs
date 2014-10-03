using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    [Flags]
    public enum LanguageStatus
    {
        Empty = 0 << 0,
        Incomplete = 1 << 0,
        Complete = 2 << 0,
        CompletenessMask = 3 << 0,

        Approved = 1 << 2,
        SuggestionsPending = 1 << 3
    }
}
