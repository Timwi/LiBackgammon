using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    public enum DoublingCubeRules
    {
        [KeyboardShortcut("n")]
        None,

        [KeyboardShortcut("t")]
        Standard,

        [KeyboardShortcut("c")]
        Crawford
    }
}
