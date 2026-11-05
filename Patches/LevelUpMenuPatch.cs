using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>
/// Scoped state holding original title values and subtitle layout for LevelUpMenu.draw.
/// Guarantees that internal menu lists are never permanently mutated across frames.
/// </summary>
public sealed class LevelUpMenuState
{
    public List<string>? LeftList { get; set; }
    public List<string>? RightList { get; set; }
    public string? OriginalLeft { get; set; }
    public string? OriginalRight { get; set; }
    public string? SubtitleLeft { get; set; }
    public string? SubtitleRight { get; set; }
    public bool LeftSwapped { get; set; }
    public bool RightSwapped { get; set; }
    public BilingualRenderScope.ScopeCookie? ScopeCookie { get; set; }
    private bool _restored;

    /// <summary>
    /// Restores left and right profession descriptions to their unmutated original state.
    /// Idempotent; safe to call multiple times or from both Postfix and Finalizer.
    /// </summary>
    public void Restore(LevelUpMenu? instance = null)
    {
        if (_restored)
            return;

        _restored = true;

        ScopeCookie?.Dispose();
        ScopeCookie = null;

        if (LeftSwapped && OriginalLeft != null && LeftList?.Count > 0)
        {
            LeftList[0] = OriginalLeft;
        }

        if (RightSwapped && OriginalRight != null && RightList?.Count > 0)
        {
            RightList[0] = OriginalRight;
        }
    }

    /// <summary>
    /// Renders secondary profession subtitles using Game1.smallFont inside the measured vertical gap
    /// between title (top+160) and first bullet (top+264), dynamically measuring actual font heights
    /// and applying bounded scaling/fit to prevent overlap with dynamic or enlarged fonts.
    /// </summary>
    public void DrawSubtitles(LevelUpMenu instance, SpriteBatch b, Color leftColor, Color rightColor)
    {
        if (instance == null || b == null)
            return;

        using (BilingualRenderScope.Begin())
        {
            float leftTitleH = 48f;
            float rightTitleH = 48f;
            float leftSubH = 24f;
            float rightSubH = 24f;

        if (Game1.dialogueFont != null)
        {
            if (LeftList != null && LeftList.Count > 0 && !string.IsNullOrEmpty(LeftList[0]))
            {
                leftTitleH = Game1.dialogueFont.MeasureString(LeftList[0]).Y;
            }
            if (RightList != null && RightList.Count > 0 && !string.IsNullOrEmpty(RightList[0]))
            {
                rightTitleH = Game1.dialogueFont.MeasureString(RightList[0]).Y;
            }
        }

        if (Game1.smallFont != null)
        {
            if (!string.IsNullOrEmpty(SubtitleLeft))
            {
                leftSubH = Game1.smallFont.MeasureString(SubtitleLeft).Y;
            }
            if (!string.IsNullOrEmpty(SubtitleRight))
            {
                rightSubH = Game1.smallFont.MeasureString(SubtitleRight).Y;
            }
        }

        LevelUpLayoutResult layout = LevelUpLayoutHelper.CalculateLayout(
            instance.xPositionOnScreen,
            instance.yPositionOnScreen,
            instance.width,
            IClickableMenu.spaceToClearSideBorder,
            IClickableMenu.spaceToClearTopBorder,
            leftTitleH,
            leftSubH,
            rightTitleH,
            rightSubH
        );

        if (layout.LeftVisible && !string.IsNullOrEmpty(SubtitleLeft) && Game1.smallFont != null)
        {
            float maxLeftWidth = layout.LeftScale > 0f ? layout.LeftMaxWidth / layout.LeftScale : layout.LeftMaxWidth;
            string leftText = LevelUpLayoutHelper.TruncateToWidth(
                SubtitleLeft,
                maxLeftWidth,
                s => Game1.smallFont.MeasureString(s).X
            );
            b.DrawString(
                Game1.smallFont,
                leftText,
                new Vector2(layout.LeftSubtitleX, layout.LeftSubtitleY),
                leftColor,
                0f,
                Vector2.Zero,
                layout.LeftScale,
                SpriteEffects.None,
                1f
            );
        }

        if (layout.RightVisible && !string.IsNullOrEmpty(SubtitleRight) && Game1.smallFont != null)
        {
            float maxRightWidth = layout.RightScale > 0f ? layout.RightMaxWidth / layout.RightScale : layout.RightMaxWidth;
            string rightText = LevelUpLayoutHelper.TruncateToWidth(
                SubtitleRight,
                maxRightWidth,
                s => Game1.smallFont.MeasureString(s).X
            );
            b.DrawString(
                Game1.smallFont,
                rightText,
                new Vector2(layout.RightSubtitleX, layout.RightSubtitleY),
                rightColor,
                0f,
                Vector2.Zero,
                layout.RightScale,
                SpriteEffects.None,
                1f
            );
        }
        }
    }
}

/// <summary>
/// Harmony patch on LevelUpMenu.draw to display bilingual profession titles during level 5 and 10 choices.
/// Uses scoped display rendering with state save/restore in Postfix and Finalizer, completely preventing
/// permanent field mutation, stale config persistence, or text collisions with bullet items.
/// </summary>
[HarmonyPatch(typeof(LevelUpMenu), nameof(LevelUpMenu.draw), new[] { typeof(SpriteBatch) })]
public static class LevelUpMenuPatch
{
    private static bool _errorLogged;

    public static void Prefix(
        LevelUpMenu __instance,
        List<string> ___leftProfessionDescription,
        List<string> ___rightProfessionDescription,
        List<int> ___professionsToChoose,
        out LevelUpMenuState? __state)
    {
        __state = null;
        try
        {
            if (ModEntry.Instance?.Config.ShowSecondaryInTooltip != true || ModEntry.Instance.Resolver == null)
                return;

            if (!__instance.isProfessionChooser)
                return;

            if (___professionsToChoose == null || ___professionsToChoose.Count < 2)
                return;

            ItemNameResolver resolver = ModEntry.Instance.Resolver;
            bool secondaryFirst = ModEntry.Instance.Config.SecondaryFirst;

            var state = new LevelUpMenuState
            {
                LeftList = ___leftProfessionDescription,
                RightList = ___rightProfessionDescription
            };
            // Assign state immediately so Postfix/Finalizer or catch handler can restore if Prefix throws midway
            __state = state;

            // 1. Process left profession
            if (___leftProfessionDescription != null && ___leftProfessionDescription.Count > 0)
            {
                string primaryLeft = ___leftProfessionDescription[0];
                string? secLeft = resolver.GetSecondaryProfessionTitle(___professionsToChoose[0]);
                state.OriginalLeft = primaryLeft;

                if (!string.IsNullOrWhiteSpace(secLeft) && !string.Equals(primaryLeft, secLeft, StringComparison.OrdinalIgnoreCase))
                {
                    if (secondaryFirst)
                    {
                        // Swap for draw: secondary is rendered as main title in dialogueFont
                        ___leftProfessionDescription[0] = secLeft;
                        state.LeftSwapped = true;
                        // Primary is rendered as subtitle in smallFont
                        state.SubtitleLeft = primaryLeft;
                    }
                    else
                    {
                        // Primary remains in main title (dialogueFont)
                        // Secondary is rendered as subtitle in smallFont
                        state.SubtitleLeft = secLeft;
                    }
                }
            }

            // 2. Process right profession
            if (___rightProfessionDescription != null && ___rightProfessionDescription.Count > 0)
            {
                string primaryRight = ___rightProfessionDescription[0];
                string? secRight = resolver.GetSecondaryProfessionTitle(___professionsToChoose[1]);
                state.OriginalRight = primaryRight;

                if (!string.IsNullOrWhiteSpace(secRight) && !string.Equals(primaryRight, secRight, StringComparison.OrdinalIgnoreCase))
                {
                    if (secondaryFirst)
                    {
                        ___rightProfessionDescription[0] = secRight;
                        state.RightSwapped = true;
                        state.SubtitleRight = primaryRight;
                    }
                    else
                    {
                        state.SubtitleRight = secRight;
                    }
                }
            }

            if (!string.IsNullOrEmpty(state.SubtitleLeft)) BilingualStringTracker.Register(state.SubtitleLeft);
            if (!string.IsNullOrEmpty(state.SubtitleRight)) BilingualStringTracker.Register(state.SubtitleRight);
            if (state.LeftSwapped && ___leftProfessionDescription?.Count > 0) BilingualStringTracker.Register(___leftProfessionDescription[0]);
            if (state.RightSwapped && ___rightProfessionDescription?.Count > 0) BilingualStringTracker.Register(___rightProfessionDescription[0]);

            if (state.SubtitleLeft != null || state.SubtitleRight != null || state.LeftSwapped || state.RightSwapped)
            {
                state.ScopeCookie = BilingualRenderScope.Begin();
            }
        }
        catch (Exception ex)
        {
            // If prefix failed after mutating profession lists, immediately restore unmutated state
            __state?.Restore(__instance);
            if (!_errorLogged)
            {
                _errorLogged = true;
                ModEntry.Instance?.Monitor.Log($"Error in LevelUpMenu draw prefix: {ex.Message}", LogLevel.Warn);
            }
        }
    }

    public static void Postfix(
        LevelUpMenu __instance,
        SpriteBatch b,
        Color ___leftProfessionColor,
        Color ___rightProfessionColor,
        LevelUpMenuState? __state)
    {
        try
        {
            if (__state != null && b != null)
            {
                __state.DrawSubtitles(__instance, b, ___leftProfessionColor, ___rightProfessionColor);
            }
        }
        catch (Exception ex)
        {
            if (!_errorLogged)
            {
                _errorLogged = true;
                ModEntry.Instance?.Monitor.Log($"Error in LevelUpMenu draw postfix: {ex.Message}", LogLevel.Warn);
            }
        }
        finally
        {
            __state?.Restore(__instance);
        }
    }

    public static Exception? Finalizer(
        LevelUpMenu __instance,
        LevelUpMenuState? __state,
        Exception? __exception)
    {
        try
        {
            __state?.Restore(__instance);
        }
        catch
        {
            // Do not mask original exception
        }
        return __exception;
    }
}
