using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using UntranslatedStardew.Core;
using UntranslatedStardew.Patches;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static void TestLocationQuestions()
    {
        LoadedDialogueSource.Invalidate();
        string prompt = new string("-Dark Shrine-^Make an offering?".ToCharArray());
        string yes = new string("Yes".ToCharArray()), no = new string("No".ToCharArray());
        const string promptKey = "Strings/Locations:WitchHut_EvilShrineLeft";
        const string yesKey = "Strings/Lexicon:QuestionDialogue_Yes", noKey = "Strings/Lexicon:QuestionDialogue_No";
        var sources = new Dictionary<string, (string Primary, string Secondary)>
        {
            [promptKey] = (prompt, "-Điện thờ-^Dâng lễ vật?"),
            [yesKey] = (yes, "Có"), [noKey] = (no, "Không")
        };
        (string, string)? Lookup(string path) => sources.TryGetValue(path, out var source) ? source : null;
        LoadedDialogueSource.Capture(prompt, promptKey);
        LoadedDialogueSource.Capture(yes, yesKey);
        LoadedDialogueSource.Capture(no, noKey);
        var answers = new[] { ("Yes", yes), ("No", no) };
        var captured = LocationQuestionTranslation.Capture(prompt, answers, LoadedDialogueSource.Get)!;
        var question = LocationQuestionTranslation.Match(captured, prompt, answers, Lookup, Gender.Female);
        Assert(question != null && question.Prompt.Secondary == "-Điện thờ-\nDâng lễ vật?"
            && question.Answers[0].Secondary == "Có" && question.Answers[1].Secondary == "Không",
            "Source-backed location question pairs a multiline prompt and both Yes/No labels");
        Assert(LocationQuestionTranslation.Capture(new string(prompt.ToCharArray()), answers, LoadedDialogueSource.Get) == null
            && LocationQuestionTranslation.Capture(prompt, new[] { ("Yes", new string(yes.ToCharArray())), ("No", no) }, LoadedDialogueSource.Get) == null,
            "Equal visible strings without captured instance provenance are not guessed for prompt or answers");
        Assert(LocationQuestionTranslation.Capture(prompt, new[] { ("Same", yes), ("Same", no) }, LoadedDialogueSource.Get) == null,
            "Duplicate response keys reject the whole location question");
        Assert(LocationQuestionTranslation.Match(captured, "Changed prompt", answers, Lookup, Gender.Female) == null
            && LocationQuestionTranslation.Match(captured, prompt, new[] { answers[1], answers[0] }, Lookup, Gender.Female) == null
            && LocationQuestionTranslation.Match(captured, prompt, new[] { ("Yes", "Changed"), ("No", no) }, Lookup, Gender.Female) == null,
            "Changed prompt, response order or response label falls back as a whole");
        sources.Remove(noKey);
        Assert(LocationQuestionTranslation.Match(captured, prompt, answers, Lookup, Gender.Female) == null,
            "Missing one translated answer suppresses the whole bilingual location question");
        sources[noKey] = (no, "Không");
        sources[promptKey] = ("A different current source", "Khác");
        Assert(LocationQuestionTranslation.Match(captured, prompt, answers, Lookup, Gender.Female) == null,
            "A changed primary source cannot translate a stale question");
        foreach (string unsafeSub in new[] { "Question#$action AddMoney 1", "Question [74]", "Question {0}", "Page#Another" })
        {
            sources[promptKey] = (prompt, unsafeSub);
            Assert(LocationQuestionTranslation.Match(captured, prompt, answers, Lookup, Gender.Female) == null,
                "Location question rejects unsafe or multi-page translation: " + unsafeSub);
        }
        sources[promptKey] = (prompt, "-Điện thờ-^Dâng lễ vật?");
        sources[yesKey] = (yes, yes);
        Assert(LocationQuestionTranslation.Match(captured, prompt, answers, Lookup, Gender.Female)?.Answers[0].Secondary == yes,
            "An identical translated answer is permitted for single-block duplicate suppression");
        sources[yesKey] = (yes, "Có");
        var pairs = new[] { question!.Prompt }.Concat(question.Answers).ToArray();
        var above = DialogueQuestionLayout.FitRows(pairs, 100, 500, true, _ => 20, text => text.Length,
            text => text.Split('\n').Length * 10, out _);
        var below = DialogueQuestionLayout.FitRows(pairs, 100, 500, false, _ => 20, text => text.Length,
            text => text.Split('\n').Length * 10, out _);
        Assert(above != null && below != null && above[prompt].Height == below[prompt].Height
            && above[prompt].Rows[0].SecondaryOffset < above[prompt].Rows[0].PrimaryOffset,
            "Location questions share block geometry for above/below rendering and response hitboxes");

        var box = (DialogueBox)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(DialogueBox));
        box.dialogues = new() { prompt };
        box.responses = new[] { new Response("Yes", yes), new Response("No", no) };
        var originalResponses = box.responses;
        AccessTools.Method(typeof(LocationQuestionPatch), "Prefix").Invoke(null, new object[] { box, prompt, box.responses });
        Assert(LocationQuestionPatch.Resolve(box, prompt, Lookup, Gender.Female) != null && ReferenceEquals(box.responses, originalResponses)
            && box.responses[0].responseKey == "Yes" && box.responses[1].responseKey == "No",
            "Constructor capture resolves location sources without replacing response objects or action keys");
        LoadedDialogueSource.Invalidate();
        Assert(LocationQuestionPatch.Resolve(box, prompt, Lookup, Gender.Female) == null,
            "Content invalidation suppresses stale captured location-question sources");

        var harmony = new Harmony("UntranslatedStardew.Tests.LocationQuestion");
        try
        {
            harmony.CreateClassProcessor(typeof(LocationQuestionPatch)).Patch();
            var ctor = AccessTools.Constructor(typeof(DialogueBox), new[] { typeof(string), typeof(Response[]), typeof(int) });
            var info = Harmony.GetPatchInfo(ctor);
            Assert(info?.Prefixes.Any(patch => patch.owner == harmony.Id) == true
                && info.Postfixes.Any(patch => patch.owner == harmony.Id),
                "Location question constructor capture and geometry refresh install at the real Harmony boundary");
        }
        finally { harmony.UnpatchAll(harmony.Id); LoadedDialogueSource.Invalidate(); }
    }
}
