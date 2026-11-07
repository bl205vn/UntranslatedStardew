using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>Reads question labels without executing either dialogue script.</summary>
internal static class DialogueQuestionTranslation
{
    internal sealed record Question(string Header, DialogueTextPair Prompt, string[] Headers,
        string[] Keys, DialogueTextPair[] Answers);

    internal static Question? Match(string primary, string secondary, string visiblePrompt,
        IReadOnlyList<(string Key, string Text)> responses, string player, Gender gender, uint days)
    {
        if (primary.Split("||").Length != secondary.Split("||").Length)
            return null;
        var main = Parse(primary, player, gender, days);
        var sub = Parse(secondary, player, gender, days);
        if (main == null || sub == null || main.Header != sub.Header
            || !main.Headers.SequenceEqual(sub.Headers) || main.Keys.Length != responses.Count
            || main.Prompt.Primary != visiblePrompt)
            return null;
        for (int i = 0; i < responses.Count; i++)
            if (responses[i].Key != main.Keys[i] || responses[i].Text != main.Answers[i].Primary)
                return null;
        return new Question(main.Header, new(visiblePrompt, sub.Prompt.Primary), main.Headers, main.Keys,
            main.Answers.Select((answer, i) => new DialogueTextPair(answer.Primary, sub.Answers[i].Primary)).ToArray());
    }

    /// <summary>Pairs narration preceding a validated question; question/answer execution remains vanilla.</summary>
    internal static string? GetNarrationLine(string primary, string secondary, int index, string visible,
        string player, Gender gender, uint days)
    {
        string[] mainWeeks = primary.Split("||");
        string[] subWeeks = secondary.Split("||");
        if (mainWeeks.Length != subWeeks.Length)
            return null;
        var main = Parse(primary, player, gender, days);
        var sub = Parse(secondary, player, gender, days);
        if (main == null || sub == null || main.Header != sub.Header || !main.Headers.SequenceEqual(sub.Headers))
            return null;
        int week = DialogueTranslation.GetWeeklyVariantIndex(days, mainWeeks.Length);
        string[] Narration(string source) => source.Split('#').TakeWhile(field => !field.StartsWith("$q ", StringComparison.Ordinal))
            .Where(field => field is not ("$b" or "$e")).ToArray();
        var mainLines = Narration(mainWeeks[week]);
        var subLines = Narration(subWeeks[week]);
        if (index < 0 || index >= mainLines.Length || mainLines.Length != subLines.Length
            || !Text(mainLines[index], player, gender, out string expected) || expected != visible
            || !Text(subLines[index], player, gender, out string translated) || translated == visible)
            return null;
        return translated;
    }

    /// <summary>Resolves an already-answered question's inlined, plain fallback without executing it.</summary>
    internal static string? GetAnsweredFallbackLine(string primary, string secondary, int index, string visible,
        string player, Gender gender, uint days, Func<string, bool> answered,
        Func<string, (string Primary, string Secondary)?> lookup)
    {
        string[] mainWeeks = primary.Split("||");
        string[] subWeeks = secondary.Split("||");
        if (mainWeeks.Length != subWeeks.Length)
            return null;
        var main = Parse(primary, player, gender, days);
        var sub = Parse(secondary, player, gender, days);
        if (main == null || sub == null || main.Header != sub.Header || !main.Headers.SequenceEqual(sub.Headers))
            return null;
        string[] header = ArgUtility.SplitBySpace(main.Header.Split('#')[0]);
        string[] ids = header[1].Split('/');
        if (ids[0] == "-1" || header[2] == "null" || !ids.Any(answered))
            return null;
        int week = DialogueTranslation.GetWeeklyVariantIndex(days, mainWeeks.Length);
        int narrationCount = mainWeeks[week].Split('#')
            .TakeWhile(field => !field.StartsWith("$q ", StringComparison.Ordinal)).Count(field => field is not ("$b" or "$e"));
        int fallbackIndex = index - narrationCount;
        if (fallbackIndex < 0 || lookup(header[2]) is not { } fallback)
            return null;
        // Vanilla inlines this value after its original weekly/token processing. Do not process new branches here.
        bool Plain(string text) => !text.Contains("||", StringComparison.Ordinal)
            && (text.StartsWith("$p ", StringComparison.Ordinal)
                ? DialogueTranslation.IsSafePreview(text)
                : DialogueTranslation.SplitPages(text).All(segment => DialogueTranslation.TryResolveSegment(segment, gender, out _)));
        if (!Plain(fallback.Primary) || !Plain(fallback.Secondary))
            return null;
        return DialogueTranslation.GetSecondaryLine(fallback.Primary, fallback.Secondary, fallbackIndex,
            visible, player, gender, days, answered);
    }

    private static Question? Parse(string source, string player, Gender gender, uint days)
    {
        string[] weeks = source.Split("||");
        source = weeks[DialogueTranslation.GetWeeklyVariantIndex(days, weeks.Length)];
        string[] fields = source.Split('#');
        int questionIndex = Array.FindIndex(fields, field => field.StartsWith("$q ", StringComparison.Ordinal));
        if (questionIndex < 0)
            return null;
        var introduction = fields.Take(questionIndex).ToArray();
        if (introduction.Any(field => field is not ("$b" or "$e") && !Text(field, player, gender, out _)))
            return null;
        fields = fields.Skip(questionIndex).ToArray();
        string shape = string.Join(",", introduction.Select(field => field switch { "$b" => "break", "$e" => "end", _ => "text" }));
        if (fields.Length < 4 || fields.Length % 2 != 0 || !fields[0].StartsWith("$q ", StringComparison.Ordinal))
            return null;
        string[] question = ArgUtility.SplitBySpace(fields[0]);
        if (question.Length != 3 || !SafeId(question[1]) || !SafeId(question[2]))
            return null;
        if (!Text(fields[1], player, gender, out string prompt))
            return null;
        int count = (fields.Length - 2) / 2;
        var headers = new string[count];
        var keys = new string[count];
        var answers = new DialogueTextPair[count];
        for (int i = 0; i < count; i++)
        {
            string header = fields[2 + i * 2];
            string[] args = ArgUtility.SplitBySpace(header);
            if (args.Length is not (4 or 5) || args[0] != "$r" || !SafeId(args[1])
                || !int.TryParse(args[2], out _) || !SafeId(args[3])
                || (args.Length == 5 && !SafeId(args[4]))
                || !Text(fields[3 + i * 2], player, gender, out string label))
                return null;
            headers[i] = header;
            keys[i] = args[3]; // Response.responseKey is the reaction key, not the remembered answer ID.
            answers[i] = new(label, "");
        }
        if (keys.Distinct(StringComparer.Ordinal).Count() != count)
            return null;
        return new(fields[0] + "#" + shape, new(prompt, ""), headers, keys, answers);
    }

    private static bool SafeId(string value)
        => value.Length > 0 && value.All(c => char.IsLetterOrDigit(c) || c is '_' or '.' or '-' or '/');

    private static bool Text(string raw, string player, Gender gender, out string text)
    {
        text = "";
        return DialogueTranslation.TryResolveSegment(raw, gender, out text)
            && !string.IsNullOrWhiteSpace(text = text.Replace("@", player));
    }
}
