using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>Pairs plain object-message pages, using object dialogue syntax rather than NPC command syntax.</summary>
internal static class ObjectDialogueTranslation
{
    // DialogueBox.getCurrentString removes platform newlines after trimming.
    // Keep authored '^' intact: it remains a SpriteText display break.
    internal static string VisibleText(string text) => text.Trim().Replace(Environment.NewLine, "");

    internal static DialogueTextPair[]? Match(string primary, string secondary, string displayed, Gender gender)
    {
        // This mirrors LocalizedContentManager.PreprocessString; ^ here remains a display newline.
        string Main(string text) => Dialogue.applyGenderSwitch(gender, text, altTokenOnly: true);
        primary = Main(primary);
        secondary = Main(secondary);
        if (primary != displayed) return null;
        string[] main = primary.Split('#');
        string[] sub = secondary.Split('#');
        if (main.Length != sub.Length) return null;
        bool Safe(string page) => !page.Any(char.IsControl)
            && page.IndexOfAny(new[] { '$', '%', '[', ']', '{', '}', '¦', '\\' }) < 0;
        if (!main.All(Safe) || !sub.All(Safe)) return null;
        return main.Select((text, i) => new DialogueTextPair(text.Trim(), sub[i].Trim().Replace('^', '\n').Normalize())).ToArray();
    }

    internal static int CurrentPage(IReadOnlyList<DialogueTextPair> pages, IReadOnlyList<string> remaining)
    {
        int index = pages.Count - remaining.Count;
        if (index < 0 || remaining.Count == 0) return -1;
        for (int i = 0; i < remaining.Count; i++)
            if (VisibleText(remaining[i]) != pages[index + i].Primary) return -1;
        return index;
    }
}
