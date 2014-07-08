using System.ComponentModel.DataAnnotations;

namespace LiBackgammon
{
    public sealed class Game
    {
        [Key]
        public string PublicID { get; set; }
        public string WhiteToken { get; set; }
        public string BlackToken { get; set; }
        public string InitialPosition { get; set; }  // Classify’d Position
        public string Moves { get; set; } // Classify’d Move[]
        public GameState State { get; set; }
    }
}
