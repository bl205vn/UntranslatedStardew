using System;
using System.Text;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>
/// Harmony patches intercepting SpriteFont.MeasureString and SpriteBatch.DrawString overloads.
/// Strictly scoped to active bilingual tooltips and menu surfaces.
/// Unscoped calls execute native MonoGame logic byte-for-byte unchanged.
/// </summary>
public static class FontInterceptionPatches
{
    [HarmonyPatch(typeof(SpriteFont), nameof(SpriteFont.MeasureString), new[] { typeof(string) })]
    public static class SpriteFontMeasureStringPatch
    {
        public static bool Prefix(SpriteFont __instance, string? text, ref Vector2 __result)
        {
            if (BilingualFontRenderer.TryMeasureString(__instance, text, out Vector2 measured))
            {
                __result = measured;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SpriteFont), nameof(SpriteFont.MeasureString), new[] { typeof(StringBuilder) })]
    public static class SpriteFontMeasureStringBuilderPatch
    {
        public static bool Prefix(SpriteFont __instance, StringBuilder? text, ref Vector2 __result)
        {
            if (BilingualFontRenderer.TryMeasureString(__instance, text, out Vector2 measured))
            {
                __result = measured;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.DrawString), new[] { typeof(SpriteFont), typeof(string), typeof(Vector2), typeof(Color) })]
    public static class SpriteBatchDrawStringColorPatch
    {
        public static bool Prefix(SpriteBatch __instance, SpriteFont spriteFont, string? text, Vector2 position, Color color)
        {
            if (BilingualFontRenderer.TryDrawString(__instance, spriteFont, text, position, color))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.DrawString), new[] { typeof(SpriteFont), typeof(string), typeof(Vector2), typeof(Color), typeof(float), typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float) })]
    public static class SpriteBatchDrawStringFloatScalePatch
    {
        public static bool Prefix(SpriteBatch __instance, SpriteFont spriteFont, string? text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
        {
            if (BilingualFontRenderer.TryDrawString(__instance, spriteFont, text, position, color, rotation, origin, new Vector2(scale, scale), effects, layerDepth))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.DrawString), new[] { typeof(SpriteFont), typeof(string), typeof(Vector2), typeof(Color), typeof(float), typeof(Vector2), typeof(Vector2), typeof(SpriteEffects), typeof(float) })]
    public static class SpriteBatchDrawStringVectorScalePatch
    {
        public static bool Prefix(SpriteBatch __instance, SpriteFont spriteFont, string? text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
        {
            if (BilingualFontRenderer.TryDrawString(__instance, spriteFont, text, position, color, rotation, origin, scale, effects, layerDepth))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.DrawString), new[] { typeof(SpriteFont), typeof(StringBuilder), typeof(Vector2), typeof(Color) })]
    public static class SpriteBatchDrawStringBuilderColorPatch
    {
        public static bool Prefix(SpriteBatch __instance, SpriteFont spriteFont, StringBuilder? text, Vector2 position, Color color)
        {
            if (BilingualFontRenderer.TryDrawString(__instance, spriteFont, text, position, color))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.DrawString), new[] { typeof(SpriteFont), typeof(StringBuilder), typeof(Vector2), typeof(Color), typeof(float), typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float) })]
    public static class SpriteBatchDrawStringBuilderFloatScalePatch
    {
        public static bool Prefix(SpriteBatch __instance, SpriteFont spriteFont, StringBuilder? text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
        {
            if (BilingualFontRenderer.TryDrawString(__instance, spriteFont, text, position, color, rotation, origin, new Vector2(scale, scale), effects, layerDepth))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.DrawString), new[] { typeof(SpriteFont), typeof(StringBuilder), typeof(Vector2), typeof(Color), typeof(float), typeof(Vector2), typeof(Vector2), typeof(SpriteEffects), typeof(float) })]
    public static class SpriteBatchDrawStringBuilderVectorScalePatch
    {
        public static bool Prefix(SpriteBatch __instance, SpriteFont spriteFont, StringBuilder? text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
        {
            if (BilingualFontRenderer.TryDrawString(__instance, spriteFont, text, position, color, rotation, origin, scale, effects, layerDepth))
            {
                return false;
            }
            return true;
        }
    }
}
