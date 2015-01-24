using System;
using System.ComponentModel.DataAnnotations;

namespace LiBackgammon
{
    public sealed class ChatMessage
    {
        [Key]
        public int ID { get; set; }
        public string GameID { get; set; }
        public Player Player { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }
        public bool Seen { get; set; }
    }
}
