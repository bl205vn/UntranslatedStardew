using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>Captures exact loaded-string provenance before vanilla constructs a location question.</summary>
[HarmonyPatch]
internal static class LocationQuestionPatch
{
    private sealed record Source(LocationQuestionTranslation.Captured Question, int Generation);
    private static readonly ConditionalWeakTable<DialogueBox, Source> Sources = new();
    private static MethodBase TargetMethod() => AccessTools.Constructor(typeof(DialogueBox),
        new[] { typeof(string), typeof(Response[]), typeof(int) });

    private static void Prefix(DialogueBox __instance, string dialogue, Response[] responses)
    {
        Sources.Remove(__instance);
        if (responses == null) return;
        var captured = LocationQuestionTranslation.Capture(dialogue,
            responses.Select(response => (response.responseKey, response.responseText)).ToArray(), LoadedDialogueSource.Get);
        if (captured != null) Sources.Add(__instance, new(captured, LoadedDialogueSource.Generation));
    }

    private static void Postfix(DialogueBox __instance)
    {
        // Vanilla preprocesses response gender blocks after its first geometry pass.
        if (Sources.TryGetValue(__instance, out _)) DialogueQuestionPatch.RebuildSourceQuestion(__instance);
    }

    internal static DialogueQuestionTranslation.Question? Resolve(DialogueBox box, string shown,
        Func<string, (string Primary, string Secondary)?> lookup, Gender gender)
        => box.characterDialogue == null && Sources.TryGetValue(box, out var source)
            && source.Generation == LoadedDialogueSource.Generation
            ? LocationQuestionTranslation.Match(source.Question, shown,
                box.responses.Select(response => (response.responseKey, response.responseText)).ToArray(), lookup, gender)
            : null;
}
