namespace UntranslatedStardew.Core;

/// <summary>Pairs simple authored segments without running the game's dialogue commands.</summary>
internal static class DialogueTranslation
{
    internal static string? GetSecondaryLine(
        string primary, string secondary, int index, string visibleText, string playerName)
    {
        string[] primaryLines = primary.Split("#$b#");
        string[] secondaryLines = secondary.Split("#$b#");
        if (primaryLines.Length != secondaryLines.Length || index < 0 || index >= primaryLines.Length)
            return null;

        for (int i = 0; i < primaryLines.Length; i++)
        {
            if (!TryGetPlainText(primaryLines[i], out primaryLines[i])
                || !TryGetPlainText(secondaryLines[i], out secondaryLines[i]))
                return null;
        }

        string primaryText = primaryLines[index].Replace("@", playerName);
        string secondaryText = secondaryLines[index].Replace("@", playerName);
        if (!string.Equals(primaryText, visibleText, StringComparison.Ordinal)
            || string.Equals(primaryText, secondaryText, StringComparison.Ordinal)
            || secondaryText.Any(char.IsControl))
            return null;

        return secondaryText;
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
