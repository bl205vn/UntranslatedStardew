using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>Deterministic smoke checks for the production dialogue resolver and layout.</summary>
internal static class DialogueChecks
{
    internal static (string Name, bool Passed)[]? Run(string group)
        => group switch
        {
            "branch" => CheckBranches(),
            "pairs" => CheckPairs(),
            "fallback" => CheckFallback(),
            "layout" => CheckLayout(),
            "all" => CheckBranches().Concat(CheckPairs()).Concat(CheckFallback()).Concat(CheckLayout()).ToArray(),
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
