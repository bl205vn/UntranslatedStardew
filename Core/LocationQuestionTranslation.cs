using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>Pairs source-backed object questions without interpreting their response keys or actions.</summary>
internal static class LocationQuestionTranslation
{
    internal sealed record Label(string Key, string Text, string Source);
    internal sealed record Captured(string Prompt, string Source, Label[] Answers);

    internal static Captured? Capture(string prompt, IReadOnlyList<(string Key, string Text)> answers,
        Func<string, string?> sourceFor)
    {
        string? Source(string text) => sourceFor(text) is { } path
            && DialogueTranslation.TryParseSourceKey(path, out string asset, out string key) ? asset + ":" + key : null;
        string? promptSource = Source(prompt);
        if (promptSource == null || answers.Count == 0) return null;
        var labels = new List<Label>();
        foreach (var answer in answers)
        {
            string? source = Source(answer.Text);
            if (source == null || string.IsNullOrEmpty(answer.Key) || labels.Any(label => label.Key == answer.Key)) return null;
            labels.Add(new(answer.Key, answer.Text, source));
        }
        return new(prompt, promptSource, labels.ToArray());
    }

    internal static DialogueQuestionTranslation.Question? Match(Captured captured, string shown,
        IReadOnlyList<(string Key, string Text)> live, Func<string, (string Primary, string Secondary)?> lookup, Gender gender)
    {
        DialogueTextPair? Pair(string path, string original, string visible)
        {
            if (lookup(path) is not { } source) return null;
            var pages = ObjectDialogueTranslation.Match(source.Primary, source.Secondary, original, gender);
            return pages is { Length: 1 } && pages[0].Primary == visible ? pages[0] : null;
        }
        if (captured.Answers.Length != live.Count || Pair(captured.Source, captured.Prompt, shown) is not { } prompt) return null;
        var answers = new DialogueTextPair[live.Count];
        for (int i = 0; i < live.Count; i++)
        {
            var label = captured.Answers[i];
            if (label.Key != live[i].Key || Pair(label.Source, label.Text, live[i].Text) is not { } pair) return null;
            answers[i] = pair;
        }
        return new("location", prompt, Array.Empty<string>(), captured.Answers.Select(label => label.Key).ToArray(), answers);
    }
}
