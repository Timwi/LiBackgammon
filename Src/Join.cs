using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;
using RT.Util.Serialization;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private HttpResponse join(HttpRequest req)
        {
            if (req.Url.Path.Length != 9)
                return HttpResponse.Redirect(req.Url.WithParent("play" + req.Url.Path));

            using (var tr = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
            using (var db = new Db())
            {
                var stuff = req.Url.Path.Substring(1);
                var publicId = stuff.Substring(0, 8);
                var game = db.Games.FirstOrDefault(g => g.PublicID == publicId);
                if (game == null)
                    return HttpResponse.Redirect(req.Url.WithParent(""));

                if (game.WhiteToken != null && game.BlackToken != null)
                    return HttpResponse.Redirect(req.Url.WithParent("play/{0}#shucks".Fmt(publicId)));

                var newToken = Rnd.GenerateString(4);
                if (game.WhiteToken == null)
                    game.WhiteToken = newToken;
                else
                    game.BlackToken = newToken;

                var moves = game.Moves.ToMoves();
                game.State = moves[0].Dice1 > moves[0].Dice2 ? GameState.WhiteToMove : GameState.BlackToMove;

                db.SaveChanges();
                tr.Complete();

                return HttpResponse.Redirect(req.Url.WithParent("play/" + publicId + newToken));
            }
        }
    }
}
