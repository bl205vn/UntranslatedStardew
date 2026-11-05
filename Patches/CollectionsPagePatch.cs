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

            // Letters (tab 7) bypass createDescription and are handled in performHoverAction
            if (__instance.currentTab == CollectionsPage.lettersTab)
                return;

            ItemNameResolver resolver = ModEntry.Instance.Resolver;
            bool secondaryFirst = ModEntry.Instance.Config.SecondaryFirst;

            // Handle Secret notes and journal scraps (tab 6)
            if (__instance.currentTab == CollectionsPage.secretNotesTab)
            {
                if (int.TryParse(id, out int noteIndex))
                {
                    string? secondaryTitle = resolver.GetSecondarySecretNoteTitle(noteIndex);
                    if (!string.IsNullOrWhiteSpace(secondaryTitle))
                    {
                        // Detect newline delimiter: "\r\n\r\n" or "\n\n" for cross-platform newline consistency
                        string delimiter = "\r\n\r\n";
                        int delimIdx = __result.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                        if (delimIdx < 0)
                        {
                            delimIdx = __result.IndexOf("\n\n", StringComparison.Ordinal);
                            if (delimIdx >= 0)
                                delimiter = "\n\n";
                        }

                        if (delimIdx >= 0)
                        {
                            // Text note: title + delimiter + body
                            string primaryTitle = __result.Substring(0, delimIdx);
                            string remainder = __result.Substring(delimIdx + delimiter.Length);
                            if (string.Equals(primaryTitle.Trim(), secondaryTitle.Trim(), StringComparison.OrdinalIgnoreCase)) return;
                            StructuredTextComposition noteComp = StructuredTextComposition.CreateBilingual(
                                primaryChunk: primaryTitle,
                                secondaryChunk: secondaryTitle,
                                secondaryFirst: secondaryFirst,
                                delimiter: "\n",
                                body: remainder,
                                bodyDelimiter: delimiter
                            );
                            BilingualStringTracker.RegisterComposition(noteComp);
                            __result = noteComp.FullText;
                        }
                        else
                        {
                            // Image note: title only, no body
                            __result = TooltipFormatter.FormatTitle(__result, __result, secondaryTitle, secondaryFirst);
                        }
                    }
                }
                return;
            }

            string doubleNewline = Environment.NewLine + Environment.NewLine;
            int firstDoubleNewline = __result.IndexOf(doubleNewline, StringComparison.Ordinal);
            if (firstDoubleNewline < 0)
            {
                // Fallback for Unix/Windows cross-platform newline
                if (__result.IndexOf("\n\n", StringComparison.Ordinal) >= 0)
                {
                    doubleNewline = "\n\n";
                    firstDoubleNewline = __result.IndexOf(doubleNewline, StringComparison.Ordinal);
                }
            }
            if (firstDoubleNewline < 0)
                return;

            string primaryTitleItem = __result.Substring(0, firstDoubleNewline);
            string remainderItem = __result.Substring(firstDoubleNewline + doubleNewline.Length);

            string? secondaryItemTitle;
            if (__instance.currentTab == CollectionsPage.achievementsTab)
            {
                secondaryItemTitle = resolver.GetSecondaryAchievementTitle(id);
            }
            else
            {
                secondaryItemTitle = resolver.GetSecondaryCollectionItemTitle(id);
            }

            if (string.IsNullOrWhiteSpace(secondaryItemTitle))
                return;

            if (!string.Equals(primaryTitleItem.Trim(), secondaryItemTitle.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                StructuredTextComposition itemComp = StructuredTextComposition.CreateBilingual(
                    primaryChunk: primaryTitleItem,
                    secondaryChunk: secondaryItemTitle,
                    secondaryFirst: secondaryFirst,
                    delimiter: "\n",
                    body: remainderItem,
                    bodyDelimiter: doubleNewline
                );
                BilingualStringTracker.RegisterComposition(itemComp);
                __result = itemComp.FullText;
            }
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
