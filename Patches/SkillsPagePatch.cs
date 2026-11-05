using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Menus;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>
/// Harmony patch on SkillsPage.performHoverAction to display bilingual titles
/// for professions and skills on hover.
/// </summary>
[HarmonyPatch(typeof(SkillsPage), nameof(SkillsPage.performHoverAction))]
internal static class SkillsPagePatch
{
    private static bool _errorLogged;

    public static void Postfix(SkillsPage __instance, int x, int y, ref string ___hoverTitle)
    {
        try
        {
            if (ModEntry.Instance?.Config.ShowSecondaryInTooltip != true || ModEntry.Instance.Resolver == null)
                return;

            if (string.IsNullOrWhiteSpace(___hoverTitle))
                return;

            ItemNameResolver resolver = ModEntry.Instance.Resolver;
            bool secondaryFirst = ModEntry.Instance.Config.SecondaryFirst;

            // 1. Check profession bars (level 5 and 10 professions)
            if (__instance.skillBars != null)
            {
                foreach (ClickableTextureComponent c in __instance.skillBars)
                {
                    if (c.containsPoint(x, y) && c.hoverText.Length > 0 && !c.name.Equals("-1"))
                    {
                        if (int.TryParse(c.name, out int professionNumber))
                        {
                            string? secTitle = resolver.GetSecondaryProfessionTitle(professionNumber);
                            if (!string.IsNullOrWhiteSpace(secTitle))
                            {
                                ___hoverTitle = TooltipFormatter.FormatTitle(___hoverTitle, ___hoverTitle, secTitle, secondaryFirst);
                            }
                        }
                        return;
                    }
                }
            }

            // 2. Check skill areas (Farming, Fishing, Foraging, Mining, Combat, Luck)
            if (__instance.skillAreas != null)
            {
                foreach (ClickableTextureComponent c in __instance.skillAreas)
                {
                    if (c.containsPoint(x, y) && c.hoverText.Length > 0)
                    {
                        if (int.TryParse(c.name, out int skillIndex))
                        {
                            string? secSkill = resolver.GetSecondarySkillName(skillIndex);
                            if (!string.IsNullOrWhiteSpace(secSkill))
                            {
                                ___hoverTitle = TooltipFormatter.FormatTitle(___hoverTitle, ___hoverTitle, secSkill, secondaryFirst);
                            }
                        }
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (!_errorLogged)
            {
                _errorLogged = true;
                ModEntry.Instance?.Monitor.Log($"Error in SkillsPage hover action: {ex.Message}", LogLevel.Warn);
            }
        }
    }
}
