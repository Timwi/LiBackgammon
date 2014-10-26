using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    public sealed class UserSession
    {
        [Key]
        public string SessionID { get; set; }
        public int UserID { get; set; }
    }
}
