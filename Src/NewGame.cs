﻿using RT.Servers;
using RT.Util;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private HttpResponse newGame(HttpRequest req)
        {
            bool black;
            if (req.Post["playas"].Value == "black")
                black = true;
            else if (req.Post["playas"].Value == "white")
                black = false;
            else if (req.Post["playas"].Value == "random")
                black = Rnd.Next(0, 2) == 0;
            else
                return HttpResponse.Redirect(req.Url.WithParent(""));

            var playTo = int.Parse(req.Post["playto"].Value);
            var cubeRules = EnumStrong.Parse<DoublingCubeRules>(req.Post["cube"].Value);
            var visibility = EnumStrong.Parse<Visibility>(req.Post["visibility"].Value);

            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var result = db.CreateNewMatch(
                    black ? CreateNewGameOption.BlackWaits : CreateNewGameOption.WhiteWaits,
                    playTo, cubeRules, visibility, req.Post["playas"].Value == "random");
                NotifySocketsAddGame(result.Game, result.Match);
                db.SaveChanges();
                tr.Complete();
                return HttpResponse.Redirect(req.Url.WithParent("play/" + result.Game.PublicID + (black ? result.Game.BlackToken : result.Game.WhiteToken)));
            }
        }
    }
}
