using System.ComponentModel.DataAnnotations;

namespace LiBackgammon
{
    public sealed class Style
    {
        [Key]
        public string HashName { get; set; }
        public string Name { get; set; }
        public string Css { get; set; }
        public bool Approved { get; set; }
    }
}
