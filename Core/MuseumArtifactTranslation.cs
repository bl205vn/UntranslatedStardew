namespace UntranslatedStardew.Core;

/// <summary>Validates the museum's exact item composition before attaching a translation.</summary>
internal static class MuseumArtifactTranslation
{
    internal static DialogueTextPair? Match(string primaryName, string primaryDescription,
        string? secondaryName, string? secondaryDescription, string visible, Func<string, string> parsePrimary)
    {
        bool Safe(string? text) => !string.IsNullOrWhiteSpace(text)
            && !text.Any(char.IsControl)
            && text.IndexOfAny(new[] { '#', '$', '%', '[', ']', '{', '}', '¦', '\\' }) < 0;
        if (!Safe(primaryName) || !Safe(primaryDescription)
            || !Safe(secondaryName) || !Safe(secondaryDescription)) return null;
        string Compose(string name, string description) => " - " + name + " - ^" + description;
        string primary = ObjectDialogueTranslation.VisibleText(parsePrimary(Compose(primaryName, primaryDescription)));
        if (primary != ObjectDialogueTranslation.VisibleText(visible)) return null;
        return new(primary, Compose(secondaryName!, secondaryDescription!).Trim());
    }
}
