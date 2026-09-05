namespace Game.Dialogue
{
    public sealed class DialogueLine
    {
        public const float WaitForAdvance = -1f;
        public const float DefaultCharsPerSecond = 0f;

        public DialogueLine(
            string id,
            string speaker,
            string text,
            float autoAdvanceDelay = WaitForAdvance,
            float charsPerSecond = DefaultCharsPerSecond)
        {
            Id = id ?? string.Empty;
            Speaker = speaker ?? string.Empty;
            Text = text ?? string.Empty;
            AutoAdvanceDelay = autoAdvanceDelay;
            CharsPerSecond = charsPerSecond;
        }

        public string Id { get; }
        public string Speaker { get; }
        public string Text { get; }
        public float AutoAdvanceDelay { get; }
        public float CharsPerSecond { get; }

        public bool WaitsForAdvance => AutoAdvanceDelay < 0f;

        public static DialogueLine Missing(string id)
        {
            var key = id ?? string.Empty;
            return new DialogueLine(key, string.Empty, "[" + key + "]");
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Speaker) ? $"{Id}: {Text}" : $"{Id}: {Speaker} / {Text}";
        }
    }
}
