using HarmonyLib;
using StardewValley;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

[HarmonyPatch(typeof(Event), nameof(Event.TryGetFestivalDataForYear), new[]
    { typeof(string), typeof(string), typeof(string) }, new[]
    { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out })]
internal static class FestivalDialogueSourcePatch
{
    private static void Postfix(Event __instance, string data, string actualKey, bool __result, string ___festivalDataAssetName)
    {
        if (__result && __instance.isFestival && data != null && actualKey != null && ___festivalDataAssetName != null)
            FestivalDialogueSource.Capture(__instance, ___festivalDataAssetName, actualKey, data);
    }
}
