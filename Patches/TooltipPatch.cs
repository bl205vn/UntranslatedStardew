using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace UntranslatedStardew.Patches;

/// <summary>
/// Harmony patch on IClickableMenu.drawToolTip to intercept the hover title
/// and append/prepend the secondary item name.
/// </summary>
[HarmonyPatch(typeof(IClickableMenu), nameof(IClickableMenu.drawToolTip))]
internal static class TooltipPatch
{
    private static bool _errorLogged;

    public static void Prefix(ref string hoverTitle, Item? hoveredItem)
    {
        try
        {
            if (hoveredItem == null || string.IsNullOrEmpty(hoverTitle))
                return;

            ModEntry.Instance?.FormatTooltipTitle(ref hoverTitle, hoveredItem);
        }
        catch (Exception ex)
        {
            if (!_errorLogged)
            {
                _errorLogged = true;
                ModEntry.Instance?.Monitor.Log($"Error in drawToolTip prefix: {ex.Message}", LogLevel.Warn);
            }
        }
    }
}
