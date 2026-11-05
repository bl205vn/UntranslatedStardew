using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>
/// Harmony patch on IClickableMenu.drawToolTip to intercept the hover title
/// and append/prepend the secondary item name, registering it for scoped fallback rendering.
/// </summary>
[HarmonyPatch(typeof(IClickableMenu), nameof(IClickableMenu.drawToolTip))]
internal static class TooltipPatch
{
    private static bool _errorLogged;

    public static void Prefix(ref string hoverTitle, Item? hoveredItem, out IDisposable? __state)
    {
        __state = null;
        try
        {
            if (hoveredItem == null || string.IsNullOrEmpty(hoverTitle))
                return;

            ModEntry.Instance?.FormatTooltipTitle(ref hoverTitle, hoveredItem);
            BilingualStringTracker.Register(hoverTitle);
            __state = BilingualRenderScope.Begin();
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

    public static void Postfix(IDisposable? __state)
    {
        __state?.Dispose();
    }

    public static Exception? Finalizer(IDisposable? __state, Exception? __exception)
    {
        __state?.Dispose();
        return __exception;
    }
}

/// <summary>
/// Harmony patch on IClickableMenu.drawHoverText (string overload) to enter BilingualRenderScope
/// whenever tracked bilingual text or bold titles are being rendered.
/// </summary>
[HarmonyPatch(typeof(IClickableMenu), nameof(IClickableMenu.drawHoverText), new[]
{
    typeof(SpriteBatch), typeof(string), typeof(SpriteFont), typeof(int), typeof(int), typeof(int), typeof(string),
    typeof(int), typeof(string[]), typeof(Item), typeof(int), typeof(string), typeof(int), typeof(int), typeof(int),
    typeof(float), typeof(CraftingRecipe), typeof(IList<Item>), typeof(Texture2D), typeof(Rectangle?), typeof(Color?),
    typeof(Color?), typeof(float), typeof(int), typeof(int)
})]
internal static class DrawHoverTextStringPatch
{
    public static void Prefix(string? text, string? boldTitleText, out IDisposable? __state)
    {
        __state = null;
        if (BilingualStringTracker.IsTracked(text) || BilingualStringTracker.IsTracked(boldTitleText))
        {
            __state = BilingualRenderScope.Begin();
        }
    }

    public static void Postfix(IDisposable? __state)
    {
        __state?.Dispose();
    }

    public static Exception? Finalizer(IDisposable? __state, Exception? __exception)
    {
        __state?.Dispose();
        return __exception;
    }
}

/// <summary>
/// Harmony patch on IClickableMenu.drawHoverText (StringBuilder overload) to enter BilingualRenderScope
/// whenever tracked bilingual text or bold titles are being rendered.
/// </summary>
[HarmonyPatch(typeof(IClickableMenu), nameof(IClickableMenu.drawHoverText), new[]
{
    typeof(SpriteBatch), typeof(StringBuilder), typeof(SpriteFont), typeof(int), typeof(int), typeof(int), typeof(string),
    typeof(int), typeof(string[]), typeof(Item), typeof(int), typeof(string), typeof(int), typeof(int), typeof(int),
    typeof(float), typeof(CraftingRecipe), typeof(IList<Item>), typeof(Texture2D), typeof(Rectangle?), typeof(Color?),
    typeof(Color?), typeof(float), typeof(int), typeof(int)
})]
internal static class DrawHoverTextStringBuilderPatch
{
    public static void Prefix(StringBuilder? text, string? boldTitleText, out IDisposable? __state)
    {
        __state = null;
        string? textStr = text?.ToString();
        if (BilingualStringTracker.IsTracked(textStr) || BilingualStringTracker.IsTracked(boldTitleText))
        {
            __state = BilingualRenderScope.Begin();
        }
    }

    public static void Postfix(IDisposable? __state)
    {
        __state?.Dispose();
    }

    public static Exception? Finalizer(IDisposable? __state, Exception? __exception)
    {
        __state?.Dispose();
        return __exception;
    }
}
