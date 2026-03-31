namespace LiBackgammon
{
    public sealed class CreateNewMatchResult(Game game, Match match)
    {
        public Game Game { get; private set; } = game;
        public Match Match { get; private set; } = match;
    }
}
