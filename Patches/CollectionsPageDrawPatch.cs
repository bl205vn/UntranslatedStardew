using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>
/// Scoped state holding original secretNoteImage and hoverText during CollectionsPage.draw.
/// Guarantees that both fields are safely suppressed during vanilla draw and restored afterward.
/// </summary>
public sealed class CollectionsNoteImageState
{
    private static readonly FieldInfo? HoverTextField = typeof(CollectionsPage).GetField(
        "hoverText",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
    );

    public CollectionsPage Instance { get; }
    public int SecretNoteImage { get; }
    public string HoverText { get; }
    private bool _restored;

    public CollectionsNoteImageState(CollectionsPage instance, int secretNoteImage, string hoverText)
    {
        Instance = instance;
        SecretNoteImage = secretNoteImage;
        HoverText = hoverText;
    }

    /// <summary>
    /// Restores CollectionsPage.secretNoteImage and CollectionsPage.hoverText to their unmutated original values.
    /// Idempotent; safe to call multiple times or from both Postfix and Finalizer.
    /// </summary>
    public void Restore()
    {
        if (_restored)
            return;

        _restored = true;
        Instance.secretNoteImage = SecretNoteImage;
        HoverTextField?.SetValue(Instance, HoverText);
    }
}

/// <summary>
/// Harmony patch on CollectionsPage.draw to safely render secret note and journal scrap image previews
/// coordinated jointly with the hover title header.
/// Suppresses both hoverText and secretNoteImage during vanilla draw, then renders the header and preview
/// with safe-area clamping to guarantee header and image never overlap.
/// </summary>
[HarmonyPatch(typeof(CollectionsPage), nameof(CollectionsPage.draw), new[] { typeof(SpriteBatch) })]
public static class CollectionsPageDrawPatch
{
    private static bool _errorLogged;

    public static void Prefix(CollectionsPage __instance, ref string ___hoverText, out CollectionsNoteImageState? __state)
    {
        __state = null;
        try
        {
            // Only intercept discovered note/journal scrap image previews on the secret notes tab
            // when no letter viewer submenu is open.
            // Leave hidden "???", notes without images, or open letter submenus untouched.
            if (__instance.currentTab == CollectionsPage.secretNotesTab
                && __instance.letterviewerSubMenu == null
                && __instance.secretNoteImage != -1
                && !string.IsNullOrEmpty(___hoverText)
                && ___hoverText != "???")
            {
                __state = new CollectionsNoteImageState(__instance, __instance.secretNoteImage, ___hoverText);
                BilingualStringTracker.Register(___hoverText);
                // Suppress both fields so vanilla drawHoverText and fixed-offset textureBox do not render
                __instance.secretNoteImage = -1;
                ___hoverText = "";
            }
        }
        catch (Exception ex)
        {
            __state?.Restore();
            if (!_errorLogged)
            {
                _errorLogged = true;
                ModEntry.Instance?.Monitor.Log($"Error in CollectionsPage draw prefix: {ex.Message}", LogLevel.Warn);
            }
        }
    }

    public static void Postfix(CollectionsPage __instance, SpriteBatch b, ref string ___hoverText, CollectionsNoteImageState? __state)
    {
        try
        {
            if (__state != null && __state.SecretNoteImage != -1 && !string.IsNullOrEmpty(__state.HoverText) && __instance.secretNoteImageTexture != null && b != null)
            {
                using (BilingualRenderScope.Begin())
                {
                    CollectionsNoteImageLayoutResult layout = CollectionsNoteImageLayoutHelper.CalculateLayout(
                        Game1.getOldMouseX(),
                        Game1.getOldMouseY(),
                        __state.HoverText,
                        s => Game1.smallFont != null ? Game1.smallFont.MeasureString(s) : Vector2.Zero,
                        Utility.getSafeArea()
                    );

                    // 1. Render the hover tooltip header via native drawHoverText with explicit override coordinates
                    IClickableMenu.drawHoverText(
                        b,
                        __state.HoverText,
                        Game1.smallFont,
                        xOffset: 0,
                        yOffset: 0,
                        moneyAmountToDisplayAtBottom: -1,
                        boldTitleText: null,
                        healAmountToDisplay: -1,
                        buffIconsToDisplay: null,
                        hoveredItem: null,
                        currencySymbol: 0,
                        extraItemToShowIndex: null,
                        extraItemToShowAmount: -1,
                        overrideX: layout.HeaderX,
                        overrideY: layout.HeaderY
                    );

                    // 2. Render coordinated image preview texture box
                    IClickableMenu.drawTextureBox(b, layout.BoxX, layout.BoxY, layout.BoxWidth, layout.BoxHeight, Color.White);

                    // 3. Render note preview image
                    Texture2D texture = __instance.secretNoteImageTexture;
                    int image = __state.SecretNoteImage;
                    Rectangle sourceRect = new Rectangle(
                        image * 64 % texture.Width,
                        image * 64 / texture.Width * 64,
                        64,
                        64
                    );

                    b.Draw(
                        texture,
                        new Vector2(layout.ImageX, layout.ImageY),
                        sourceRect,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        layout.ImageScale,
                        SpriteEffects.None,
                        0.865f
                    );
                }
            }
        }
        catch (Exception ex)
        {
            if (!_errorLogged)
            {
                _errorLogged = true;
                ModEntry.Instance?.Monitor.Log($"Error in CollectionsPage draw postfix: {ex.Message}", LogLevel.Warn);
            }
        }
        finally
        {
            if (__state != null)
            {
                ___hoverText = __state.HoverText;
                __state.Restore();
            }
        }
    }

    public static Exception? Finalizer(CollectionsPage __instance, CollectionsNoteImageState? __state, Exception? __exception)
    {
        try
        {
            __state?.Restore();
        }
        catch
        {
            // Do not mask original exception
        }
        return __exception;
    }
}
