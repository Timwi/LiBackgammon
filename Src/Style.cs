﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
