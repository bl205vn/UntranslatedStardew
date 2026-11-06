using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static void TestDialoguePrototype()
    {
        Assembly mod = typeof(RawContentProvider).Assembly;
        MethodInfo pair = mod.GetType("UntranslatedStardew.Core.DialogueTranslation", true)!
            .GetMethod("GetSecondaryLine", BindingFlags.Static | BindingFlags.NonPublic)!;

        string? Pair(string primary, string secondary, int index, string visible, Gender gender = Gender.Male, uint daysPlayed = 0)
            => (string?)pair.Invoke(null, new object[] { primary, secondary, index, visible, "Alex", gender, daysPlayed });

        Assert(Pair("Hello, @$h#$b#Goodbye.", "Chào @.$1#$b#Tạm biệt.", 0, "Hello, Alex") == "Chào Alex.",
            "Dialogue pairs a player-name token and portrait suffix without executing commands");
        Assert(Pair("Hello.#$b#Goodbye.", "Xin chào.#$b#Tạm biệt.", 1, "Goodbye.") == "Tạm biệt.",
            "Dialogue uses the logical segment index");
        Assert(Pair("Hello.#$b#Goodbye.", "Xin chào.", 0, "Hello.") == null
            && Pair("Hello.", "Xin chào.", 0, "Different live text.") == null
            && Pair("Hello.", "Hello.", 0, "Hello.") == null,
            "Dialogue skips mismatched segments, changed live text and duplicates");
        foreach (string unsafeText in new[] { "$c 1#Hello.#Bye.", "Hello.^Bye.^Extra.", "Hello.||Bye.", "$action AddMoney 1", "Hello. [74]", "Hello.\\nBye." })
            Assert(Pair("Hello.", unsafeText, 0, "Hello.") == null, "Dialogue skips unsupported syntax: " + unsafeText);

        Assert(Pair("Hello ${sir^madam^friend}$ @.", "Chào ${anh¦chị¦bạn}$ @.", 0,
                "Hello madam Alex.", Gender.Female) == "Chào chị Alex."
            && Pair("Hello ${sir^madam^friend}$ @.", "Chào ${anh¦chị¦bạn}$ @.", 0,
                "Hello friend Alex.", Gender.Undefined) == "Chào bạn Alex.",
            "Dialogue resolves the same player gender for both translation blocks");
        Assert(Pair("Hello, sir.$h^Hello, madam.$s", "Chào anh.¦Chào chị.", 0,
                "Hello, madam.", Gender.Female) == "Chào chị.",
            "Dialogue supports legacy gender delimiters and branch portrait suffixes");
        Assert(Pair("Hello.", "Chào ${anh^[74]}$.", 0, "Hello.") == null
            && Pair("Hello.", "Chào ${anh^chị.", 0, "Hello.") == null,
            "Unsafe or malformed gender blocks fall back even when the unsafe branch is inactive");

        Assert(Pair("${^}$Hello.", "${^}$Xin chào.", 0, "Hello.") == "Xin chào."
            && Pair("Hello${^ again}$.", "Chào${^ lần nữa}$.", 0, "Hello again.", Gender.Female) == "Chào lần nữa."
            && Pair("Hello${^ again}$.", "Chào${^ lần nữa}$.", 0, "Hello.") == "Chào.",
            "Empty gender alternatives disappear and optional text stays aligned");
        Assert(Pair("Hello.#$b#Goodbye.", "${^}$#$b#Tạm biệt.", 0, "Hello.") == null
            && Pair("Hello.#$b#Goodbye.", "${^}$#$b#Tạm biệt.", 1, "Goodbye.") == "Tạm biệt.",
            "A blank translation is suppressed without shifting or suppressing the next authored page");

        const string weeklyMain = "First week.||Second week.#$b#Another line.";
        const string weeklySecondary = "Tuần đầu.||Tuần hai.#$b#Dòng tiếp.";
        Assert(Pair(weeklyMain, weeklySecondary, 0, "First week.", daysPlayed: 6) == "Tuần đầu."
            && Pair(weeklyMain, weeklySecondary, 0, "Second week.", daysPlayed: 7) == "Tuần hai."
            && Pair(weeklyMain, weeklySecondary, 1, "Another line.", daysPlayed: 13) == "Dòng tiếp."
            && Pair(weeklyMain, weeklySecondary, 0, "First week.", daysPlayed: 14) == "Tuần đầu.",
            "Weekly dialogue matches vanilla week boundaries and selects authored pages within the active variant");
        Assert(Pair(weeklyMain, "Tuần đầu.", 0, "First week.") == null
            && Pair(weeklyMain, weeklySecondary, 0, "First week.", daysPlayed: 7) == null,
            "Weekly dialogue rejects unequal variant counts and mismatched live text");

        MethodInfo match = pair.DeclaringType!.GetMethod("MatchBlocks", BindingFlags.Static | BindingFlags.NonPublic)!;
        string[][] variants = { new[] { "Oh my!", "What a lovely day." }, new[] { "Ôi chao!", "Một ngày đẹp trời." } };
        Array? Match(string primary, string secondary, string[][] data)
            => (Array?)match.Invoke(null, new object[] { primary, secondary, data, "Alex" });
        var matched = Match("Ôi chao! Một ngày đẹp trời.", "Oh my! What a lovely day.", variants);
        object? firstPair = matched?.GetValue(0);
        Assert(matched?.Length == 2
            && firstPair?.GetType().GetProperty("Secondary")?.GetValue(firstPair) is "Oh my!"
            && Match("Oh my! What a lovely day.", "Ôi chao! Một ngày đẹp trời.", variants)?.Length == 2,
            "Authored blocks match the exact live translations in either direction");
        Assert(Match("Ôi chao! Một ngày đẹp trời.", "Oh my! A lovely day.", variants) == null
            && Match("Ôi chao! Một ngày đẹp trời.", "Oh my! What a lovely day.", variants.Append(variants[0]).ToArray()) == null
            && Match("Hello Alex! See you.", "Chào Alex! Hẹn gặp lại.",
                new[] { new[] { "Hello @!", "See you." }, new[] { "Chào @!", "Hẹn gặp", "lại." } }) == null,
            "Stale, ambiguous or uneven authored block data cannot mispair dialogue");
        Assert(Match("Hello Alex! See you.", "Chào Alex! Hẹn gặp lại.",
            new[] { new[] { "Hello @!", "See you." }, new[] { "Chào @!", "Hẹn gặp lại." } })?.Length == 2,
            "Authored blocks resolve player-name placeholders before matching");

        MethodInfo fit = mod.GetType("UntranslatedStardew.Core.DialoguePairLayout", true)!
            .GetMethod("Fit", BindingFlags.Static | BindingFlags.NonPublic)!;
        (string Secondary, int PrimaryOffset, int SecondaryOffset, int Height)? Fit(int height, bool secondaryFirst)
            => ((string, int, int, int)?)fit.Invoke(null, new object[] {
                "short sentence", 8, height, 30, secondaryFirst,
                (Func<string, float>)(line => line.Length),
                (Func<string, float>)(text => text.Split('\n').Length * 10)
            });

        Assert(Fit(54, true) == ("short\nsentence", 24, 0, 54),
            "Wrapped secondary keeps both lines immediately above its primary segment");
        Assert(Fit(54, false) == ("short\nsentence", 0, 34, 54),
            "SecondaryFirst flips the whole wrapped block");
        Assert(Fit(53, true) == null,
            "An over-height pair falls back instead of overlapping the box");

        Type layoutType = fit.DeclaringType!;
        Type pairType = mod.GetType("UntranslatedStardew.Core.DialogueTextPair", true)!;
        var pairs = Array.CreateInstance(pairType, 2);
        pairs.SetValue(Activator.CreateInstance(pairType, "First", "short"), 0);
        pairs.SetValue(Activator.CreateInstance(pairType, "Second", "short sentence"), 1);
        var fitPage = layoutType.GetMethod("FitPage", BindingFlags.Static | BindingFlags.NonPublic)!;
        Array? Page(int height, bool secondaryFirst)
            => (Array?)fitPage.Invoke(null, new object[] {
                pairs, 8, height, secondaryFirst,
                (Func<string, int>)(text => text == "First" ? 20 : 30),
                (Func<string, float>)(line => line.Length),
                (Func<string, float>)(text => text.Split('\n').Length * 10)
            });
        var second = Page(92, true)?.GetValue(1);
        Type? positioned = second?.GetType();
        Assert(positioned?.GetProperty("SecondaryOffset")?.GetValue(second) is 38
            && positioned.GetProperty("PrimaryOffset")?.GetValue(second) is 62,
            "Second pair follows the first with its wrapped translation directly above");
        Assert(Page(91, true) == null, "Multi-pair page falls back as a whole when it overflows");
        var reveal = positioned?.GetMethod("RevealedCharacters", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert(reveal?.Invoke(second, new object[] { 6 }) is 0
            && reveal.Invoke(second, new object[] { 7 }) is 1
            && reveal.Invoke(second, new object[] { 12 }) is 6,
            "Second pair reveal excludes the separating space and first pair text");

        var safePreview = pair.DeclaringType!.GetMethod("IsSafePreview", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert(safePreview.Invoke(null, new object[] { "Hello.$h#$b#Goodbye." }) is true
            && safePreview.Invoke(null, new object[] { "Hello.#$b#$action AddMoney 1" }) is false
            && safePreview.Invoke(null, new object[] { "Hello.#$b#Here is [74]." }) is false
            && safePreview.Invoke(null, new object[] { "Hello.||$action AddMoney 1" }) is false,
            "Console preview validates all authored pages before constructing dialogue");

        var visiblePosition = layoutType.GetMethod("GetVisiblePosition", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert(visiblePosition.Invoke(null, new object[] { 8, 10 }) is 8
            && visiblePosition.Invoke(null, new object[] { 9, 10 }) is 10,
            "Final translation is revealed at vanilla's click-to-advance threshold");

        var prepare = layoutType.GetMethod("PreparePairs", BindingFlags.Static | BindingFlags.NonPublic)!;
        var prefixed = (Array)prepare.Invoke(null, new object[] { pairs, "Evelyn: " })!;
        var primaryProperty = pairType.GetProperty("Primary")!;
        Assert(primaryProperty.GetValue(prefixed.GetValue(0)) is "Evelyn: First"
            && primaryProperty.GetValue(prefixed.GetValue(1)) is "Second"
            && primaryProperty.GetValue(pairs.GetValue(0)) is "First",
            "Portraitless dialogue adds the vanilla speaker prefix once without changing the aligned data");

        var runChecks = mod.GetType("UntranslatedStardew.Core.DialogueChecks", true)!
            .GetMethod("Run", BindingFlags.Static | BindingFlags.NonPublic)!;
        (string Name, bool Passed)[]? Run(string group)
            => ((string, bool)[]?)runChecks.Invoke(null, new object[] { group });
        var groups = new[] { "branch", "pairs", "fallback", "layout" }.Select(group => Run(group)!).ToArray();
        Assert(groups.All(group => group.Length > 0 && group.All(check => check.Passed))
            && Run("all")!.SequenceEqual(groups.SelectMany(group => group)) && Run("invalid") == null,
            "Console dialogue checks run individually and aggregate the same passing results");

        var original = AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.draw), new[] { typeof(SpriteBatch) });
        var patch = mod.GetType("UntranslatedStardew.Patches.DialogueBoxPatch", true)!;
        var harmony = new Harmony("UntranslatedStardew.Tests.DialoguePrototype");
        try
        {
            harmony.CreateClassProcessor(patch).Patch();
            var wrapper = AccessTools.Method(patch, "DrawDialogueText");
            Assert(PatchProcessor.GetCurrentInstructions(original).Count(instruction => instruction.Calls(wrapper)) == 2,
                "Harmony installs both NPC body draw wrappers in Stardew 1.6.15");
        }
        finally
        {
            harmony.Unpatch(original, HarmonyPatchType.All, harmony.Id);
        }
    }
}
