using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace UntranslatedStardew.Core;

/// <summary>
/// Measures and draws bilingual text runs using exact run-font metrics and caller transforms.
/// Completely recursion-guarded, thread-safe, and scoped to active bilingual surfaces.
/// </summary>
public static class BilingualFontRenderer
{
    private static readonly ThreadLocal<bool> IsRendering = new(() => false);
    private static readonly ThreadLocal<bool> IsMeasuring = new(() => false);

    public static bool IsRenderingActive => IsRendering.Value;
    public static bool IsMeasuringActive
    {
        get => IsMeasuring.Value;
        set => IsMeasuring.Value = value;
    }

    /// <summary>
    /// Attempts to measure string using run-based metrics when inside an active bilingual scope.
    /// Returns true if intercepted and measured; false if caller should use native MonoGame measure.
    /// </summary>
    public static bool TryMeasureString(SpriteFont font, string? text, out Vector2 size)
    {
        size = Vector2.Zero;
        if (!BilingualRenderScope.IsActive || IsMeasuring.Value || string.IsNullOrEmpty(text) || font == null)
            return false;

        bool wasMeasuring = IsMeasuring.Value;
        IsMeasuring.Value = true;
        try
        {
            BilingualFontManager? manager = BilingualFontManager.Instance;
            if (manager == null)
                return false;

            SpriteFont? secondaryFont = manager.GetSecondaryFont(font);
            TextRunPlan? plan = BilingualRunPlanner.Plan(text, font, secondaryFont, manager);
            if (plan == null)
                return false;

            size = plan.TotalSize;
            return true;
        }
        finally
        {
            IsMeasuring.Value = wasMeasuring;
        }
    }

    /// <summary>
    /// Attempts to measure StringBuilder using run-based metrics when inside an active bilingual scope.
    /// </summary>
    public static bool TryMeasureString(SpriteFont font, StringBuilder? text, out Vector2 size)
    {
        size = Vector2.Zero;
        if (!BilingualRenderScope.IsActive || IsMeasuring.Value || text == null || text.Length == 0 || font == null)
            return false;

        return TryMeasureString(font, text.ToString(), out size);
    }

    /// <summary>
    /// Intercepts SpriteBatch.DrawString overloads and renders runs with matching fonts and transforms.
    /// Preserves global block flipping across SpriteEffects, origin, rotation, and nullable scale.
    /// </summary>
    public static bool TryDrawString(
        SpriteBatch b,
        SpriteFont font,
        string? text,
        Vector2 position,
        Color color,
        float rotation = 0f,
        Vector2 origin = default,
        Vector2? scale = null,
        SpriteEffects effects = SpriteEffects.None,
        float layerDepth = 0f)
    {
        if (!BilingualRenderScope.IsActive || IsRendering.Value || string.IsNullOrEmpty(text) || font == null || b == null)
            return false;

        Vector2 effectiveScale = scale ?? Vector2.One;

        BilingualFontManager? manager = BilingualFontManager.Instance;
        if (manager == null)
            return false;

        TextRunPlan? plan;
        bool wasMeasuring = IsMeasuring.Value;
        IsMeasuring.Value = true;
        try
        {
            SpriteFont? secondaryFont = manager.GetSecondaryFont(font);
            plan = BilingualRunPlanner.Plan(text, font, secondaryFont, manager);
        }
        finally
        {
            IsMeasuring.Value = wasMeasuring;
        }

        if (plan == null)
            return false;

        try
        {
            IsRendering.Value = true;
            foreach (TextRun run in plan.Runs)
            {
                // Reflect run offsets under horizontal/vertical SpriteEffects to preserve total block flipping
                float effectiveOffsetX = run.Offset.X;
                float effectiveOffsetY = run.Offset.Y;

                if ((effects & SpriteEffects.FlipHorizontally) != 0)
                {
                    effectiveOffsetX = plan.TotalSize.X - run.Offset.X - run.Size.X;
                }
                if ((effects & SpriteEffects.FlipVertically) != 0)
                {
                    effectiveOffsetY = plan.TotalSize.Y - run.Offset.Y - run.Size.Y;
                }

                Vector2 effectiveOffset = new Vector2(effectiveOffsetX, effectiveOffsetY);
                Vector2 runOrigin = origin - effectiveOffset;

                b.DrawString(
                    run.Font,
                    run.Text,
                    position,
                    color,
                    rotation,
                    runOrigin,
                    effectiveScale,
                    effects,
                    layerDepth
                );
            }
            return true;
        }
        finally
        {
            IsRendering.Value = false;
        }
    }

    public static bool TryDrawString(
        SpriteBatch b,
        SpriteFont font,
        StringBuilder? text,
        Vector2 position,
        Color color,
        float rotation = 0f,
        Vector2 origin = default,
        Vector2? scale = null,
        SpriteEffects effects = SpriteEffects.None,
        float layerDepth = 0f)
    {
        if (!BilingualRenderScope.IsActive || IsRendering.Value || text == null || text.Length == 0 || font == null || b == null)
            return false;

        return TryDrawString(b, font, text.ToString(), position, color, rotation, origin, scale, effects, layerDepth);
    }
}
