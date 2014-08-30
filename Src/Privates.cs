using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        internal readonly HashSet<MainWebSocket> ActiveMainSockets = new HashSet<MainWebSocket>();
        internal readonly Dictionary<string, List<PlayWebSocket>> ActivePlaySockets = new Dictionary<string, List<PlayWebSocket>>();
    }
}
