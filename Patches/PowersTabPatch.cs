using System;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>
/// Harmony patch on PowersTab.performHoverAction to display bilingual titles
/// for unlocked special items and powers.
/// </summary>
[HarmonyPatch(typeof(PowersTab), nameof(PowersTab.performHoverAction))]
internal static class PowersTabPatch
{
    private static bool _errorLogged;

    public static void Postfix(PowersTab __instance, int x, int y)
    {
        try
        {
            if (ModEntry.Instance?.Config.ShowSecondaryInTooltip != true || ModEntry.Instance.Resolver == null)
                return;

            if (string.IsNullOrEmpty(__instance.hoverText) || __instance.hoverText == "???")
                return;

            if (__instance.powers == null || __instance.currentPage < 0 || __instance.currentPage >= __instance.powers.Count)
                return;

            ItemNameResolver resolver = ModEntry.Instance.Resolver;
            bool secondaryFirst = ModEntry.Instance.Config.SecondaryFirst;

            foreach (ClickableTextureComponent c in __instance.powers[__instance.currentPage])
            {
                if (c.containsPoint(x, y) && c.drawShadow)
                {
                    string powerKey = c.name;
                    string? secondaryTitle = resolver.GetSecondaryPowerName(powerKey);
                    if (!string.IsNullOrWhiteSpace(secondaryTitle))
                    {
                        string formattedTitle = TooltipFormatter.FormatTitle(c.label, c.label, secondaryTitle, secondaryFirst);
                        BilingualStringTracker.Register(formattedTitle);
                        __instance.hoverText = formattedTitle;
                        using (BilingualRenderScope.Begin())
                        {
                            __instance.descriptionText = Game1.parseText(c.hoverText, Game1.smallFont, Math.Max((int)Game1.dialogueFont.MeasureString(__instance.hoverText).X, 320));
                        }
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            if (!_errorLogged)
            {
                _errorLogged = true;
                ModEntry.Instance?.Monitor.Log($"Error in PowersTab hover action: {ex.Message}", LogLevel.Warn);
            }
        }
    }
}
