using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>Reuses only formatted templates actually selected during vanilla's NPC dialogue construction.</summary>
internal static class GiftAdviceTranslation
{
    internal sealed record Piece(string Path, string Text, object[] Arguments, Gender? SpeakerGender = null);
    internal sealed record Advice(string Npc, string Item, string Primary, Piece[] Pieces,
        string PrimaryNpc, string PrimaryItem, int Generation);

    internal static bool IsAdvicePath(string path)
    {
        const string prefix = "Strings/StringsFromCSFiles:NPC.cs.";
        path = path.Replace('\\', '/');
        return path.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(path[prefix.Length..], out int number)
            && number is >= 4079 and <= 4182;
    }

    internal static Advice? Match(string raw, IReadOnlyList<Piece> captured, string primaryNpc, string primaryItem)
    {
        const string token = "%revealtaste:";
        int at = raw.IndexOf(token, StringComparison.Ordinal);
        if (at <= 0 || raw.IndexOf('%', at + 1) >= 0) return null;
        string[] args = raw[(at + token.Length)..].Split(':', 2);
        if (args.Length != 2 || string.IsNullOrWhiteSpace(args[0]) || string.IsNullOrWhiteSpace(args[1])
            || args.Any(a => a.Any(c => char.IsWhiteSpace(c) || c is '#' or '$' or '^'))) return null;
        string primary = raw[..at];
        Piece[]? match = null;
        for (int start = 0; start < captured.Count; start++)
        {
            string combined = "";
            for (int end = start; end < captured.Count; end++)
            {
                combined += captured[end].Text;
                if (combined.Length > primary.Length) break;
                if (Capitalize(combined) != primary) continue;
                var candidate = captured.Skip(start).Take(end - start + 1).ToArray();
                if (candidate.Any(piece => !IsAdvicePath(piece.Path))) continue;
                if (match != null) return null;
                match = candidate;
            }
        }
        return match == null ? null : new(args[0], args[1], primary, match, primaryNpc, primaryItem, LoadedDialogueSource.Generation);
    }

    internal static string? Translate(Advice advice, Func<string, string?> lookup, string? npcName, string? itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return null;
        string result = "";
        foreach (var piece in advice.Pieces)
        {
            string? template = lookup(piece.Path);
            if (template == null) return null;
            if (piece.SpeakerGender is { } gender)
                template = gender == Gender.Male ? template.Split('/')[0] : template.Split('/').Last();
            var arguments = piece.Arguments.Select(arg => arg is string text
                ? text == advice.PrimaryItem ? itemName : text == advice.PrimaryNpc ? npcName ?? text : text
                : arg).ToArray();
            try { result += string.Format(template, arguments); }
            catch (FormatException) { return null; }
        }
        return Capitalize(result);
    }

    private static string Capitalize(string text) => text.Length == 0 ? text : text[..1].ToUpper() + text[1..];
}
