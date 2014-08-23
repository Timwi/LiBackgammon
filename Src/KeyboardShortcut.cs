using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class KeyboardShortcutAttribute : Attribute
    {
        public string Shortcut { get; private set; }
        public KeyboardShortcutAttribute(string shortcut) { Shortcut = shortcut; }
    }
}
