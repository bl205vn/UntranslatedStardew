using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Graphics;

namespace UntranslatedStardew.Core;

/// <summary>Wraps our collection labels with the same role font used for drawing, independent of the game locale.</summary>
public static class RoleTextWrapper
{
    public static string WrapForRole(string text, SpriteFont font, int width, FontRole role)
    {
        using (BilingualRenderScope.Begin(role))
            return Wrap(text, width, part => BilingualFontRenderer.TryMeasureString(font, part, out var size)
                ? size.X : font.MeasureString(part).X);
    }

    public static string Wrap(string text, float width, Func<string, float> measure)
    {
        if (string.IsNullOrEmpty(text) || width <= 0) return text;
        var output = new StringBuilder();
        var line = new StringBuilder();
        void BreakLine()
        {
            output.Append(line.ToString().TrimEnd(' ', '\t')).Append('\n');
            line.Clear();
        }
        foreach (Match match in Regex.Matches(text.Normalize(NormalizationForm.FormC).Replace("\r\n", "\n").Replace('\r', '\n'), @"\n|[^\S\n]+|[^\s]+"))
        {
            string token = match.Value;
            if (token == "\n") { BreakLine(); continue; }
            bool whitespace = string.IsNullOrWhiteSpace(token);
            if (measure(line.ToString() + token) > width && line.Length > 0) BreakLine();
            if (whitespace && line.Length == 0) continue;
            if (measure(token) <= width) { line.Append(token); continue; }
            // Long unspaced labels (including CJK) can break at text-element boundaries.
            var elements = StringInfo.GetTextElementEnumerator(token);
            while (elements.MoveNext())
            {
                string element = elements.GetTextElement();
                if (line.Length > 0 && measure(line.ToString() + element) > width) BreakLine();
                line.Append(element);
            }
        }
        return output.Append(line).ToString().TrimEnd(' ', '\t');
    }
}
