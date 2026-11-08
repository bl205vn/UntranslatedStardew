using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>Reads parallel scripts without executing event commands or text tokens.</summary>
internal static class EventDialogueTranslation
{
    internal sealed record Text(string Primary, string Secondary, string? Actor);

    internal static bool TryNormalizeAsset(string? asset, out string normalized)
    {
        normalized = "";
        if (asset == null) return false;
        asset = asset.Replace('\\', '/');
        const string prefix = "Data/Events/";
        if (!asset.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        string file = asset[prefix.Length..];
        if (string.IsNullOrWhiteSpace(file) || file is "." or ".." || file.Contains('/')
            || file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
        normalized = prefix + file;
        return true;
    }

    internal static string? FindKey(IReadOnlyDictionary<string, string> entries, string id,
        IReadOnlyList<string> liveCommands, Gender gender)
    {
        if (string.IsNullOrWhiteSpace(id) || id == "-1") return null;
        var matches = entries.Where(entry => Event.SplitPreconditions(entry.Key).FirstOrDefault() == id
            && Commands(entry.Value, gender) is { } commands && commands.SequenceEqual(liveCommands))
            .Select(entry => entry.Key).Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    private static string[]? Commands(string script, Gender gender)
    {
        if (script.Contains('[') || script.Contains(']') || script.Contains("{{", StringComparison.Ordinal)) return null;
        return ArgUtility.SplitQuoteAware(Dialogue.applyGenderSwitchBlocks(gender, script), '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, keepQuotesAndEscapes: true);
    }

    internal static Text? Match(string primary, string secondary, IReadOnlyList<string> liveCommands,
        int index, string[] liveArgs, Gender gender)
    {
        // Event.ParseCommands also expands TokenParser expressions. Unknown expansions
        // must fall back; never run TokenParser on a secondary script to reproduce them.
        string[]? main = Commands(primary, gender), sub = Commands(secondary, gender);
        if (main == null || sub == null) return null;
        if (main.Length != sub.Length || index < 0 || index >= main.Length || !main.SequenceEqual(liveCommands)) return null;
        for (int i = 0; i < main.Length; i++)
        {
            var a = ArgUtility.SplitBySpaceQuoteAware(main[i]);
            var b = ArgUtility.SplitBySpaceQuoteAware(sub[i]);
            if (a.Length == 0 || a.Length != b.Length || a[0] != b[0]) return null;
            int textAt = a[0] switch { "speak" => 2, "message" => 1, _ => -1 };
            if (textAt < 0)
            {
                if (!a.SequenceEqual(b)) return null;
                continue;
            }
            if (a.Length != textAt + 1 || (textAt == 2 && a[1] != b[1])) return null;
        }
        var current = ArgUtility.SplitBySpaceQuoteAware(main[index]);
        var translated = ArgUtility.SplitBySpaceQuoteAware(sub[index]);
        if (!current.SequenceEqual(liveArgs)) return null;
        return current[0] switch
        {
            "speak" => new(current[2], translated[2], current[1]),
            "message" => new(current[1], translated[1], null),
            _ => null
        };
    }
}
