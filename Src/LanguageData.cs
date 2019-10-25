using System;
using System.Collections.Generic;
using RT.Serialization;

namespace LiBackgammon
{
    public sealed class LanguageData
    {
        // Maps from CSS selector to translated text
        [ClassifyNotNull, ClassifyIgnoreIfEmpty, ClassifyIgnoreIfDefault]
        public Dictionary<string, string> Translations = new Dictionary<string, string>();

        // Keys are the translator tokens
        [ClassifyNotNull, ClassifyIgnoreIfEmpty, ClassifyIgnoreIfDefault]
        public Dictionary<string, LanguageSuggestion> Suggestions = new Dictionary<string, LanguageSuggestion>();
    }

    public sealed class LanguageSuggestion
    {
        public DateTime LastChange;

        // Maps from CSS selector to translated text
        [ClassifyNotNull, ClassifyIgnoreIfEmpty, ClassifyIgnoreIfDefault]
        public Dictionary<string, string> Translations = new Dictionary<string, string>();
    }
}
