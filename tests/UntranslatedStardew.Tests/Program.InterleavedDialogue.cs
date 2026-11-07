using UntranslatedStardew.Core;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static void TestInterleavedDialogue()
    {
        int MainHeight(string text) => Math.Max(1, (int)Math.Ceiling(text.Length / 10d)) * 20;
        DialogueInterleavedLayout.Page? Fit(string main, string sub, bool above = false, int height = 400)
            => DialogueInterleavedLayout.Fit(new[] { new DialogueTextPair(main, sub) }, 10, height, above,
                MainHeight, text => text.Length, _ => 10);
        const string main = "One two three four five six";
        const string sub = "Một hai ba bốn năm sáu";
        var below = Fit(main, sub)!;
        var above = Fit(main, sub, true)!;
        Assert(below.Rows.Length == 3 && below.Rows[0].Secondary == "Một hai ba"
            && below.Rows[0].SecondaryOffset < below.Rows[1].PrimaryOffset,
            "Wrapped dialogue interleaves a secondary row before the next primary row");
        Assert(above.Height == below.Height && above.Rows.Zip(below.Rows).All(pair =>
            pair.First.SecondaryOffset < pair.First.PrimaryOffset && pair.Second.PrimaryOffset < pair.Second.SecondaryOffset),
            "SecondaryFirst flips every visual row while preserving combined page height");
        Assert(below.Rows.All(row => main.Substring(row.Start, row.Primary.Length) == row.Primary)
            && below.Rows[0].RevealAt < main.Length && below.Rows[1].Start > below.Rows[0].RevealAt,
            "Interleaved primary rows retain exact source offsets across skipped wrapping spaces");
        int firstEnd = below.Rows[0].RevealAt;
        Assert(below.Rows[0].RevealedCharacters(firstEnd) == below.Rows[0].Primary.Length
            && firstEnd < below.Rows[1].RevealAt && firstEnd < main.Length,
            "The first secondary row can reveal before the entire primary block completes");
        var extra = Fit("Hi.", "One two three four five six")!;
        Assert(extra.Rows.Length > 1 && extra.Rows[1].Primary == "" && extra.Rows[1].RevealAt == 3,
            "Extra secondary rows wait for the block end without shifting source offsets");
        var unspaced = Fit("123456789012345678901", "abcdefghijklmno")!;
        Assert(unspaced.Rows.Length == 3 && string.Concat(unspaced.Rows.Select(row => row.Primary)) == "123456789012345678901",
            "Long unspaced primary lines break without losing characters");
        var explicitBreak = Fit("First^Second", "Đầu\nSau")!;
        Assert(explicitBreak.Rows.Length == 2 && explicitBreak.Rows[1].Start == 6
            && explicitBreak.Rows[1].Secondary == "Sau", "Explicit line breaks preserve source positions and row order");
        Assert(Fit(main, sub, height: 40) == null, "An overflowing interleaved page falls back before partial rendering");
        var same = Fit("One two three four", "One two three four")!;
        Assert(same.Rows.All(row => row.Secondary == ""), "Identical language text is not duplicated in interleaved layout");
        var pairs = DialogueInterleavedLayout.Fit(new[] { new DialogueTextPair("Hello.", "Chào."),
            new DialogueTextPair(main, sub) }, 10, 500, false, MainHeight, text => text.Length, _ => 10)!;
        Assert(pairs.Rows[1].Start == "Hello. ".Length && pairs.Rows[1].Secondary.Length > 0,
            "Authored semantic pairs remain boundaries for visual interleaving");
        var question = DialogueQuestionLayout.FitRows(new[] { new DialogueTextPair(main, sub), new DialogueTextPair("Yes.", "Vâng.") },
            10, 500, false, MainHeight, text => text.Length, _ => 10, out string? failure);
        Assert(failure == null && question![main].Rows.Length == 3 && question[main].Height == below.Height,
            "Question draw and hitbox rows use the same interleaved page height");
        Assert(DialogueQuestionLayout.FitRows(new[] { new DialogueTextPair("Same", "First"), new DialogueTextPair("Same", "Other") },
            10, 500, false, MainHeight, text => text.Length, _ => 10, out _) == null,
            "Question interleaving rejects conflicting identical labels");
    }
}
