using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>Reads parallel scripts without executing event commands or text tokens.</summary>
internal static class EventDialogueTranslation
{
    internal sealed record Text(string Primary, string Secondary, string? Actor);
    internal sealed record QuestionText(DialogueTextPair Prompt, DialogueTextPair[] Answers);

    private static string[] SplitAndTrim(string text, string delimiter)
        => text.Split(delimiter, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static string[] SplitAndTrim(string text, char delimiter)
        => text.Split(delimiter, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    internal static bool TryNormalizeAsset(string? asset, out string normalized, bool allowFestivals = false)
    {
        normalized = "";
        if (asset == null) return false;
        asset = asset.Replace('\\', '/');
        const string eventPrefix = "Data/Events/";
        const string festivalPrefix = "Data/Festivals/";
        string prefix;
        if (asset.StartsWith(eventPrefix, StringComparison.OrdinalIgnoreCase))
            prefix = eventPrefix;
        else if (allowFestivals && asset.StartsWith(festivalPrefix, StringComparison.OrdinalIgnoreCase))
            prefix = festivalPrefix;
        else
            return false;

        string file = asset[prefix.Length..];
        if (string.IsNullOrWhiteSpace(file) || file is "." or ".." || file.Contains('/')
            || file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
        normalized = prefix + file;
        return true;
    }

    internal static bool TryParseFestivalKey(string? translationKey, out string asset, out string key)
    {
        asset = "";
        key = "";
        if (string.IsNullOrWhiteSpace(translationKey)) return false;
        int sep = translationKey.IndexOf(':');
        if (sep <= 0 || sep >= translationKey.Length - 1) return false;
        string candidateAsset = translationKey[..sep];
        string candidateKey = translationKey[(sep + 1)..];
        if (!candidateAsset.Replace('\\', '/').StartsWith("Data/Festivals/", StringComparison.OrdinalIgnoreCase)
            || !TryNormalizeAsset(candidateAsset, out asset, allowFestivals: true)) return false;
        key = candidateKey;
        return true;
    }

    internal static string? FindKey(IReadOnlyDictionary<string, string> entries, string id,
        IReadOnlyList<string> liveCommands, Gender gender, bool isFestival = false)
    {
        if (string.IsNullOrWhiteSpace(id) || id == "-1") return null;
        var matches = entries.Where(entry => (isFestival || Event.SplitPreconditions(entry.Key).FirstOrDefault() == id)
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

    private static bool ValidateScripts(string[]? main, string[]? sub, IReadOnlyList<string> liveCommands, int index)
    {
        if (main == null || sub == null) return false;
        if (main.Length != sub.Length || index < 0 || index >= main.Length || !main.SequenceEqual(liveCommands)) return false;
        for (int i = 0; i < main.Length; i++)
        {
            var a = ArgUtility.SplitBySpaceQuoteAware(main[i]);
            var b = ArgUtility.SplitBySpaceQuoteAware(sub[i]);
            if (a.Length == 0 || b.Length == 0 || a[0] != b[0]) return false;
            if (a[0] != "quickQuestion" && a.Length != b.Length) return false;
            if (a[0] is "speak" or "message")
            {
                int textAt = a[0] == "speak" ? 2 : 1;
                if (a.Length != textAt + 1 || (textAt == 2 && a[1] != b[1])) return false;
                continue;
            }
            if (a[0] == "question")
            {
                if (a.Length != 3 || a[1] != b[1]) return false;
                var aParts = SplitAndTrim(a[2], '#');
                var bParts = SplitAndTrim(b[2], '#');
                if (aParts.Length < 2 || aParts.Length != bParts.Length) return false;
                continue;
            }
            if (a[0] == "quickQuestion")
            {
                var aBreak = SplitAndTrim(main[i].Substring(main[i].IndexOf(' ') + 1), "(break)");
                var bBreak = SplitAndTrim(sub[i].Substring(sub[i].IndexOf(' ') + 1), "(break)");
                if (aBreak.Length != bBreak.Length || aBreak.Length < 1) return false;
                for (int k = 1; k < aBreak.Length; k++)
                    if (aBreak[k] != bBreak[k]) return false;
                var aParts = SplitAndTrim(aBreak[0], '#');
                var bParts = SplitAndTrim(bBreak[0], '#');
                if (aParts.Length < 2 || aParts.Length != bParts.Length) return false;
                continue;
            }
            if (!a.SequenceEqual(b)) return false;
        }
        return true;
    }

    internal static Text? Match(string primary, string secondary, IReadOnlyList<string> liveCommands,
        int index, string[] liveArgs, Gender gender)
    {
        string[]? main = Commands(primary, gender), sub = Commands(secondary, gender);
        if (!ValidateScripts(main, sub, liveCommands, index)) return null;
        var current = ArgUtility.SplitBySpaceQuoteAware(main![index]);
        var translated = ArgUtility.SplitBySpaceQuoteAware(sub![index]);
        if (!current.SequenceEqual(liveArgs)) return null;
        return current[0] switch
        {
            "speak" => new(current[2], translated[2], current[1]),
            "message" => new(current[1], translated[1], null),
            _ => null
        };
    }

    internal static QuestionText? MatchQuestion(string primary, string secondary, IReadOnlyList<string> liveCommands,
        int index, string[] liveArgs, Gender gender)
    {
        string[]? main = Commands(primary, gender), sub = Commands(secondary, gender);
        if (!ValidateScripts(main, sub, liveCommands, index)) return null;
        var current = ArgUtility.SplitBySpaceQuoteAware(main![index]);
        var translated = ArgUtility.SplitBySpaceQuoteAware(sub![index]);
        if (!current.SequenceEqual(liveArgs)) return null;
        if (current[0] == "question")
        {
            var aParts = SplitAndTrim(current[2], '#');
            var bParts = SplitAndTrim(translated[2], '#');
            if (aParts.Length != bParts.Length || aParts.Length < 2) return null;
            var answers = new DialogueTextPair[aParts.Length - 1];
            for (int i = 1; i < aParts.Length; i++)
                answers[i - 1] = new(aParts[i].Trim(), bParts[i].Trim());
            return new(new(aParts[0].Trim(), bParts[0].Trim()), answers);
        }
        if (current[0] == "quickQuestion")
        {
            var aBreak = SplitAndTrim(main[index].Substring(main[index].IndexOf(' ') + 1), "(break)");
            var bBreak = SplitAndTrim(sub[index].Substring(sub[index].IndexOf(' ') + 1), "(break)");
            var aParts = SplitAndTrim(aBreak[0], '#');
            var bParts = SplitAndTrim(bBreak[0], '#');
            if (aParts.Length != bParts.Length || aParts.Length < 2) return null;
            var answers = new DialogueTextPair[aParts.Length - 1];
            for (int i = 1; i < aParts.Length; i++)
                answers[i - 1] = new(aParts[i].Trim(), bParts[i].Trim());
            return new(new(aParts[0].Trim(), bParts[0].Trim()), answers);
        }
        return null;
    }
}
