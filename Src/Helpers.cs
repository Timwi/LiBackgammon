using System.Collections.Generic;
using System.Linq;
using RT.Util;
using RT.Util.Json;
using RT.Util.Serialization;

namespace LiBackgammon
{
    public static class Helpers
    {
        public static List<Move> ToMoves(this string json)
        {
            return ClassifyJson.Deserialize<List<Move>>(JsonValue.Parse(json));
        }

        public static Position ToPosition(this string json)
        {
            return ClassifyJson.Deserialize<Position>(JsonValue.Parse(json));
        }

        public static int[] GetInts(this JsonValue json)
        {
            return json.GetList().Select(v => v.GetInt()).ToArray();
        }

        public static CreateNewGameResult CreateNewGame(this Db db, CreateNewGameOption option, bool doublingCube, Visibility visibility, bool isCrawford = false, int? match = null, int? gameInMatch = null)
        {
            string publicId;
            do
            {
                publicId = Rnd.GenerateString(8);
            }
            while (db.Games.Any(g => g.PublicID == publicId));

            var result = new CreateNewGameResult(
                publicId,
                option == CreateNewGameOption.BlackWaits ? null : Rnd.GenerateString(4),
                option == CreateNewGameOption.WhiteWaits ? null : Rnd.GenerateString(4));

            var moves = "[]";
            var state = option == CreateNewGameOption.WhiteWaits ? GameState.White_Waiting : GameState.Black_Waiting;

            if (option == CreateNewGameOption.RollAlready)
            {
                int initialDice1 = Rnd.Next(1, 7);
                int initialDice2 = Rnd.Next(1, 6);
                if (initialDice2 >= initialDice1)
                    initialDice2++;
                moves = ClassifyJson.Serialize(new[] { new Move { Dice1 = initialDice1, Dice2 = initialDice2 } }).ToString();
                state = initialDice1 > initialDice2 ? GameState.White_ToMove : GameState.Black_ToMove;
            }

            db.Games.Add(new Game
            {
                PublicID = publicId,
                InitialPosition = ClassifyJson.Serialize(doublingCube ? Position.StandardInitialPosition : Position.NoCubeInitialPosition).ToString(),
                Moves = moves,
                State = state,
                WhiteScore = 0,
                BlackScore = 0,
                WhiteToken = result.WhiteToken,
                BlackToken = result.BlackToken,
                RematchOffer = RematchOffer.None,
                Match = match,
                GameInMatch = gameInMatch,
                IsCrawfordGame = isCrawford,
                Visibility = visibility
            });

            return result;
        }

        public static CreateNewGameResult CreateNewMatch(this Db db, CreateNewGameOption option, int playTo, DoublingCubeRules cubeRules, Visibility visibility)
        {
            Match match = null;
            if (playTo > 1)
            {
                match = new Match { DoublingCubeRules = cubeRules, MaxScore = playTo };
                db.Matches.Add(match);
                db.SaveChanges();
            }

            var result = db.CreateNewGame(
                option: option,
                doublingCube: cubeRules == DoublingCubeRules.Standard || (cubeRules == DoublingCubeRules.Crawford && playTo > 1),
                isCrawford: false,
                match: match.NullOr(m => m.ID),
                gameInMatch: match.NullOr(m => 1),
                visibility: visibility);

            if (match != null)
                match.FirstGame = result.PublicID;

            return result;
        }
    }
}
