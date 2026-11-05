using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Menus;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>
/// Harmony patch on CollectionsPage.createDescription to inject the secondary display name
/// adjacent to the primary title while preserving description bodies, stats, counts, and concealments.
/// </summary>
[HarmonyPatch(typeof(CollectionsPage), nameof(CollectionsPage.createDescription), new[] { typeof(string) })]
internal static class CollectionsPagePatch
{
    private static bool _errorLogged;

    public static void Postfix(CollectionsPage __instance, string id, ref string __result)
    {
        try
        {
            if (ModEntry.Instance?.Config.ShowSecondaryInTooltip != true || ModEntry.Instance.Resolver == null)
                return;

            if (string.IsNullOrEmpty(__result) || __result == "???" || id == "???")
                return;

            // Secret notes (tab 6) and Letters (tab 7) are out of scope
            if (__instance.currentTab == CollectionsPage.secretNotesTab || __instance.currentTab == CollectionsPage.lettersTab)
                return;

            string doubleNewline = Environment.NewLine + Environment.NewLine;
            int firstDoubleNewline = __result.IndexOf(doubleNewline, StringComparison.Ordinal);
            if (firstDoubleNewline < 0)
                return;

            string primaryTitle = __result.Substring(0, firstDoubleNewline);
            string remainder = __result.Substring(firstDoubleNewline + doubleNewline.Length);

            ItemNameResolver resolver = ModEntry.Instance.Resolver;
            string? secondaryTitle;
            if (__instance.currentTab == CollectionsPage.achievementsTab)
            {
                secondaryTitle = resolver.GetSecondaryAchievementTitle(id);
            }
            else
            {
                secondaryTitle = resolver.GetSecondaryCollectionItemTitle(id);
            }

            if (string.IsNullOrWhiteSpace(secondaryTitle))
                return;

            string formattedTitle = TooltipFormatter.FormatTitle(primaryTitle, primaryTitle, secondaryTitle, ModEntry.Instance.Config.SecondaryFirst);
            __result = $"{formattedTitle}{doubleNewline}{remainder}";
        }
        catch (Exception ex)
        {
            if (!_errorLogged)
            {
                _errorLogged = true;
                ModEntry.Instance?.Monitor.Log($"Error in CollectionsPage createDescription: {ex.Message}", LogLevel.Warn);
            }
        }
    }
}
