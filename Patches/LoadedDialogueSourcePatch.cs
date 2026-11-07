using HarmonyLib;
using StardewValley;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

[HarmonyPatch(typeof(LocalizedContentManager), nameof(LocalizedContentManager.LoadStringReturnNullIfNotFound), new[] { typeof(string), typeof(bool) })]
internal static class LoadedDialogueSourcePatch
{
    private static void Postfix(LocalizedContentManager __instance, string path, string? __result)
    {
        if (ReferenceEquals(__instance, Game1.content)) LoadedDialogueSource.Capture(__result, path);
    }
}
