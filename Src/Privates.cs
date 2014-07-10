using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        internal Dictionary<string, List<BgWebSocket>> ActiveSockets = new Dictionary<string, List<BgWebSocket>>();
    }
}
