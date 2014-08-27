using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        internal readonly Dictionary<string, List<PlayWebSocket>> ActiveSockets = new Dictionary<string, List<PlayWebSocket>>();
    }
}
