using RT.Serialization;

namespace LiBackgammon
{
    public sealed class LanguageData
    {
        // Maps from CSS selector to translated text
        [ClassifyNotNull, ClassifyIgnoreIfEmpty, ClassifyIgnoreIfDefault]
        public Dictionary<string, string> Translations = [];

        // Keys are the translator tokens
        [ClassifyNotNull, ClassifyIgnoreIfEmpty, ClassifyIgnoreIfDefault]
        public Dictionary<string, LanguageSuggestion> Suggestions = [];
    }

    public sealed class LanguageSuggestion
    {
        public DateTime LastChange;

        // Maps from CSS selector to translated text
        [ClassifyNotNull, ClassifyIgnoreIfEmpty, ClassifyIgnoreIfDefault]
        public Dictionary<string, string> Translations = [];
    }
}
