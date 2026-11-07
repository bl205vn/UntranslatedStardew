using StardewValley;
using StardewValley.BellsAndWhistles;

namespace UntranslatedStardew.Core;

/// <summary>Measures primary dialogue using vanilla draw rules, independently of bilingual layout.</summary>
internal static class PrimaryDialogueMetrics
{
    /// <summary>
    /// Measures the exact rendered height of primary text matching SpriteText.drawString.
    /// BMFont wraps at width - 4 (SpriteText line 701), whereas native Latin wraps at width
    /// with space-skipping and forward-neighbor character offsets.
    /// </summary>
    internal static int Measure(string text, int width)
    {
        if (string.IsNullOrEmpty(text) || width <= 0)
            return 0;

        if (IsUsingNonSpriteSheetFont() && !SpriteText.forceEnglishFont)
            return SpriteText.getHeightOfString(text, width - 4);

        return MeasureNativeLatinHeight(text, width);
    }

    private static bool IsUsingNonSpriteSheetFont()
    {
        if (!LocalizedContentManager.CurrentLanguageLatin)
        {
            if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru)
                return Game1.options?.useAlternateFont ?? false;
            return true;
        }
        return false;
    }

    internal static int MeasureNativeLatinHeight(string s, int width)
    {
        if (string.IsNullOrEmpty(s) || width <= 0)
            return 0;

        s = s.Replace(Environment.NewLine, "");
        s = s.Replace('♡', '<');
        float y = 0f;
        float x = 0f;
        int accumulated = 0;
        float zoom = SpriteText.FontPixelZoom;

        for (int i = 0; i < s.Length; i++)
        {
            float tempzoom = zoom;
            if (DialogueNativeFont.IsIconCharacter(s[i]) || SpriteText.forceEnglishFont)
            {
                zoom = 3f;
            }

            if (s[i] == '^')
            {
                y += 18f * zoom;
                x = 0f;
                accumulated = 0;
                zoom = tempzoom;
                continue;
            }

            accumulated = 0;
            if (SpriteText.positionOfNextSpace(s, i, (int)x, accumulated) >= width)
            {
                y += 18f * zoom;
                accumulated = 0;
                x = 0f;
                if (s[i] == ' ')
                {
                    zoom = tempzoom;
                    continue;
                }
            }

            if (i < s.Length - 1)
            {
                x += 8f * zoom + accumulated + SpriteText.getWidthOffsetForChar(s[i + 1]) * zoom;
            }

            if (s[i] != '^')
            {
                x += SpriteText.getWidthOffsetForChar(s[i]) * zoom;
            }

            zoom = tempzoom;
        }

        return (int)(y + 16f * zoom);
    }

}
