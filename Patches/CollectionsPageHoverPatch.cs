using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>
/// Harmony patch on CollectionsPage.performHoverAction to display bilingual titles
/// for letters (tab 7) and side tabs on hover.
/// Leaves tabs 0..6 to createDescription, preventing double formatting.
/// </summary>
[HarmonyPatch(typeof(CollectionsPage), nameof(CollectionsPage.performHoverAction), new[] { typeof(int), typeof(int) })]
internal static class CollectionsPageHoverPatch
{
    private static bool _errorLogged;

    public static void Postfix(CollectionsPage __instance, int x, int y, ref string ___hoverText)
    {
        try
        {
            // If the letter viewer submenu is actively open, do not hover underlying letters grid
            if (__instance.letterviewerSubMenu != null)
                return;

            if (ModEntry.Instance?.Config.ShowSecondaryInTooltip != true || ModEntry.Instance.Resolver == null)
                return;

            if (string.IsNullOrEmpty(___hoverText) || ___hoverText == "???")
                return;

            ItemNameResolver resolver = ModEntry.Instance.Resolver;
            bool secondaryFirst = ModEntry.Instance.Config.SecondaryFirst;

            // 1. Check sideTabs hover
            if (__instance.sideTabs != null)
            {
                foreach (var kvp in __instance.sideTabs)
                {
                    ClickableTextureComponent c = kvp.Value;
                    if (c != null && c.containsPoint(x, y))
                    {
                        int tabId = kvp.Key;
                        string? secTabName = resolver.GetSecondarySideTabName(tabId);
                        if (!string.IsNullOrWhiteSpace(secTabName) && !string.Equals(c.hoverText.Trim(), secTabName.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            string parsedPrimary = Game1.parseText(c.hoverText, Game1.smallFont, 256);
                            string parsedSecondary = Game1.parseText(secTabName, Game1.smallFont, 256);
                            ___hoverText = secondaryFirst
                                ? $"{parsedSecondary}\n{parsedPrimary}"
                                : $"{parsedPrimary}\n{parsedSecondary}";
                        }
                        return;
                    }
                }
            }

            // 2. Check Letters tab (tab 7)
            if (__instance.currentTab == CollectionsPage.lettersTab && __instance.collections != null)
            {
                if (__instance.collections.TryGetValue(CollectionsPage.lettersTab, out var pages)
                    && __instance.currentPage >= 0 && __instance.currentPage < pages.Count)
                {
                    foreach (ClickableTextureComponent c2 in pages[__instance.currentPage])
                    {
                        if (c2.containsPoint(x, y, 2))
                        {
                            string[] data_split = ArgUtility.SplitBySpace(c2.name);
                            if (data_split.Length > 1 && Convert.ToBoolean(data_split[1]))
                            {
                                string mailId = data_split[0];
                                int firstSpace = c2.name.IndexOf(' ');
                                if (firstSpace >= 0)
                                {
                                    int secondSpace = c2.name.IndexOf(' ', firstSpace + 1);
                                    if (secondSpace >= 0 && secondSpace + 1 < c2.name.Length)
                                    {
                                        string primaryTitle = c2.name.Substring(secondSpace + 1);
                                        if (!string.IsNullOrEmpty(primaryTitle) && primaryTitle != "???")
                                        {
                                            string? secTitle = resolver.GetSecondaryMailTitle(mailId);
                                            if (!string.IsNullOrWhiteSpace(secTitle) && !string.Equals(primaryTitle.Trim(), secTitle.Trim(), StringComparison.OrdinalIgnoreCase))
                                            {
                                                // Parse primary and secondary sections independently to 256px
                                                // so native wrapping of long primary titles cannot suppress the secondary title
                                                string parsedPrimary = Game1.parseText(primaryTitle, Game1.smallFont, 256);
                                                string parsedSecondary = Game1.parseText(secTitle, Game1.smallFont, 256);
                                                ___hoverText = secondaryFirst
                                                    ? $"{parsedSecondary}\n{parsedPrimary}"
                                                    : $"{parsedPrimary}\n{parsedSecondary}";
                                            }
                                        }
                                    }
                                }
                            }
                            return;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (!_errorLogged)
            {
                _errorLogged = true;
                ModEntry.Instance?.Monitor.Log($"Error in CollectionsPage performHoverAction postfix: {ex.Message}", LogLevel.Warn);
            }
        }
    }
}
