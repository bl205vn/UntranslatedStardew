namespace UntranslatedStardew.Core;

/// <summary>Places a complete primary page beside its complete, smaller translation block.</summary>
internal static class DialogueBlockLayout
{
    internal readonly record struct Row(string Primary, string Secondary, int Start, int RevealAt, int PrimaryOffset, int SecondaryOffset)
    {
        internal int RevealedCharacters(int position) => Math.Clamp(position - Start, 0, Primary.Length);
    }
    internal sealed record Page(Row[] Rows, int Height);

    internal static Page? Fit(IReadOnlyList<DialogueTextPair> pairs, int width, int height, bool above,
        Func<string, int> primaryHeight, Func<string, float> secondaryWidth, Func<string, float> secondaryHeight)
    {
        if (width <= 0 || height <= 0 || pairs.Count == 0) return null;
        // Authored pairs remain a source-validation tool. They no longer partition
        // the visible page: all primary text must precede all secondary text.
        string primary = string.Join(" ", pairs.Select(pair => pair.Primary));
        string secondary = string.Join(" ", pairs.Select(pair => pair.Secondary));
        if (secondary == primary) secondary = "";
        string wrapped = secondary.Length == 0 ? ""
            : RoleTextWrapper.Wrap(secondary.Replace('^', '\n'), width, secondaryWidth);
        if (wrapped.Split('\n').Any(line => secondaryWidth(line) > width)) return null;
        int mainHeight = primaryHeight(primary);
        int subHeight = wrapped.Length == 0 ? 0 : (int)Math.Ceiling(secondaryHeight(wrapped));
        if (mainHeight <= 0 || subHeight < 0) return null;
        // Native sprite glyphs have negative ascender offsets. Keep the two
        // blocks separated even when the smaller native font is placed first.
        const int top = 4;
        int gap = subHeight > 0 ? 12 : 0;
        int total = top + mainHeight + gap + subHeight;
        if (total > height) return null;
        return new(new[] { new Row(primary, wrapped, 0, primary.Length,
            top + (above ? subHeight + gap : 0), top + (above ? 0 : mainHeight + gap)) }, total);
    }
}
