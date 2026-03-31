using System.ComponentModel.DataAnnotations;

namespace LiBackgammon
{
    public sealed class Language
    {
        [Key]
        public string HashName { get; set; }
        public string Name { get; set; }
        public string Data { get; set; }
        public bool Approved { get; set; }
        public DateTime LastChange { get; set; }
    }
}
