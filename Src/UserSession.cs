using System.ComponentModel.DataAnnotations;

namespace LiBackgammon
{
    public sealed class UserSession
    {
        [Key]
        public string SessionID { get; set; }
        public int UserID { get; set; }
    }
}
