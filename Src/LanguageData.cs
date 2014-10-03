using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RT.Util.Serialization;

namespace LiBackgammon
{
    public sealed class LanguageData
    {
        // Maps from CSS selector to translated text
        [ClassifyNotNull, ClassifyIgnoreIfEmpty, ClassifyIgnoreIfDefault]
        public Dictionary<string, string> Translations = new Dictionary<string, string>();

        // Maps from Token to map from CSS selector to translated text
        [ClassifyNotNull, ClassifyIgnoreIfEmpty, ClassifyIgnoreIfDefault]
        public Dictionary<string, Dictionary<string, string>> Suggestions = new Dictionary<string, Dictionary<string, string>>();
    }
}
