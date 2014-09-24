using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    public sealed class Language
    {
        [Key]
        public string HashName { get; set; }
        public string Name { get; set; }
        public string Data { get; set; }
        public bool Approved { get; set; }
    }
}
