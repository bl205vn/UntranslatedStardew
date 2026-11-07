namespace UntranslatedStardew.Core;

/// <summary>Builds a safe sample from the same resolved locale pair as the ordinary preview.</summary>
internal static class DialogueQuestionPreview
{
    internal static (string Primary, string Secondary)? Create(string primary, string secondary, int repetitions)
    {
        if (repetitions is < 1 or > 3 || !DialogueTranslation.TryGetPlainText(primary, out string main) || main != primary
            || !DialogueTranslation.TryGetPlainText(secondary, out string sub) || sub != secondary)
            return null;
        string Script(string text) => "$q -1 null#" + text + "#$r PreviewA 0 accept#"
            + string.Join(" ", Enumerable.Repeat(text, repetitions)) + "#$r PreviewB 0 refuse#" + text;
        return (Script(primary), Script(secondary));
    }
}
