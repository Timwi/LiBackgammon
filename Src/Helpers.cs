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

        public static Game CreateNewGame(this Db db, CreateNewGameOption option, bool doublingCube, Visibility visibility, int? match = null, int? gameInMatch = null, bool isRandom = false)
        {
            string publicId;
            do
            {
                publicId = Rnd.GenerateString(8);
            }
            while (db.Games.Any(g => g.PublicID == publicId));

            var whiteToken = option == CreateNewGameOption.BlackWaits ? null : Rnd.GenerateString(4);
            var blackToken = option == CreateNewGameOption.WhiteWaits ? null : Rnd.GenerateString(4);

            var moves = "[]";
            var state = isRandom ? GameState.Random_Waiting : option == CreateNewGameOption.WhiteWaits ? GameState.White_Waiting : GameState.Black_Waiting;

            if (option == CreateNewGameOption.RollAlready)
            {
                int initialDice1 = Rnd.Next(1, 7);
                int initialDice2 = Rnd.Next(1, 6);
                if (initialDice2 >= initialDice1)
                    initialDice2++;
                moves = ClassifyJson.Serialize(new[] { new Move { Dice1 = initialDice1, Dice2 = initialDice2 } }).ToString();
                state = initialDice1 > initialDice2 ? GameState.White_ToMove : GameState.Black_ToMove;
            }

            var game = new Game
            {
                PublicID = publicId,
                InitialPosition = ClassifyJson.Serialize(doublingCube ? Position.StandardInitialPosition : Position.NoCubeInitialPosition).ToString(),
                Moves = moves,
                State = state,
                WhiteScore = 0,
                BlackScore = 0,
                WhiteToken = whiteToken,
                BlackToken = blackToken,
                RematchOffer = RematchOffer.None,
                Match = match,
                GameInMatch = gameInMatch,
                HasDoublingCube = doublingCube,
                Visibility = visibility
            };
            db.Games.Add(game);
            return game;
        }

        public static CreateNewMatchResult CreateNewMatch(this Db db, CreateNewGameOption option, int playTo, DoublingCubeRules cubeRules, Visibility visibility, bool isRandom = false)
        {
            Match match = null;
            if (playTo > 1)
            {
                match = new Match { DoublingCubeRules = cubeRules, MaxScore = playTo };
                db.Matches.Add(match);
                db.SaveChanges();
            }

            var game = db.CreateNewGame(
                option: option,
                doublingCube: cubeRules == DoublingCubeRules.Standard || (cubeRules == DoublingCubeRules.Crawford && playTo > 1),
                match: match.NullOr(m => m.ID),
                gameInMatch: match.NullOr(m => 1),
                visibility: visibility,
                isRandom: isRandom);

            if (match != null)
                match.FirstGame = game.PublicID;

            return new CreateNewMatchResult(game, match);
        }
    }
}
