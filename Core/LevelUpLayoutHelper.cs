using System;

namespace UntranslatedStardew.Core;

/// <summary>
/// Layout calculation result for LevelUpMenu profession chooser.
/// </summary>
public readonly struct LevelUpLayoutResult
{
    public int LeftTitleX { get; init; }
    public int RightTitleX { get; init; }
    public int TitleY { get; init; }
    public int LeftSubtitleX { get; init; }
    public int RightSubtitleX { get; init; }
    public int SubtitleY { get; init; }
    public int Bullet1Y { get; init; }
    public int TotalTitleGap { get; init; }
    public int SubtitleToBulletGap { get; init; }
    public float LeftMaxWidth { get; init; }
    public float RightMaxWidth { get; init; }
}

/// <summary>
/// Pure layout geometry calculations and text clipping helpers for LevelUpMenu profession chooser.
/// Derived strictly from decompiled Stardew Valley 1.6.15 source coordinates.
/// </summary>
public static class LevelUpLayoutHelper
{
    // Vanilla LevelUpMenu.cs constants:
    // Title drawn at: top + 160 (dialogueFont)
    // First bullet drawn at: top + 128 + 8 + 64 * 2 = top + 264 (smallFont)
    // Left icon drawn at: top + 160 - 16 = top + 144 to top + 208 (16*4 = 64px)
    public const int TitleYOffset = 160;
    public const int SubtitleYOffset = 208;
    public const int Bullet1YOffset = 264;

    /// <summary>
    /// Calculates the exact screen coordinates and bounded widths for profession titles and subtitles.
    /// </summary>
    public static LevelUpLayoutResult CalculateLayout(
        int xPositionOnScreen,
        int yPositionOnScreen,
        int width,
        int spaceToClearSideBorder,
        int spaceToClearTopBorder)
    {
        int leftX = xPositionOnScreen + spaceToClearSideBorder + 32;
        int rightX = xPositionOnScreen + spaceToClearSideBorder + width / 2;
        int titleY = yPositionOnScreen + spaceToClearTopBorder + TitleYOffset;
        int subtitleY = yPositionOnScreen + spaceToClearTopBorder + SubtitleYOffset;
        int bullet1Y = yPositionOnScreen + spaceToClearTopBorder + Bullet1YOffset;

        // Bounded max width per column before icon / bullet boundaries
        // Left column icon: width / 2 - 112 -> margin before icon is (width/2 - 112) - 32 = width/2 - 144
        // Right column icon: width - 128 -> margin before icon is (width - 128) - (width/2) = width/2 - 128
        float leftMaxWidth = Math.Max(100f, width / 2f - 144f);
        float rightMaxWidth = Math.Max(100f, width / 2f - 128f);

        return new LevelUpLayoutResult
        {
            LeftTitleX = leftX,
            RightTitleX = rightX,
            TitleY = titleY,
            LeftSubtitleX = leftX,
            RightSubtitleX = rightX,
            SubtitleY = subtitleY,
            Bullet1Y = bullet1Y,
            TotalTitleGap = bullet1Y - titleY,
            SubtitleToBulletGap = bullet1Y - subtitleY,
            LeftMaxWidth = leftMaxWidth,
            RightMaxWidth = rightMaxWidth
        };
    }

    /// <summary>
    /// Pure text truncation helper that clamps text to a maximum width using a measurement delegate.
    /// Appends ellipsis "..." when text overflows, ensuring no unbounded visual overflow.
    /// </summary>
    public static string TruncateToWidth(string text, float maxWidth, Func<string, float> measureString)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f)
            return "";

        if (measureString(text) <= maxWidth)
            return text;

        const string ellipsis = "...";
        float ellipsisWidth = measureString(ellipsis);
        if (ellipsisWidth >= maxWidth)
            return ellipsis;

        int low = 1;
        int high = text.Length - 1;
        int bestLen = 0;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            string candidate = text.Substring(0, mid) + ellipsis;
            if (measureString(candidate) <= maxWidth)
            {
                bestLen = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return bestLen > 0 ? text.Substring(0, bestLen) + ellipsis : ellipsis;
    }
}
