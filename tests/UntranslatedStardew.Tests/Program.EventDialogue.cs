using HarmonyLib;
using StardewValley;
using UntranslatedStardew.Core;
using UntranslatedStardew.Patches;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static void TestEventDialogue()
    {
        const string main = "music/1 2/farmer 1 2 0/speak Emily \"Hi @! A slash / inside text.$h\"/pause 500/message \"You can sew now!\"/end";
        const string sub = "music/1 2/farmer 1 2 0/speak Emily \"Chào @! Dấu / trong câu.$h\"/pause 500/message \"Đã mở may đồ!\"/end";
        string[] Commands(string raw) => ArgUtility.SplitQuoteAware(raw, '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries, keepQuotesAndEscapes: true);
        var live = Commands(main);
        EventDialogueTranslation.Text? Pair(string translated = sub, int index = 3, string[]? actual = null)
            => EventDialogueTranslation.Match(main, translated, actual ?? live, index,
                ArgUtility.SplitBySpaceQuoteAware((actual ?? live)[index]), Gender.Female);
        Assert(Pair() is { Actor: "Emily" } text && text.Primary.Contains("slash / inside")
            && text.Secondary.Contains("Dấu /"),
            "Event speak pairs quoted slash text by command index and speaker without splitting the dialogue");
        Assert(Pair(index: 5) is { Actor: null, Secondary: "Đã mở may đồ!" },
            "Event message pairs its own command without changing its index or action");
        Assert(Pair(sub.Replace("speak Emily", "speak Leah")) == null
            && Pair(sub.Replace("pause 500", "pause 100")) == null && Pair(sub + "/addMoney 1") == null,
            "Event pairing rejects changed actors, action arguments and extra commands");
        Assert(Pair(sub.Replace("Chào @!", "[Random text]")) == null
            && Pair(actual: live.Select((command, i) => i == 4 ? "pause 999" : command).ToArray()) == null,
            "Event pairing refuses unresolved tokens and runtime scripts changed by forks or mods");
        Assert(Pair(index: 4) == null && EventDialogueTranslation.FindKey(new Dictionary<string, string> { ["992559/a"] = main, ["992559/b"] = main }, "992559", live, Gender.Female) == null
            && EventDialogueTranslation.FindKey(new Dictionary<string, string> { ["992559/t 600 1130"] = main, ["77/a"] = main }, "992559", live, Gender.Female) == "992559/t 600 1130",
            "Event source selection refuses action commands and ambiguous ID preconditions");
        Assert(EventDialogueTranslation.TryNormalizeAsset(@"Data\Events\Farm", out var asset) && asset == "Data/Events/Farm"
            && !EventDialogueTranslation.TryNormalizeAsset("Data/Events/../Farm", out _)
            && !EventDialogueTranslation.TryNormalizeAsset("Data/Festivals/spring13", out _),
            "Event source paths are separate from dialogue, reject traversal and do not imply festival support");
        Assert(!EventDialoguePatch.IsEligible((StardewValley.Menus.DialogueBox)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(StardewValley.Menus.DialogueBox))),
            "An uncaptured event box stays vanilla");
        var oldGame = Game1.game1;
        var sources = AccessTools.Field(typeof(EventDialoguePatch), "Sources").GetValue(null)!;
        var box = (StardewValley.Menus.DialogueBox)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(StardewValley.Menus.DialogueBox));
        try
        {
            var game = (Game1)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Game1));
            var location = (GameLocation)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(GameLocation));
            var owner = (Event)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Event));
            GC.SuppressFinalize(owner); // A fixture must never invoke Event.notifyDone on collection.
            owner.CurrentCommand = 3;
            location.currentEvent = owner;
            AccessTools.Field(typeof(Game1), "instanceGameLocation").SetValue(game, location);
            Game1.game1 = game;
            var sourceType = typeof(EventDialoguePatch).GetNestedType("Source", System.Reflection.BindingFlags.NonPublic)!;
            object source = Activator.CreateInstance(sourceType, owner, 3, LoadedDialogueSource.Generation,
                new EventDialogueTranslation.Text("Hi", "Chào", "Emily"))!;
            sources.GetType().GetMethod("Add")!.Invoke(sources, new[] { box, source });
            Assert(EventDialoguePatch.IsEligible(box), "A captured event box is eligible only for its active command");
            owner.CurrentCommand = 4;
            Assert(!EventDialoguePatch.IsEligible(box), "Advancing the event invalidates an old box translation");
            owner.CurrentCommand = 3;
            owner.skipped = true;
            Assert(!EventDialoguePatch.IsEligible(box), "Skipping the event suppresses captured translations");
            owner.skipped = false;
            LoadedDialogueSource.Invalidate();
            Assert(!EventDialoguePatch.IsEligible(box), "Content reload invalidates captured event translations");
        }
        finally
        {
            sources.GetType().GetMethod("Remove")!.Invoke(sources, new object[] { box });
            Game1.game1 = oldGame;
        }
        var harmony = new Harmony("UntranslatedStardew.Tests.EventDialogue");
        try
        {
            harmony.CreateClassProcessor(typeof(EventDialoguePatch)).Patch();
            foreach (string name in new[] { nameof(Event.DefaultCommands.Speak), nameof(Event.DefaultCommands.Message) })
            {
                var info = Harmony.GetPatchInfo(AccessTools.Method(typeof(Event.DefaultCommands), name));
                Assert(info?.Prefixes.Any(p => p.owner == harmony.Id) == true && info.Postfixes.Any(p => p.owner == harmony.Id),
                    "Event " + name + " capture installs on the real vanilla command boundary");
            }
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }
}
