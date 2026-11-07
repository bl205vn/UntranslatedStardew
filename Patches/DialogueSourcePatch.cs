using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewValley;

namespace UntranslatedStardew.Patches;

/// <summary>Tracks reaction keys when vanilla reparses or copies a Dialogue.</summary>
[HarmonyPatch(typeof(Dialogue))]
internal static class DialogueSourcePatch
{
    private sealed record Source(string? Key);
    private static readonly ConditionalWeakTable<Dialogue, Source> Sources = new();

    [HarmonyPrefix]
    [HarmonyPatch("parseDialogueString", typeof(string), typeof(string))]
    private static void Prefix(Dialogue __instance, string translationKey)
    {
        Sources.Remove(__instance);
        Sources.Add(__instance, new(translationKey));
    }

    [HarmonyPostfix]
    [HarmonyPatch(MethodType.Constructor, typeof(Dialogue))]
    private static void Copy(Dialogue __instance, Dialogue? other)
    {
        Sources.Remove(__instance);
        if (other != null)
            Sources.Add(__instance, new(GetKey(other)));
    }

    internal static string? GetKey(Dialogue dialogue)
        => Sources.TryGetValue(dialogue, out var source) ? source.Key : dialogue.TranslationKey;
}
