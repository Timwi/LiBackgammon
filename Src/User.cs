using System.ComponentModel.DataAnnotations;

namespace LiBackgammon
{
    public sealed class User
    {
        [Key]
        public int UserID { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public UserFlags Flags { get; set; }
    }
}
