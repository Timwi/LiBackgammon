using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    public sealed class CreateNewMatchResult
    {
        public Game Game { get; private set; }
        public Match Match { get; private set; }
        public CreateNewMatchResult(Game game, Match match)
        {
            Game = game;
            Match = match;
        }
    }
}
