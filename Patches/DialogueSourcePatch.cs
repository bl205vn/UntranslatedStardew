using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewValley;

namespace UntranslatedStardew.Patches;

/// <summary>Tracks reaction keys when vanilla reparses a Dialogue after an answer.</summary>
[HarmonyPatch(typeof(Dialogue), "parseDialogueString")]
internal static class DialogueSourcePatch
{
    private sealed record Source(string? Key);
    private static readonly ConditionalWeakTable<Dialogue, Source> Sources = new();

    private static void Prefix(Dialogue __instance, string translationKey)
    {
        Sources.Remove(__instance);
        Sources.Add(__instance, new(translationKey));
    }

    internal static string? GetKey(Dialogue dialogue)
        => Sources.TryGetValue(dialogue, out var source) ? source.Key : dialogue.TranslationKey;
}
