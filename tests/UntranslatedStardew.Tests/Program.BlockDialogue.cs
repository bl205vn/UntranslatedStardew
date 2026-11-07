using StardewValley.BellsAndWhistles;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static void TestBlockDialogue()
    {
        var oldLanguage = StardewValley.LocalizedContentManager.CurrentLanguageCode;
        var zoomField = HarmonyLib.AccessTools.Field(typeof(SpriteText), "fontPixelZoom");
        float oldZoom = (float)zoomField.GetValue(null)!;
        bool oldForce = SpriteText.forceEnglishFont;
        try
        {
            StardewValley.LocalizedContentManager.CurrentLanguageCode = StardewValley.LocalizedContentManager.LanguageCode.en;
            zoomField.SetValue(null, 3f);
            SpriteText.forceEnglishFont = false;
            const string gunther = "The museum looks amazing! Thanks again for all your help.";
            Assert(SpriteText.getHeightOfString(gunther, 712) == 156
                && PrimaryDialogueMetrics.Measure(gunther, 716) == 102,
                "Gunther boundary: a two-line native draw no longer reserves three lines at width 716");
            Assert(PrimaryDialogueMetrics.Measure("First^Second", 1000) == 102
                && PrimaryDialogueMetrics.Measure("", 1000) == 0,
                "Primary measurement preserves explicit native line breaks and empty text");
        }
        finally
        {
            StardewValley.LocalizedContentManager.CurrentLanguageCode = oldLanguage;
            zoomField.SetValue(null, oldZoom);
            SpriteText.forceEnglishFont = oldForce;
        }
        int MainHeight(string text) => Math.Max(1, (int)Math.Ceiling(text.Length / 10d)) * 20;
        int SubHeight(string text) => text.Split('\n').Length * 10;
        DialogueBlockLayout.Page? Fit(string main, string sub, bool above = false, int height = 400)
            => DialogueBlockLayout.Fit(new[] { new DialogueTextPair(main, sub) }, 10, height, above,
                MainHeight, text => text.Length, text => SubHeight(text));
        const string main = "One two three four five six";
        const string sub = "Một hai ba bốn năm sáu";
        var below = Fit(main, sub)!;
        var above = Fit(main, sub, true)!;
        var block = below.Rows.Single();
        Assert(block.SecondaryOffset >= block.PrimaryOffset + MainHeight(main),
            "Long primary text finishes its entire block before the secondary block starts");
        Assert(above.Height == below.Height && above.Rows.Single().PrimaryOffset
            >= above.Rows.Single().SecondaryOffset + SubHeight(above.Rows.Single().Secondary),
            "SecondaryFirst swaps whole blocks without changing combined height");
        Assert(block.Primary == main && block.Start == 0 && block.RevealAt == main.Length,
            "Block layout preserves the exact full primary source and reveal threshold");
        Assert(block.RevealedCharacters(main.Length - 2) < block.RevealAt
            && block.RevealedCharacters(DialoguePairLayout.GetVisiblePosition(main.Length - 1, main.Length)) == block.RevealAt,
            "Secondary remains hidden until all primary text is visible including vanilla's final-character threshold");
        var extra = Fit("Hi.", "One two three four five six")!.Rows.Single();
        Assert(extra.Primary == "Hi." && extra.Secondary.Contains('\n') && extra.RevealAt == 3,
            "A longer secondary stays one wrapped block and waits for the primary to complete");
        const string unspaced = "123456789012345678901";
        Assert(Fit(unspaced, "abcdefghijklmno")!.Rows.Single().Primary == unspaced,
            "Primary long-word wrapping remains vanilla's responsibility without losing characters");
        var explicitBreak = Fit("First^Second", "Đầu\nSau")!.Rows.Single();
        Assert(explicitBreak.Primary == "First^Second" && explicitBreak.Secondary == "Đầu\nSau"
            && explicitBreak.RevealAt == "First^Second".Length,
            "Explicit primary and secondary breaks stay inside their respective full blocks");
        Assert(Fit(main, sub, height: 40) == null, "An overflowing bilingual block falls back before partial rendering");
        Assert(Fit(main, main)!.Rows.Single().Secondary == "", "Identical language text occupies only the primary block");
        var pairs = DialogueBlockLayout.Fit(new[] { new DialogueTextPair("Hello.", "Chào."),
            new DialogueTextPair(main, sub) }, 10, 500, false, MainHeight, text => text.Length, text => SubHeight(text))!.Rows.Single();
        Assert(pairs.Primary == "Hello. " + main && pairs.RevealAt == pairs.Primary.Length
            && pairs.SecondaryOffset >= pairs.PrimaryOffset + MainHeight(pairs.Primary),
            "Authored pairs combine into all primary text followed by all secondary text on the same page");
        var question = DialogueQuestionLayout.FitRows(new[] { new DialogueTextPair(main, sub), new DialogueTextPair("Yes.", "Vâng.") },
            10, 500, false, MainHeight, text => text.Length, text => SubHeight(text), out string? failure);
        Assert(failure == null && question![main].Rows.Length == 1 && question[main].Height == below.Height,
            "Question draw and hitboxes share the same whole-block height");
        Assert(DialogueQuestionLayout.FitRows(new[] { new DialogueTextPair("Same", "First"), new DialogueTextPair("Same", "Other") },
            10, 500, false, MainHeight, text => text.Length, text => SubHeight(text), out _) == null,
            "Question block layout rejects conflicting identical labels");
    }
}
