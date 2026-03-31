using System.ComponentModel.DataAnnotations;

namespace LiBackgammon
{
    public sealed class Match
    {
        [Key]
        public int ID { get; set; }
        public string FirstGame { get; set; }
        public int MaxScore { get; set; }
        public DoublingCubeRules DoublingCubeRules { get; set; }
    }
}
