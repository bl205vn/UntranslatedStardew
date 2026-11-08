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
        Assert(EventDialogueTranslation.TryNormalizeAsset("Data/Festivals/spring13", out var festAsset, allowFestivals: true)
            && festAsset == "Data/Festivals/spring13"
            && !EventDialogueTranslation.TryNormalizeAsset("Data/Festivals/../spring13", out _, allowFestivals: true),
            "Festival source paths validate properly when festival support is scoped and enabled");
        Assert(EventDialogueTranslation.FindKey(new Dictionary<string, string> { ["set-up"] = main }, "festival_fall16", live, Gender.Female, isFestival: true) == "set-up",
            "Festival key selection matches unique live commands under festival context");

        // Event and Festival question / quickQuestion command adapters
        const string qMain = "pause 100/question fork \"Go left or right?#Left#Right\"/end";
        const string qSub = "pause 100/question fork \"Đi trái hay phải?#Trái#Phải\"/end";
        var qLive = Commands(qMain);
        var qMatch = EventDialogueTranslation.MatchQuestion(qMain, qSub, qLive, 1, ArgUtility.SplitBySpaceQuoteAware(qLive[1]), Gender.Female);
        Assert(qMatch != null && qMatch.Prompt.Primary == "Go left or right?" && qMatch.Prompt.Secondary == "Đi trái hay phải?"
            && qMatch.Answers.Length == 2 && qMatch.Answers[0].Secondary == "Trái" && qMatch.Answers[1].Secondary == "Phải",
            "Event question pairs prompt and answer labels with matching command provenance");
        Assert(EventDialogueTranslation.MatchQuestion(qMain, qSub.Replace("Trái#Phải", "Trái"), qLive, 1, ArgUtility.SplitBySpaceQuoteAware(qLive[1]), Gender.Female) == null,
            "Event question rejects mismatched answer count");
        Assert(EventDialogueTranslation.MatchQuestion(qMain, qSub.Replace("fork", "spoon"), qLive, 1, ArgUtility.SplitBySpaceQuoteAware(qLive[1]), Gender.Female) == null,
            "Event question rejects changed dialogue key");

        const string qqMain = "quickQuestion Which?#A#B(break)end(break)end";
        const string qqSub = "quickQuestion Chọn?#A-vi#B-vi(break)end(break)end";
        var qqLive = new[] { qqMain };
        var qqMatch = EventDialogueTranslation.MatchQuestion(qqMain, qqSub, qqLive, 0, ArgUtility.SplitBySpaceQuoteAware(qqMain), Gender.Female);
        Assert(qqMatch != null && qqMatch.Prompt.Secondary == "Chọn?" && qqMatch.Answers[0].Secondary == "A-vi",
            "Event quickQuestion pairs prompt and answers while verifying break logic");
        Assert(EventDialogueTranslation.MatchQuestion(qqMain, qqSub.Replace("end(break)end", "end(break)pause 100"), qqLive, 0, ArgUtility.SplitBySpaceQuoteAware(qqMain), Gender.Female) == null,
            "Event quickQuestion rejects changed branch logic");

        // Farm sewing event 992559 divergent story (26 commands vs 19 commands) intentionally remains fallback
        var en26 = string.Join("/", Enumerable.Range(0, 26).Select(i => $"command{i}"));
        var vi19 = string.Join("/", Enumerable.Range(0, 19).Select(i => $"command{i}"));
        Assert(EventDialogueTranslation.Match(en26, vi19, Commands(en26), 0, new[] { "command0" }, Gender.Female) == null,
            "Divergent story scripts with differing command lengths strictly fall back without fake pairing");
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
            foreach (var adapter in new[] { typeof(EventScriptSourcePatch), typeof(FestivalDialogueSourcePatch),
                typeof(EventDialoguePatch.FestivalNpcDialoguePatch), typeof(GiftAdviceCapturePatch),
                typeof(GiftAdviceFormattedStringPatch), typeof(GiftAdviceGenderStringPatch) })
            {
                var patched = harmony.CreateClassProcessor(adapter).Patch();
                Assert(patched is { Count: > 0 }, $"{adapter.Name} installs on the real game assembly");
            }
            foreach (string name in new[] { nameof(Event.DefaultCommands.Speak), nameof(Event.DefaultCommands.Message), nameof(Event.DefaultCommands.Question), nameof(Event.DefaultCommands.QuickQuestion) })
            {
                var info = Harmony.GetPatchInfo(AccessTools.Method(typeof(Event.DefaultCommands), name));
                Assert(info?.Prefixes.Any(p => p.owner == harmony.Id) == true && info.Postfixes.Any(p => p.owner == harmony.Id),
                    "Event " + name + " capture installs on the real vanilla command boundary");
            }
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }
}
