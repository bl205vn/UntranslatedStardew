using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static void TestObjectDialogue()
    {
        var assembly = typeof(RawContentProvider).Assembly;
        var tracker = assembly.GetType("UntranslatedStardew.Core.LoadedDialogueSource", true)!;
        var capture = AccessTools.Method(tracker, "Capture");
        var get = AccessTools.Method(tracker, "Get");
        var reset = AccessTools.Method(tracker, "Invalidate");
        string first = new string("A message.".ToCharArray());
        string equal = new string("A message.".ToCharArray());
        reset.Invoke(null, null);
        capture.Invoke(null, new object[] { first, "Strings/Characters:One" });
        Assert((string?)get.Invoke(null, new object[] { first }) == "Strings/Characters:One"
            && get.Invoke(null, new object[] { equal }) == null,
            "Object dialogue provenance follows string identity, never a same-text lookup");
        capture.Invoke(null, new object[] { first, "Strings/Characters:Two" });
        capture.Invoke(null, new object[] { first, "Strings/Characters:One" });
        Assert(get.Invoke(null, new object[] { first }) == null, "Ambiguous shared string sources stay suppressed");
        capture.Invoke(null, new object[] { equal, "Strings/Characters:One" });
        capture.Invoke(null, new object[] { equal, "Data/Events/Town:1" });
        Assert(get.Invoke(null, new object[] { equal }) == null, "A shared unsupported source cannot inherit a previous supported tag");
        reset.Invoke(null, null);
        Assert(get.Invoke(null, new object[] { first }) == null, "Locale/content invalidation clears captured string provenance");

        var type = assembly.GetType("UntranslatedStardew.Core.ObjectDialogueTranslation", true)!;
        var match = AccessTools.Method(type, "Match");
        Array? Match(string main, string sub, string displayed, Gender gender = Gender.Male)
            => (Array?)match.Invoke(null, new object[] { main, sub, displayed, gender });
        var pages = Match("Woman: Hello.^Goodbye.#Next page.", "Người phụ nữ: Chào.^Tạm biệt.#Trang sau.", "Woman: Hello.^Goodbye.#Next page.");
        var pairType = assembly.GetType("UntranslatedStardew.Core.DialogueTextPair", true)!;
        var primary = pairType.GetProperty("Primary")!;
        var secondary = pairType.GetProperty("Secondary")!;
        Assert(pages?.Length == 2 && (string?)primary.GetValue(pages.GetValue(0)) == "Woman: Hello.^Goodbye."
            && (string?)secondary.GetValue(pages.GetValue(0)) == "Người phụ nữ: Chào.\nTạm biệt.",
            "Object pages preserve speaker labels and treat caret as a display newline");
        Assert(Match("Hello ${sir^madam}$.", "Chào ${anh^chị}$.", "Hello madam.", Gender.Female) is { Length: 1 },
            "Object messages mirror LoadString gender preprocessing");
        Assert(Match("Hello.#Next.", "Chào.", "Hello.#Next.") == null
            && Match("Hello.", "%item object 74 1 %%", "Hello.") == null
            && Match("Hello.", "Give [74].", "Hello.") == null
            && Match("Hello {0}.", "Chào {0}.", "Hello {0}.") == null
            && Match("Hello.", "Chào.", "Modified Hello.") == null,
            "Object messages reject shifted pages, commands, gifts, templates and changed primary text");
        var page = AccessTools.Method(type, "CurrentPage");
        Assert(page.Invoke(null, new object[] { pages!, new[] { "Next page." } }) is 1
            && page.Invoke(null, new object[] { pages!, new[] { "Woman: Hello.^Goodbye.", "Next page." } }) is 0
            && page.Invoke(null, new object[] { pages!, new[] { "Changed page." } }) is -1,
            "Page selection uses remaining source pages, so reveal-only clicks cannot advance translation");
        var blank = Match("One.##Three.", "Một.##Ba.", "One.##Three.");
        Assert(blank is { Length: 3 } && (string?)secondary.GetValue(blank.GetValue(1)) == "",
            "Blank object pages keep their authored slots");

        var harmony = new Harmony("UntranslatedStardew.Tests.ObjectDialogue");
        var ctor = AccessTools.Constructor(typeof(DialogueBox), new[] { typeof(string) });
        var load = AccessTools.Method(typeof(LocalizedContentManager), nameof(LocalizedContentManager.LoadStringReturnNullIfNotFound), new[] { typeof(string), typeof(bool) });
        try
        {
            harmony.CreateClassProcessor(assembly.GetType("UntranslatedStardew.Patches.ObjectDialoguePatch", true)!).Patch();
            harmony.CreateClassProcessor(assembly.GetType("UntranslatedStardew.Patches.LoadedDialogueSourcePatch", true)!).Patch();
            Assert(Harmony.GetPatchInfo(ctor)?.Postfixes.Any(p => p.owner == harmony.Id) == true
                && Harmony.GetPatchInfo(load)?.Postfixes.Any(p => p.owner == harmony.Id) == true,
                "Object message constructor and exact LoadString boundary install on actual 1.6.15 methods");
        }
        finally
        {
            harmony.Unpatch(ctor, HarmonyPatchType.All, harmony.Id);
            harmony.Unpatch(load, HarmonyPatchType.All, harmony.Id);
            harmony.Unpatch(AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.receiveLeftClick)), HarmonyPatchType.All, harmony.Id);
            harmony.Unpatch(AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.update)), HarmonyPatchType.All, harmony.Id);
            harmony.Unpatch(AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.draw), new[] { typeof(SpriteBatch) }), HarmonyPatchType.All, harmony.Id);
            reset.Invoke(null, null);
        }
    }
}
