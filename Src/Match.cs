using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
