using System.Text.RegularExpressions;
using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>Pairs simple authored segments without running the game's dialogue commands.</summary>
internal static class DialogueTranslation
{
    private static readonly string[] SupportedDialogueAssets =
    {
        "Data/ExtraDialogue",
        "Data/EngagementDialogue",
        "Strings/Characters",
        "Strings/Events",
        "Strings/StringsFromCSFiles",
        "Strings/Locations",
        "Strings/1_6_Strings",
        "Strings/StringsFromMaps",
        "Strings/SimpleNonVillagerDialogues",
        "Strings/animationDescriptions",
        "Strings/SpeechBubbles",
        "Strings/Movies",
        "Strings/Lexicon"
    };

    private static readonly string[] SupportedDialoguePrefixes =
    {
        "Characters/Dialogue/",
        "Strings/schedules/"
    };

    private static readonly string[] PageSeparators = { "#$b#", "#$e#" };

    internal static string[] SplitPages(string text)
        => text.Split(PageSeparators, StringSplitOptions.None);

    internal static bool TryParseSourceKey(string? translationKey, out string asset, out string key)
    {
        asset = "";
        key = "";
        if (string.IsNullOrWhiteSpace(translationKey))
            return false;

        int separator = translationKey.IndexOf(':');
        if (separator <= 0 || separator >= translationKey.Length - 1)
            return false;

        string candidateAsset = translationKey[..separator].Replace('\\', '/');
        string candidateKey = translationKey[(separator + 1)..];
        if (string.IsNullOrWhiteSpace(candidateKey) || !TryNormalizeDialogueAsset(candidateAsset, out string normalizedAsset))
            return false;

        asset = normalizedAsset;
        key = candidateKey;
        return true;
    }

    internal static bool IsSupportedDialogueAsset(string? assetName)
        => TryNormalizeDialogueAsset(assetName, out _);

    private static bool TryNormalizeDialogueAsset(string? assetName, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(assetName))
            return false;

        string candidate = assetName.Replace('\\', '/');
        foreach (string prefix in SupportedDialoguePrefixes)
        {
            if (candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string file = candidate[prefix.Length..];
                if (string.IsNullOrWhiteSpace(file) || file is "." or ".."
                    || file.Contains('/') || file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    return false;

                normalized = prefix + file;
                return true;
            }
        }

        foreach (string exact in SupportedDialogueAssets)
        {
            if (candidate.Equals(exact, StringComparison.OrdinalIgnoreCase))
            {
                normalized = exact;
                return true;
            }
        }

        return false;
    }

    internal static string? GetSecondaryLine(
        string primary, string secondary, int index, string visibleText, string playerName, Gender gender, uint daysPlayed, Func<string, bool>? answered = null)
    {
        if (primary.Contains("$q ", StringComparison.Ordinal) || secondary.Contains("$q ", StringComparison.Ordinal))
            return DialogueQuestionTranslation.GetNarrationLine(primary, secondary, index, visibleText, playerName, gender, daysPlayed);
        string[] primaryWeeks = primary.Split("||");
        string[] secondaryWeeks = secondary.Split("||");
        if (primaryWeeks.Length != secondaryWeeks.Length)
            return null;

        int week = GetWeeklyVariantIndex(daysPlayed, primaryWeeks.Length);
        string main = primaryWeeks[week];
        string sub = secondaryWeeks[week];
        string[] primaryBranches = { main };
        string[] secondaryBranches = { sub };
        if (IsConditional(main) || IsConditional(sub))
        {
            if (!TryGetConditionalBranches(main, out string mainCondition, out primaryBranches)
                || !TryGetConditionalBranches(sub, out string subCondition, out secondaryBranches)
                || mainCondition != subCondition)
                return null;

            if (answered != null && mainCondition.StartsWith("$p:", StringComparison.Ordinal))
            {
                string[] ids = ArgUtility.SplitBySpace(mainCondition[3..]);
                int selected = ids.Any(answered) ? 0 : 1;
                primaryBranches = new[] { primaryBranches[selected] };
                secondaryBranches = new[] { secondaryBranches[selected] };
            }
        }

        bool found = false;
        string? matchedSecondary = null;
        for (int branch = 0; branch < primaryBranches.Length; branch++)
        {
            string[] primaryLines = SplitPages(primaryBranches[branch]);
            string[] secondaryLines = SplitPages(secondaryBranches[branch]);
            if (primaryLines.Length != secondaryLines.Length)
                return null;

            for (int i = 0; i < primaryLines.Length; i++)
            {
                if (!TryResolveSegment(primaryLines[i], gender, out primaryLines[i])
                    || !TryResolveSegment(secondaryLines[i], gender, out secondaryLines[i]))
                    return null;
            }

            if (index < 0 || index >= primaryLines.Length)
                continue;

            string primaryText = primaryLines[index].Replace("@", playerName);
            string secondaryText = secondaryLines[index].Replace("@", playerName);
            if (!string.Equals(primaryText, visibleText, StringComparison.Ordinal))
                continue;

            // Blank and duplicate translations must also participate in ambiguity checks.
            if (found && !string.Equals(matchedSecondary, secondaryText, StringComparison.Ordinal))
                return null;

            found = true;
            matchedSecondary = secondaryText;
        }

        return found && !string.IsNullOrWhiteSpace(visibleText)
            && !string.IsNullOrWhiteSpace(matchedSecondary)
            && matchedSecondary != visibleText && !matchedSecondary.Any(char.IsControl)
                ? matchedSecondary
                : null;
    }

    private static bool IsConditional(string text)
        => text.StartsWith("$d", StringComparison.Ordinal) || text.StartsWith("$query", StringComparison.Ordinal)
            || text.StartsWith("$p", StringComparison.Ordinal);

    private static bool TryGetConditionalBranches(string text, out string condition, out string[] branches)
    {
        condition = "";
        branches = Array.Empty<string>();
        int delimiter = text.IndexOf('#');
        if (delimiter < 0)
            return false;

        string headerText = text[..delimiter];
        bool query = headerText.StartsWith("$query ", StringComparison.Ordinal);
        bool response = headerText.StartsWith("$p ", StringComparison.Ordinal);
        if (response)
        {
            string arguments = headerText[3..].Trim();
            if (arguments.Length == 0 || arguments.Any(char.IsControl)
                || arguments.IndexOfAny(new[] { '#', '|', '$', '[', ']', '{', '}' }) >= 0)
                return false;

            condition = "$p:" + arguments;
        }
        else if (query)
        {
            string expression = headerText[7..].Trim();
            if (expression.Length == 0 || expression.Any(char.IsControl) || expression.Contains('|'))
                return false;
            // Preserve quoted values and argument casing. The secondary query is never evaluated.
            condition = "$query:" + expression;
        }
        else
        {
            string[] header = headerText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (header.Length != 2 || header[0] != "$d")
                return false;

            string state = header[1].ToLowerInvariant();
            if (state == "communitycenter")
                state = "cc";
            if (state is not ("joja" or "cc" or "bus" or "kent"))
                return false;
            condition = "$d:" + state;
        }

        string body = text[(delimiter + 1)..];
        // $p rewrites only the next #-delimited field. Multi-page forms need a separate adapter.
        if (response && body.Contains('#'))
            return false;

        branches = body.Split('|');
        if (query && branches.Length == 1)
            branches = new[] { branches[0], branches[0] };
        return branches.Length == 2
            && branches.All(branch => SplitPages(branch).All(IsSafeSegment));
    }

    internal static DialogueTextPair[]? MatchBlocks(
        string primary, string secondary, string[][]? variants, string playerName)
    {
        if (variants == null)
            return null;

        string[]? primaryBlocks = null;
        string[]? secondaryBlocks = null;
        foreach (string[]? variant in variants)
        {
            if (variant == null || variant.Length < 2
                || variant.Any(block => block == null || !TryGetPlainText(block, out string plain) || plain != block))
                continue;

            string[] blocks = variant.Select(block => block.Replace("@", playerName)).ToArray();
            string joined = string.Join(" ", blocks);
            if (string.Equals(joined, primary, StringComparison.Ordinal))
            {
                if (primaryBlocks != null)
                    return null;

                primaryBlocks = blocks;
            }
            else if (string.Equals(joined, secondary, StringComparison.Ordinal))
            {
                if (secondaryBlocks != null)
                    return null;

                secondaryBlocks = blocks;
            }
        }

        if (primaryBlocks == null || secondaryBlocks == null || primaryBlocks.Length != secondaryBlocks.Length)
            return null;

        return primaryBlocks.Zip(secondaryBlocks, (main, sub) => new DialogueTextPair(main, sub)).ToArray();
    }

    private static readonly Regex GenderBlock = new(@"\$\{([^{}$]*)\}\$");

    private static bool IsSafeSegment(string segment)
    {
        bool validBlocks = true;
        string plain = GenderBlock.Replace(segment, match =>
        {
            string contents = match.Groups[1].Value;
            char separator = contents.Contains('¦') ? '¦' : '^';
            string[] variants = contents.Split(separator);
            validBlocks &= variants.Length is 2 or 3
                && variants.All(variant => !variant.Any(char.IsControl)
                    && variant.IndexOfAny(new[] { '#', '$', '%', '[', ']', '{', '}', '^', '¦', '|', '\\' }) < 0);
            return "xx";
        });

        string[] legacy = plain.Split(new[] { '^', '¦' });
        return validBlocks && legacy.Length <= 2 && legacy.All(part => TryGetPlainText(part, out _));
    }

    internal static int GetWeeklyVariantIndex(uint daysPlayed, int variantCount)
        => (int)(daysPlayed / 7 % (uint)variantCount);

    internal static bool IsSafePreview(string text)
        => text.Split("||").All(variant =>
        {
            if (!IsConditional(variant))
                return SplitPages(variant).All(IsSafeSegment);
            if (!TryGetConditionalBranches(variant, out string condition, out _))
                return false;

            return !condition.StartsWith("$query:", StringComparison.Ordinal)
                || condition is "$query:TRUE" or "$query:FALSE"
                || Regex.IsMatch(condition, @"^\$query:YEAR [0-9]+(?: [0-9]+)?$");
        });

    internal static bool TryResolveSegment(string segment, Gender gender, out string text)
    {
        text = "";
        if (!IsSafeSegment(segment))
            return false;

        // Vanilla resolves blocks and ¦ first, then the legacy ^ split.
        string selected = Dialogue.applyGenderSwitch(gender, segment, altTokenOnly: true);
        selected = Dialogue.applyGenderSwitch(gender, selected);
        // Empty alternatives still occupy the authored slot. They have nothing to render.
        return string.IsNullOrWhiteSpace(selected) || TryGetPlainText(selected, out text);
    }

    internal static bool TryGetPlainText(string value, out string text)
    {
        text = value.Trim();
        int portrait = text.LastIndexOf('$');
        if (portrait >= 0)
        {
            string command = text[portrait..];
            if (command is "$h" or "$s" or "$u" or "$l" or "$a"
                || (command.Length > 1 && command.Skip(1).All(character => character is >= '0' and <= '9')))
                text = text[..portrait].TrimEnd();
        }

        return text.Length >= 2
            && !text.Any(char.IsControl)
            && text.IndexOfAny(new[] { '#', '$', '%', '[', ']', '{', '}', '^', '¦', '|', '\\' }) < 0;
    }
}
