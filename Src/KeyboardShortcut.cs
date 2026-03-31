namespace LiBackgammon
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class KeyboardShortcutAttribute(string shortcut) : Attribute
    {
        public string Shortcut { get; private set; } = shortcut;
    }
}
