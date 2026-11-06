using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
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

        string? Pair(string primary, string secondary, int index, string visible)
            => (string?)pair.Invoke(null, new object[] { primary, secondary, index, visible, "Alex" });

        Assert(Pair("Hello, @$h#$b#Goodbye.", "Chào @.$1#$b#Tạm biệt.", 0, "Hello, Alex") == "Chào Alex.",
            "Dialogue pairs a player-name token and portrait suffix without executing commands");
        Assert(Pair("Hello.#$b#Goodbye.", "Xin chào.#$b#Tạm biệt.", 1, "Goodbye.") == "Tạm biệt.",
            "Dialogue uses the logical segment index");
        Assert(Pair("Hello.#$b#Goodbye.", "Xin chào.", 0, "Hello.") == null
            && Pair("Hello.", "Xin chào.", 0, "Different live text.") == null
            && Pair("Hello.", "Hello.", 0, "Hello.") == null,
            "Dialogue skips mismatched segments, changed live text and duplicates");
        foreach (string unsafeText in new[] { "$c 1#Hello.#Bye.", "Hello.^Bye.", "Hello.||Bye.", "$action AddMoney 1", "Hello. [74]", "Hello.\\nBye." })
            Assert(Pair("Hello.", unsafeText, 0, "Hello.") == null, "Dialogue skips unsupported syntax: " + unsafeText);

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

        var original = AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.draw), new[] { typeof(SpriteBatch) });
        var patch = mod.GetType("UntranslatedStardew.Patches.DialogueBoxPatch", true)!;
        var harmony = new Harmony("UntranslatedStardew.Tests.DialoguePrototype");
        try
        {
            harmony.CreateClassProcessor(patch).Patch();
            var wrapper = AccessTools.Method(patch, "DrawPortraitText");
            Assert(PatchProcessor.GetCurrentInstructions(original).Count(instruction => instruction.Calls(wrapper)) == 1,
                "Harmony installs the portrait dialogue draw wrapper in Stardew 1.6.15");
        }
        finally
        {
            harmony.Unpatch(original, HarmonyPatchType.All, harmony.Id);
        }
    }
}
