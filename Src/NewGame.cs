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

                int initialDice1;
                int initialDice2;
                do
                {
                    initialDice1 = Rnd.Next(1, 7);
                    initialDice2 = Rnd.Next(1, 7);
                }
                while (initialDice1 == initialDice2);

                var game = new Game
                {
                    PublicID = publicId,
                    InitialPosition = ClassifyJson.Serialize(Position.StandardInitialPosition).ToString(),
                    Moves = ClassifyJson.Serialize(new[] { new Move { Dice1 = initialDice1, Dice2 = initialDice2 } }).ToString()
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
