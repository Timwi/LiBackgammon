using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBackgammon
{
    [Flags]
    public enum UserFlags
    {
        None = 0,

        CanCreateUsers = 1 << 0,
        CanApproveTranslations = 1 << 1,
        CanApproveStyles = 1 << 2
    }
}
