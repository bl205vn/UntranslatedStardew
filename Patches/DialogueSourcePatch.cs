using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>Tracks reaction keys when vanilla reparses or copies a Dialogue.</summary>
[HarmonyPatch(typeof(Dialogue))]
internal static class DialogueSourcePatch
{
    private sealed record Source(string? Key, int BaseIndex = 0, GiftAdviceTranslation.Advice? Advice = null);
    private static readonly ConditionalWeakTable<Dialogue, Source> Sources = new();

    [HarmonyPrefix]
    [HarmonyPatch("parseDialogueString", typeof(string), typeof(string))]
    private static void Prefix(Dialogue __instance, string translationKey)
    {
        // chooseResponse appends the reaction to the existing dialogue list.
        // Its source dictionary starts at page zero, not at the absolute list index.
        int baseIndex = __instance.dialogues?.Count ?? 0;
        Sources.Remove(__instance);
        Sources.Add(__instance, new(translationKey, baseIndex));
    }

    [HarmonyPostfix]
    [HarmonyPatch(MethodType.Constructor, typeof(Dialogue))]
    private static void Copy(Dialogue __instance, Dialogue? other)
    {
        Sources.Remove(__instance);
        if (other != null)
        {
            if (Sources.TryGetValue(other, out var source))
                Sources.Add(__instance, source);
            else
                Sources.Add(__instance, new(other.TranslationKey, 0));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(MethodType.Constructor, typeof(NPC), typeof(string), typeof(string))]
    private static void CaptureLoaded(Dialogue __instance, string translationKey, string dialogueText)
    {
        if (translationKey == null && GetKey(__instance) == null && dialogueText != null)
        {
            if (LoadedDialogueSource.Get(dialogueText) is { } source)
            {
                Sources.Remove(__instance);
                Sources.Add(__instance, new(source, 0));
            }
            else if (GiftAdviceCapturePatch.Resolve(__instance.speaker, dialogueText) is { } advice)
            {
                Sources.Remove(__instance);
                Sources.Add(__instance, new(null, 0, advice));
            }
        }
    }

    internal static string? GetKey(Dialogue dialogue)
        => Sources.TryGetValue(dialogue, out var source) ? source.Key : dialogue.TranslationKey;

    internal static GiftAdviceTranslation.Advice? GetGiftAdvice(Dialogue dialogue)
        => Sources.TryGetValue(dialogue, out var source) ? source.Advice : null;

    internal static int GetBaseIndex(Dialogue dialogue)
        => Sources.TryGetValue(dialogue, out var source) ? source.BaseIndex : 0;

    internal static int GetRelativeIndex(Dialogue dialogue)
        => dialogue.currentDialogueIndex - GetBaseIndex(dialogue);
}
