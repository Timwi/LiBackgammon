using System;
using RT.Util.ExtensionMethods;
using RT.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using RT.Servers;
using System.Threading;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private HttpResponse mainSocket(HttpRequest req)
        {
            return new MainWebSocket(this, req.Url);
        }
    }
}
