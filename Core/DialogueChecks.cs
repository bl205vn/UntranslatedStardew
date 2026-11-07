using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>Deterministic smoke checks for the production dialogue resolver and layout.</summary>
internal static class DialogueChecks
{
    internal static (string Name, bool Passed)[]? Run(string group)
        => group switch
        {
            "branch" => CheckBranches().Concat(CheckWorldState()).Concat(CheckQueries()).Concat(CheckResponses()).ToArray(),
            "state" => CheckWorldState(),
            "query" => CheckQueries(),
            "response" => CheckResponses(),
            "question" => CheckQuestions(),
            "pairs" => CheckPairs(),
            "fallback" => CheckFallback(),
            "layout" => CheckLayout(),
            "all" => CheckBranches().Concat(CheckWorldState()).Concat(CheckQueries()).Concat(CheckResponses()).Concat(CheckQuestions()).Concat(CheckPairs()).Concat(CheckFallback()).Concat(CheckLayout()).ToArray(),
            _ => null
        };

    private static string? Resolve(string primary, string secondary, string visible, int page = 0,
        Gender gender = Gender.Male, uint daysPlayed = 0)
        => DialogueTranslation.GetSecondaryLine(primary, secondary, page, visible, "Alex", gender, daysPlayed);

    private static (string, bool)[] CheckBranches()
    {
        const string primary = "Hello ${sir^madam}$!";
        const string secondary = "Chào ${anh^chị}$!";
        return new[]
        {
            ("Gender block selects the matching branch for male and female",
                Resolve(primary, secondary, "Hello sir!") == "Chào anh!"
                && Resolve(primary, secondary, "Hello madam!", gender: Gender.Female) == "Chào chị!"),
            ("Legacy gender delimiter and an empty alternative",
                Resolve("Hello, sir.^Hello, madam.", "Chào anh.¦Chào chị.", "Hello, madam.", gender: Gender.Female) == "Chào chị."
                && Resolve("${^}$Hello.", "Chào${^ chị}$.", "Hello.") == "Chào."),
            ("Weekly variant changes at day 7 and loops at day 14",
                Resolve("First.||Second.", "Đầu.||Sau.", "First.", daysPlayed: 6) == "Đầu."
                && Resolve("First.||Second.", "Đầu.||Sau.", "Second.", daysPlayed: 7) == "Sau."
                && Resolve("First.||Second.", "Đầu.||Sau.", "First.", daysPlayed: 14) == "Đầu.")
        };
    }

    private static (string, bool)[] CheckWorldState()
    {
        const string primary = "$d kent#Kent is home.|Kent is away.";
        const string secondary = "$d kent#Kent đã về.|Kent đi vắng.";
        return new[]
        {
            ("World-state branch follows the live primary text",
                Resolve(primary, secondary, "Kent is home.") == "Kent đã về."
                && Resolve(primary, secondary, "Kent is away.") == "Kent đi vắng."),
            ("World-state conditions must agree",
                Resolve(primary, secondary.Replace("$d kent", "$d bus"), "Kent is home.") == null),
            ("Indistinguishable world-state branches fall back",
                Resolve("$d kent#Hello.|Hello.", "$d kent#Chào.|Xin chào.", "Hello.") == null
                && Resolve("$d kent#Hello.|Hello.", "$d kent#Hello.|Xin chào.", "Hello.") == null),
            ("Unsafe inactive branches are rejected before a console preview",
                !DialogueTranslation.IsSafePreview("$d kent#Hello.|$action AddMoney 1")
                && Resolve(primary, "$d kent#Kent đã về.|Here is [74].", "Kent is home.") == null)
        };
    }

    private static (string, bool)[] CheckQueries()
    {
        const string primary = "$query YEAR 2#Second year.|First year.";
        const string secondary = "$query YEAR 2#Năm hai.|Năm đầu.";
        return new[]
        {
            ("Query branches follow the visible text without evaluating the secondary condition",
                Resolve(primary, secondary, "Second year.") == "Năm hai."
                && Resolve(primary, secondary, "First year.") == "Năm đầu."),
            ("Query headers must match, including quoted argument casing",
                Resolve(primary, secondary.Replace("YEAR 2", "YEAR 3"), "Second year.") == null
                && Resolve("$query CUSTOM \"Abigail\"#Yes.|No.", "$query CUSTOM \"abigail\"#Có.|Không.", "Yes.") == null),
            ("Single-branch queries use the same text for either outcome",
                Resolve("$query FALSE#Hello.", "$query FALSE#Xin chào.", "Hello.") == "Xin chào."),
            ("Uncertain query branches and unsafe preview queries fall back",
                Resolve("$query TRUE#Hello.|Hello.", "$query TRUE#Chào.|Xin chào.", "Hello.") == null
                && !DialogueTranslation.IsSafePreview("$query CUSTOM#Hello.|Goodbye.")
                && DialogueTranslation.IsSafePreview(primary))
        };
    }

    private static (string, bool)[] CheckResponses()
    {
        const string primary = "$p AnswerA#You agreed.|You refused.";
        const string secondary = "$p AnswerA#Bạn đồng ý.|Bạn từ chối.";
        return new[]
        {
            ("Previous-response branches follow the live primary outcome",
                Resolve(primary, secondary, "You agreed.") == "Bạn đồng ý."
                && Resolve(primary, secondary, "You refused.") == "Bạn từ chối."),
            ("Response IDs and condition types must match",
                Resolve(primary, secondary.Replace("AnswerA", "answera"), "You agreed.") == null
                && Resolve(primary, secondary.Replace("AnswerA", "AnswerB"), "You agreed.") == null
                && Resolve(primary, "$query TRUE#Bạn đồng ý.|Bạn từ chối.", "You agreed.") == null),
            ("Ambiguous previous-response text falls back",
                Resolve("$p AnswerA#Hello.|Hello.", "$p AnswerA#Chào.|Xin chào.", "Hello.") == null
                && Resolve("$p AnswerA#Hello.|Hello.", "$p AnswerA#Chào.|Chào.", "Hello.") == "Chào."),
            ("Previous answers disambiguate identical primary text",
                DialogueTranslation.GetSecondaryLine("$p AnswerA#Hello.|Hello.", "$p AnswerA#Đã chọn.|Chưa chọn.",
                    0, "Hello.", "Alex", Gender.Male, 0, id => id == "AnswerA") == "Đã chọn."
                && DialogueTranslation.GetSecondaryLine("$p AnswerA#Hello.|Hello.", "$p AnswerA#Đã chọn.|Chưa chọn.",
                    0, "Hello.", "Alex", Gender.Male, 0, _ => false) == "Chưa chọn."),
            ("Response preview rejects extra pages and actions",
                DialogueTranslation.IsSafePreview(primary)
                && !DialogueTranslation.IsSafePreview("$p AnswerA#Hello.|Goodbye.#$b#Another page.")
                && !DialogueTranslation.IsSafePreview("$p AnswerA#Hello.|$action AddMoney 1"))
        };
    }

    private static (string, bool)[] CheckQuestions()
    {
        const string main = "$q -1 null#Hello @!#$r Yes 0 accept#Yes please.#$r No -10 refuse#No thanks.";
        const string sub = "$q -1 null#Chào @!#$r Yes 0 accept#Vâng ạ.#$r No -10 refuse#Không cảm ơn.";
        var answers = new[] { ("accept", "Yes please."), ("refuse", "No thanks.") };
        DialogueQuestionTranslation.Question? Match(string p, string s, (string, string)[]? r = null)
            => DialogueQuestionTranslation.Match(p, s, "Hello Alex!", r ?? answers, "Alex", Gender.Male, 0);
        var matched = Match(main, sub);
        var pairs = new[] { new DialogueTextPair("Prompt", "Câu hỏi"), new DialogueTextPair("Yes", "Một câu trả lời dài cần xuống dòng") };
        Dictionary<string, DialogueQuestionLayout.Row>? Fit(int height, bool above = true)
            => DialogueQuestionLayout.Fit(pairs, 80, height, above, _ => 20, text => text.Length * 5, text => (1 + text.Count(c => c == '\n')) * 12);
        return new[]
        {
            ("Question prompt and answers pair by reaction key and live order",
                matched?.Prompt.Secondary == "Chào Alex!" && matched.Answers[1].Secondary == "Không cảm ơn."
                && Match("Introduction.#" + main, "Mở đầu.#" + sub) != null
                && Match(main, sub, new[] { answers[1], answers[0] }) == null),
            ("Question IDs, friendship and reaction headers must agree",
                Match(main, sub.Replace("No -10", "No 0")) == null
                && Match(main, sub.Replace("accept", "wrong")) == null
                && Match(main, sub.Replace("$q -1", "$q Other")) == null),
            ("Shared remembered answer IDs still pair through distinct reaction keys",
                Match(main.Replace("$r No", "$r Yes"), sub.Replace("$r No", "$r Yes")) != null),
            ("Question weekly variants must have matching structure",
                Match(main + "||" + main, sub) == null
                && DialogueQuestionTranslation.Match(main + "||" + main.Replace("Hello @!", "Goodbye @!"),
                    sub + "||" + sub.Replace("Chào @!", "Tạm biệt @!"), "Goodbye Alex!", answers,
                    "Alex", Gender.Female, 7)?.Prompt.Secondary == "Tạm biệt Alex!"),
            ("Question actions, extra pages and duplicate reaction keys fall back",
                Match(main, sub.Replace("Vâng ạ.", "%item object 74 1 %%")) == null
                && Match(main, sub + "#$b#More.") == null
                && Match(main.Replace("refuse", "accept"), sub.Replace("refuse", "accept")) == null),
            ("A wrapped question uses one height for both lines and flips their offsets",
                Fit(300) is { } above && Fit(300, false) is { } below
                && above["Yes"].Pair.Secondary.Contains('\n')
                && above["Yes"].Height == below["Yes"].Height
                && above["Yes"].Pair.PrimaryOffset > above["Yes"].Pair.SecondaryOffset
                && below["Yes"].Pair.PrimaryOffset < below["Yes"].Pair.SecondaryOffset),
            ("Identical translations occupy a single row without duplicate text",
                DialogueQuestionLayout.Fit(new[] { new DialogueTextPair("Same", "Same"),
                    new DialogueTextPair("Other", "Other") }, 200, 100, true, _ => 20, text => text.Length * 5, _ => 12)
                    is { } single && single["Same"].Height == 20 && single["Same"].Pair.Secondary == ""),
            ("Oversized questions and conflicting identical labels fall back as a whole",
                Fit(100) == null && DialogueQuestionLayout.Fit(new[] {
                    new DialogueTextPair("Same", "First"), new DialogueTextPair("Same", "Other") },
                    200, 200, true, _ => 20, text => text.Length * 5, _ => 12) == null)
        };
    }

    private static (string, bool)[] CheckPairs()
    {
        const string english = "Oh my! What a lovely day.";
        const string vietnamese = "Ôi chao! Một ngày đẹp trời.";
        string[][] variants =
        {
            new[] { "Oh my!", "What a lovely day." },
            new[] { "Ôi chao!", "Một ngày đẹp trời." }
        };
        var forward = DialogueTranslation.MatchBlocks(english, vietnamese, variants, "Alex");
        var reverse = DialogueTranslation.MatchBlocks(vietnamese, english, variants, "Alex");
        return new[]
        {
            ("Exact authored blocks pair in either language order",
                forward is { Length: 2 } && reverse is { Length: 2 }
                && forward[0] == new DialogueTextPair("Oh my!", "Ôi chao!")
                && reverse[1] == new DialogueTextPair("Một ngày đẹp trời.", "What a lovely day.")),
            ("Authored page and player-name placeholder stay aligned",
                Resolve("Hello.#$b#Goodbye, @.", "Chào.#$b#Tạm biệt, @.", "Goodbye, Alex.", page: 1) == "Tạm biệt, Alex.")
        };
    }

    private static (string, bool)[] CheckFallback()
        => new[]
        {
            ("Mismatched live text or page structure suppresses the secondary",
                Resolve("Hello.", "Chào.", "Changed.") == null
                && Resolve("Hello.#$b#Goodbye.", "Chào.", "Hello.") == null),
            ("Unsafe dialogue commands stay outside the preview and translation resolver",
                !DialogueTranslation.IsSafePreview("Hello.||$action AddMoney 1")
                && Resolve("Hello.", "Chào [74].", "Hello.") == null),
            ("Blank secondary page does not shift the next translation",
                Resolve("Hello.#$b#Goodbye.", "${^}$#$b#Tạm biệt.", "Hello.") == null
                && Resolve("Hello.#$b#Goodbye.", "${^}$#$b#Tạm biệt.", "Goodbye.", page: 1) == "Tạm biệt."),
            ("An overflowing pair returns no layout", FitSample(20, true) == null)
        };

    private static (string, bool)[] CheckLayout()
    {
        var above = FitSample(54, true);
        var below = FitSample(54, false);
        return new[]
        {
            ("Wrapped secondary stays beside its primary and flips as a block",
                above is { Length: 1 } && below is { Length: 1 }
                && above[0].Secondary == "short\nsentence"
                && above[0].PrimaryOffset == 24 && above[0].SecondaryOffset == 0
                && below[0].PrimaryOffset == 0 && below[0].SecondaryOffset == 34),
            ("The last translation reveals at the vanilla advance threshold",
                DialoguePairLayout.GetVisiblePosition(8, 10) == 8
                && DialoguePairLayout.GetVisiblePosition(9, 10) == 10)
        };
    }

    private static DialoguePairLayout.PositionedPair[]? FitSample(int height, bool secondaryFirst)
        => DialoguePairLayout.FitPage(new[] { new DialogueTextPair("Primary", "short sentence") }, 8, height,
            secondaryFirst, _ => 30, line => line.Length, text => text.Split('\n').Length * 10);
}
