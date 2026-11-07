using System.Globalization;
using System.Text.RegularExpressions;

namespace UntranslatedStardew.Core;

/// <summary>Interleaves visual rows inside each authored pair without inventing word-level translation alignment.</summary>
internal static class DialogueInterleavedLayout
{
    internal readonly record struct Row(string Primary, string Secondary, int Start, int RevealAt, int PrimaryOffset, int SecondaryOffset)
    {
        internal int RevealedCharacters(int position) => Math.Clamp(position - Start, 0, Primary.Length);
    }
    internal sealed record Page(Row[] Rows, int Height);
    private readonly record struct MainLine(string Text, int Start);

    internal static Page? Fit(IReadOnlyList<DialogueTextPair> pairs, int width, int height, bool above,
        Func<string, int> primaryHeight, Func<string, float> secondaryWidth, Func<string, float> secondaryHeight)
    {
        if (width <= 0 || height <= 0 || pairs.Count == 0) return null;
        int lineHeight = primaryHeight("T");
        if (lineHeight <= 0) return null;
        var rows = new List<Row>();
        // Native Latin glyphs extend above their draw origin. Reserve ascender room
        // between visual rows as well as at the top of the page.
        const int rowGap = 12;
        int top = 4, blockStart = 0;
        foreach (var pair in pairs)
        {
            var main = WrapPrimary(pair.Primary, lineHeight, primaryHeight);
            if (main == null) return null;
            string secondary = pair.Secondary == pair.Primary ? "" : pair.Secondary.Replace('^', '\n');
            string[] sub = secondary.Length == 0 ? Array.Empty<string>()
                : RoleTextWrapper.Wrap(secondary, width, secondaryWidth).Split('\n');
            if (sub.Any(line => secondaryWidth(line) > width)) return null;
            int count = Math.Max(main.Count, sub.Length);
            for (int i = 0; i < count; i++)
            {
                string primary = i < main.Count ? main[i].Text : "";
                string small = i < sub.Length ? sub[i] : "";
                int primarySize = i < main.Count ? lineHeight : 0;
                int secondarySize = small.Length == 0 ? 0 : (int)Math.Ceiling(secondaryHeight(small));
                int gap = primarySize > 0 && secondarySize > 0 ? 4 : 0;
                int rowHeight = primarySize + secondarySize + gap;
                if (top + rowHeight > height) return null;
                int start = blockStart + (i < main.Count ? main[i].Start : pair.Primary.Length);
                int revealAt = i < main.Count ? start + primary.Length : blockStart + pair.Primary.Length;
                rows.Add(new(primary, small, start, revealAt,
                    top + (above ? secondarySize + gap : 0), top + (above ? 0 : primarySize + gap)));
                top += rowHeight + rowGap;
            }
            blockStart += pair.Primary.Length + 1;
        }
        return new(rows.ToArray(), Math.Max(0, top - rowGap));
    }

    private static List<MainLine>? WrapPrimary(string text, int lineHeight, Func<string, int> measure)
    {
        var lines = new List<MainLine>();
        string line = "";
        int start = 0;
        void Flush()
        {
            lines.Add(new(line.TrimEnd(), start));
            line = "";
        }
        foreach (Match token in Regex.Matches(text, @"\^|\n|[^\S\r\n]+|[^\s^]+"))
        {
            string word = token.Value;
            if (word is "^" or "\n") { Flush(); start = token.Index + word.Length; continue; }
            bool whitespace = string.IsNullOrWhiteSpace(word);
            if (line.Length == 0 && whitespace) continue;
            if (line.Length > 0 && measure(line + word) > lineHeight) Flush();
            if (line.Length == 0 && whitespace) continue;
            if (line.Length == 0) start = token.Index;
            if (measure(word) <= lineHeight) { line += word; continue; }
            var elements = StringInfo.GetTextElementEnumerator(word);
            while (elements.MoveNext())
            {
                string element = elements.GetTextElement();
                if (measure(element) > lineHeight) return null;
                if (line.Length > 0 && measure(line + element) > lineHeight) Flush();
                if (line.Length == 0) start = token.Index + elements.ElementIndex;
                line += element;
            }
        }
        if (line.Length > 0 || lines.Count == 0) Flush();
        return lines;
    }
}
