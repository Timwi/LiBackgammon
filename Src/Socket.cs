using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private HttpResponse socket(HttpRequest req)
        {
            if (req.Url.Path.Length < 9)
                throw new HttpException(HttpStatusCode._400_BadRequest);

            using (var tr = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
            using (var db = new Db())
            {
                var stuff = req.Url.Path.Substring(1);
                var publicId = stuff.Substring(0, 8);
                var game = db.Games.FirstOrDefault(g => g.PublicID == publicId);
                if (game == null)
                    return HttpResponse.Redirect(req.Url.WithParent(""));

                var playerToken = stuff.Substring(8);
                if (playerToken != "" && playerToken != game.WhiteToken && playerToken != game.BlackToken)
                    return HttpResponse.Redirect(req.Url.WithParent("play/" + game.PublicID));
                var player = playerToken == game.WhiteToken ? Player.White : playerToken == game.BlackToken ? Player.Black : Player.Spectator;

                return new BgWebSocket(this, publicId, player);
            }
        }
    }
}
