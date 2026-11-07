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
        if (parsePrimary(Compose(primaryName, primaryDescription)).Trim() != visible.Trim()) return null;
        return new(visible.Trim(), Compose(secondaryName!, secondaryDescription!).Trim());
    }
}
