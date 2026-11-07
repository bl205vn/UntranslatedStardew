using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.BellsAndWhistles;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static void TestDialoguePrototype()
    {
        TestDialogueNarration();
        TestDialogueProgress();
        TestObjectDialogue();
        Assembly mod = typeof(RawContentProvider).Assembly;
        var previewBuilder = mod.GetType("UntranslatedStardew.Core.DialogueQuestionPreview", true)!
            .GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!;
        (string Primary, string Secondary)? Preview(string main, string sub, int repeats = 3)
            => ((string, string)?)previewBuilder.Invoke(null, new object[] { main, sub, repeats });
        var viEn = Preview("Ôi chao! Một ngày đẹp trời.", "Oh my! What a lovely day.");
        var enVi = Preview("Oh my! What a lovely day.", "Ôi chao! Một ngày đẹp trời.");
        Assert(viEn != null && enVi != null && viEn.Value.Primary == enVi.Value.Secondary
            && viEn.Value.Secondary == enVi.Value.Primary,
            "Question sample follows both configured language directions without hardcoded secondary text");
        Assert(Preview("Hello.#$action AddMoney 1", "Chào.") == null
            && Preview("Hello.", "Chào.", 0) == null,
            "Question preview builder rejects injected commands and invalid repetition counts");
        string game = LocateGamePath()!;
        var englishFont = XnbFontMetadataParser.ParseFile(Path.Combine(game, "Content", "Fonts", "SpriteFont1.xnb"))
            .CreateHeadlessSpriteFont();
        var displayLabels = viEn!.Value.Secondary.Split('#').Where((_, i) => i % 2 == 1).ToArray();
        Assert(displayLabels.SelectMany(label => label).All(englishFont.Characters.Contains),
            "Configured English question labels fit the installed unsuffixed secondary font glyph set");
        Assert(!"Bạn sẽ chọn câu trả lời nào?".All(englishFont.Characters.Contains),
            "Installed English font reproduces the old hardcoded Vietnamese sample rejection");
        MethodInfo pair = mod.GetType("UntranslatedStardew.Core.DialogueTranslation", true)!
            .GetMethod("GetSecondaryLine", BindingFlags.Static | BindingFlags.NonPublic)!;

        string? Pair(string primary, string secondary, int index, string visible, Gender gender = Gender.Male, uint daysPlayed = 0, Func<string, bool>? answered = null)
            => (string?)pair.Invoke(null, new object?[] { primary, secondary, index, visible, "Alex", gender, daysPlayed, answered });

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

        Assert(Pair("$d cc#Open.|Closed.", "$d communitycenter#Mở cửa.|Đóng cửa.", 0, "Open.") == "Mở cửa."
            && Pair("$d kent#Home.#$b#Welcome.|Away.", "$d kent#Ở nhà.#$b#Chào mừng.|Đi vắng.", 1, "Welcome.") == "Chào mừng."
            && Pair("First.||$d kent#Home.|Away.", "Đầu.||$d kent#Ở nhà.|Đi vắng.", 0, "Away.", daysPlayed: 7) == "Đi vắng.",
            "World-state branches combine with aliases, authored pages and weekly variants");
        Assert(Pair("$d kent#Hello.|Hello.", "$d kent#Chào.|Chào.", 0, "Hello.") == "Chào."
            && Pair("$d kent#Hello.|Hello.", "$d kent#Chào.|Xin chào.", 0, "Hello.") == null
            && Pair("$d kent#Hello.|Hello.", "$d kent#${^}$|Xin chào.", 0, "Hello.") == null,
            "World-state ambiguity includes blank translations and permits only identical results");

        Assert(Pair("$query YEAR 2#Home.#$b#Welcome.|Away.", "$query YEAR 2#Ở nhà.#$b#Chào mừng.|Đi vắng.", 1,
                "Welcome.") == "Chào mừng."
            && Pair("First.||$query TRUE#Home.|Away.", "Đầu.||$query TRUE#Ở nhà.|Đi vắng.", 0,
                "Home.", daysPlayed: 7) == "Ở nhà."
            && Pair("$query TRUE#Hello.", "$query TRUE#Chào.|Chào.", 0, "Hello.") == "Chào.",
            "Queries combine with authored pages, weekly variants and implicit false branches");
        Assert(Pair("$query TRUE#Hello.|Hello.", "$query TRUE#${^}$|Chào.", 0, "Hello.") == null
            && Pair("$query TRUE#Hello.|Goodbye.", "$query TRUE#Chào.|$action AddMoney 1", 0, "Hello.") == null
            && Pair("$d kent#Hello.|Goodbye.", "$query TRUE#Chào.|Tạm biệt.", 0, "Hello.") == null,
            "Query ambiguity, unsafe inactive branches and different condition types fall back");

        Assert(Pair("$p AnswerA AnswerB#Hello, @.$h|Goodbye.", "$p AnswerA AnswerB#Chào @.$1|Tạm biệt.", 0,
                "Hello, Alex.") == "Chào Alex."
            && Pair("First.||$p AnswerA#Hello.|Goodbye.", "Đầu.||$p AnswerA#Chào.|Tạm biệt.", 0,
                "Goodbye.", daysPlayed: 7) == "Tạm biệt.",
            "Response prerequisites preserve IDs, player names, portraits and weekly selection");
        Assert(Pair("$p AnswerA#Hello.|Goodbye.", "$p AnswerA#${^}$|Tạm biệt.", 0, "Hello.") == null
            && Pair("$p AnswerA#Hello.|Hello.", "$p AnswerA#${^}$|Tạm biệt.", 0, "Hello.") == null
            && Pair("$p AnswerA#Hello.|Goodbye.#$b#More.", "$p AnswerA#Chào.|Tạm biệt.#$b#Nữa.", 0, "Hello.") == null,
            "Blank responses and unsupported prerequisite page structures fall back");

        Assert(Pair("$p AnswerA#Hello.|Hello.", "$p AnswerA#Đã chọn.|Chưa chọn.", 0, "Hello.", answered: id => id == "AnswerA") == "Đã chọn."
            && Pair("$p AnswerA AnswerB#Hello.|Goodbye.", "$p AnswerA AnswerB#Chào.|Tạm biệt.", 0, "Hello.", answered: id => id == "AnswerB") == "Chào."
            && Pair("$p AnswerA#Hello.|Hello.", "$p AnswerA#Đã chọn.|Chưa chọn.", 0, "Hello.", answered: _ => false) == "Chưa chọn."
            && Pair("$p AnswerA#Hello.|Goodbye.", "$p AnswerA#Chào.|Tạm biệt.", 0, "Goodbye.", answered: _ => true) == null,
            "Previous-response history resolves identical primary branches and must agree with live text");

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
        var groups = new[] { "branch", "question", "pairs", "fallback", "layout", "sources", "narration", "qfallback" }.Select(group => Run(group)!).ToArray();
        Assert(groups.All(group => group.Length > 0 && group.All(check => check.Passed))
            && Run("coverage") is { Length: 5 } coverageChecks && coverageChecks.All(check => check.Passed)
            && Run("question") is { Length: 8 } questionChecks && questionChecks.All(check => check.Passed)
            && Run("state") is { Length: 4 } stateChecks && stateChecks.All(check => check.Passed)
            && Run("response") is { Length: 5 } responseChecks && responseChecks.All(check => check.Passed)
            && Run("all")!.SequenceEqual(groups.SelectMany(group => group)) && Run("invalid") == null,
            "Console dialogue checks run individually and aggregate the same passing results");

        var original = AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.draw), new[] { typeof(SpriteBatch) });
        var patch = mod.GetType("UntranslatedStardew.Patches.DialogueBoxPatch", true)!;
        var harmony = new Harmony("UntranslatedStardew.Tests.DialoguePrototype");
        try
        {
            harmony.CreateClassProcessor(patch).Patch();
            var questionPatch = mod.GetType("UntranslatedStardew.Patches.DialogueQuestionPatch", true)!;
            harmony.CreateClassProcessor(questionPatch).Patch();
            var measure = AccessTools.Method(typeof(SpriteText), nameof(SpriteText.getHeightOfString));
            var questionMeasure = AccessTools.Method(questionPatch, "Measure");
            foreach (string method in new[] { "setUpQuestions", "performHoverAction", "update", "draw" })
            {
                var target = AccessTools.Method(typeof(DialogueBox), method, method == "draw" ? new[] { typeof(SpriteBatch) } : null);
                var instructions = PatchProcessor.GetCurrentInstructions(target).ToArray();
                Assert(!instructions.Any(i => i.Calls(measure)) && instructions.Any(i => i.Calls(questionMeasure)),
                    $"Question height wrapper covers every call in {method}");
            }
            var sourcePatch = mod.GetType("UntranslatedStardew.Patches.DialogueSourcePatch", true)!;
            harmony.CreateClassProcessor(sourcePatch).Patch();
            var sourcePrefix = AccessTools.Method(sourcePatch, "Prefix");
            var getKey = AccessTools.Method(sourcePatch, "GetKey");
            var emptyDialogue = (Dialogue)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Dialogue));
            AccessTools.Field(typeof(Dialogue), nameof(Dialogue.dialogues)).SetValue(emptyDialogue, new List<DialogueLine>());
            AccessTools.Field(typeof(Dialogue), "indexesWithoutPortrait").SetValue(emptyDialogue, new HashSet<int>());
            AccessTools.Field(typeof(Dialogue), nameof(Dialogue.TranslationKey)).SetValue(emptyDialogue, "Characters/Dialogue/Evelyn:Question");
            var unreparsedCopy = new Dialogue(emptyDialogue);
            Assert((string?)getKey.Invoke(null, new object[] { unreparsedCopy }) == "Characters/Dialogue/Evelyn:Question",
                "Copying an unreparsed dialogue preserves its initial translation key");
            sourcePrefix.Invoke(null, new object[] { emptyDialogue, "Characters/Dialogue/Evelyn:Question" });
            sourcePrefix.Invoke(null, new object[] { emptyDialogue, "Characters/Dialogue/Evelyn:Reaction" });
            Assert((string?)getKey.Invoke(null, new object[] { emptyDialogue }) == "Characters/Dialogue/Evelyn:Reaction",
                "A reparsed dialogue uses its reaction key rather than the original question key");
            var reactionCopy = new Dialogue(emptyDialogue);
            Assert(reactionCopy.TranslationKey == "Characters/Dialogue/Evelyn:Question"
                && (string?)getKey.Invoke(null, new object[] { reactionCopy }) == "Characters/Dialogue/Evelyn:Reaction",
                "Copying a reparsed dialogue preserves the current reaction key despite a stale readonly TranslationKey");
            sourcePrefix.Invoke(null, new object?[] { emptyDialogue, null });
            Assert(getKey.Invoke(null, new object[] { emptyDialogue }) == null,
                "An unkeyed reparse clears stale translation provenance");
            var clearedCopy = new Dialogue(emptyDialogue);
            Assert(clearedCopy.TranslationKey == "Characters/Dialogue/Evelyn:Question"
                && getKey.Invoke(null, new object[] { clearedCopy }) == null,
                "Copying a dialogue after an unkeyed reparse preserves intentional null provenance over a stale TranslationKey");

            var tryParseSourceKey = pair.DeclaringType!.GetMethod("TryParseSourceKey", BindingFlags.Static | BindingFlags.NonPublic)!;
            (string Asset, string Key)? ParseSource(string? rawKey)
            {
                object?[] args = { rawKey, null, null };
                return tryParseSourceKey.Invoke(null, args) is true ? ((string)args[1]!, (string)args[2]!) : null;
            }
            Assert(ParseSource(@"Characters\Dialogue\Abigail:Mon") == ("Characters/Dialogue/Abigail", "Mon")
                && ParseSource(@"Strings\schedules\Abigail:marriage_Mon.000") == ("Strings/schedules/Abigail", "marriage_Mon.000")
                && ParseSource(@"Data\ExtraDialogue:Clint_No InventorySpace") == ("Data/ExtraDialogue", "Clint_No InventorySpace")
                && ParseSource(@"Data\EngagementDialogue:Abigail0") == ("Data/EngagementDialogue", "Abigail0")
                && ParseSource(@"Strings\Characters:WipedMemory") == ("Strings/Characters", "WipedMemory")
                && ParseSource(@"Strings\Events:Birth0") == ("Strings/Events", "Birth0")
                && ParseSource(@"Strings\StringsFromCSFiles:NPC.cs.3926") == ("Strings/StringsFromCSFiles", "NPC.cs.3926")
                && ParseSource(@"Strings\Locations:FieldOfficeDonated_0") == ("Strings/Locations", "FieldOfficeDonated_0")
                && ParseSource(@"Strings\1_6_Strings:Fizz_Intro_1") == ("Strings/1_6_Strings", "Fizz_Intro_1")
                && ParseSource(@"Strings\StringsFromMaps:BusStop_Desert") == ("Strings/StringsFromMaps", "BusStop_Desert")
                && ParseSource(@"Strings\SimpleNonVillagerDialogues:Derp") == ("Strings/SimpleNonVillagerDialogues", "Derp")
                && ParseSource(@"Strings\animationDescriptions:abigail_videogames") == ("Strings/animationDescriptions", "abigail_videogames")
                && ParseSource(@"Strings\SpeechBubbles:Abigail_Greeting") == ("Strings/SpeechBubbles", "Abigail_Greeting"),
                "Dialogue source parser accepts and normalizes supported vanilla dictionary dialogue assets");
            foreach (string rejected in new[]
            {
                "Data/IncomingPhoneCalls:Abigail",
                "Data/Objects:74",
                "Data/Quests:1",
                "Data/Events/Town:1",
                "Data/Festivals/spring13:Abigail",
                "Data/mail:wizard",
                "Strings/UI:Confirm",
                "Strings/Objects:Parsnip_Name",
                "Characters/Dialogue/../Abigail:Mon",
                "Characters/Dialogue/sub/Abigail:Mon",
                "Strings/schedules/../Abigail:key",
                "Strings/schedules/sub/Abigail:key",
                "Characters/Dialogue/Abigail:",
                ":Mon",
                ""
            })
            {
                Assert(ParseSource(rejected) == null, "Dialogue source parser rejects unsupported or unsafe source: " + rejected);
            }

            using (var enProvider = new RawContentProvider(() => new DummyServiceProvider(), Path.Combine(game, "Content"), "en"))
            {
                var tryGetDialogueString = AccessTools.Method(typeof(RawContentProvider), "TryGetDialogueString")!;
                string? LoadDialogue(string asset, string key)
                {
                    object?[] args = { asset, key, null };
                    return tryGetDialogueString.Invoke(enProvider, args) is true ? (string?)args[2] : null;
                }
                foreach (string rawSource in new[]
                {
                    @"Data\ExtraDialogue:Wizard_Hatch",
                    @"Data\EngagementDialogue:Abigail0",
                    @"Strings\Characters:WipedMemory",
                    @"Strings\Events:ElliottBook_default",
                    @"Strings\StringsFromCSFiles:NPC.cs.3926",
                    @"Strings\Locations:FieldOfficeDonated_0",
                    @"Strings\1_6_Strings:Jas_IceCream"
                })
                {
                    var parsed = ParseSource(rawSource);
                    Assert(parsed is { } source && !string.IsNullOrWhiteSpace(LoadDialogue(source.Asset, source.Key)),
                        "Installed content resolves broadened dictionary dialogue source: " + rawSource);
                }

                string? rawFormatted = LoadDialogue("Strings/Characters", "MovieInvite_CantInvite");
                Assert(rawFormatted != null && rawFormatted.Contains("{0}", StringComparison.Ordinal)
                    && Pair(rawFormatted, "Không thể mời {0}!", 0, rawFormatted.Replace("{0}", "Abigail")) == null,
                    "Preprocessed formatted dialogue with substituted arguments falls back instead of mispairing");
            }

            var wrapper = AccessTools.Method(patch, "DrawDialogueText");
            Assert(PatchProcessor.GetCurrentInstructions(original).Count(instruction => instruction.Calls(wrapper)) == 4,
                "Harmony installs NPC body, question and response draw wrappers in Stardew 1.6.15");
        }
        finally
        {
            foreach (string method in new[] { "setUpQuestions", "performHoverAction", "update", "draw" })
                harmony.Unpatch(AccessTools.Method(typeof(DialogueBox), method, method == "draw" ? new[] { typeof(SpriteBatch) } : null), HarmonyPatchType.All, harmony.Id);
            harmony.Unpatch(AccessTools.Method(typeof(Dialogue), "parseDialogueString"), HarmonyPatchType.All, harmony.Id);
            harmony.Unpatch(AccessTools.Constructor(typeof(Dialogue), new[] { typeof(Dialogue) }), HarmonyPatchType.All, harmony.Id);
            harmony.Unpatch(AccessTools.Constructor(typeof(Dialogue), new[] { typeof(NPC), typeof(string), typeof(string) }), HarmonyPatchType.All, harmony.Id);
        }
    }
}
