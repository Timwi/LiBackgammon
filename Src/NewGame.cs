using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using RT.Servers;
using RT.Util;
using RT.Util.Serialization;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private HttpResponse newGame(HttpRequest req)
        {
            bool black;
            if (req.Url.Path == "/black")
                black = true;
            else if (req.Url.Path == "/white")
                black = false;
            else
                return HttpResponse.Redirect(req.Url.WithParent(""));

            using (var tr = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
            using (var db = new Db())
            {
                string publicId;
                do
                {
                    publicId = Rnd.GenerateString(8);
                }
                while (db.Games.Any(g => g.PublicID == publicId));

                var game = new Game
                {
                    PublicID = publicId,
                    InitialPosition = ClassifyJson.Serialize(Position.StandardInitialPosition).ToString(),
                    Moves = "[]"
                };
                var token = Rnd.GenerateString(4);
                if (black)
                    game.BlackToken = token;
                else
                    game.WhiteToken = token;
                db.Games.Add(game);
                db.SaveChanges();
                tr.Complete();

                return HttpResponse.Redirect(req.Url.WithParent("play/" + publicId + token));
            }
        }
    }
}
