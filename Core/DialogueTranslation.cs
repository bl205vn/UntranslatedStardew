using System.Text.RegularExpressions;
using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>Pairs simple authored segments without running the game's dialogue commands.</summary>
internal static class DialogueTranslation
{
    internal static string? GetSecondaryLine(
        string primary, string secondary, int index, string visibleText, string playerName, Gender gender, uint daysPlayed)
    {
        string[] primaryWeeks = primary.Split("||");
        string[] secondaryWeeks = secondary.Split("||");
        if (primaryWeeks.Length != secondaryWeeks.Length)
            return null;

        int week = GetWeeklyVariantIndex(daysPlayed, primaryWeeks.Length);
        string[] primaryLines = primaryWeeks[week].Split("#$b#");
        string[] secondaryLines = secondaryWeeks[week].Split("#$b#");
        if (primaryLines.Length != secondaryLines.Length || index < 0 || index >= primaryLines.Length)
            return null;

        for (int i = 0; i < primaryLines.Length; i++)
        {
            if (!TryResolveSegment(primaryLines[i], gender, out primaryLines[i])
                || !TryResolveSegment(secondaryLines[i], gender, out secondaryLines[i]))
                return null;
        }

        string primaryText = primaryLines[index].Replace("@", playerName);
        string secondaryText = secondaryLines[index].Replace("@", playerName);
        if (string.IsNullOrWhiteSpace(primaryText) || string.IsNullOrWhiteSpace(secondaryText)
            || !string.Equals(primaryText, visibleText, StringComparison.Ordinal)
            || string.Equals(primaryText, secondaryText, StringComparison.Ordinal)
            || secondaryText.Any(char.IsControl))
            return null;

        return secondaryText;
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
        => text.Split("||").All(variant => variant.Split("#$b#").All(IsSafeSegment));

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
