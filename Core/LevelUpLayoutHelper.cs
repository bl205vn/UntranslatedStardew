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
    public float LeftScale { get; init; }
    public float RightScale { get; init; }
    public int LeftSubtitleY { get; init; }
    public int RightSubtitleY { get; init; }
    public bool LeftVisible { get; init; }
    public bool RightVisible { get; init; }
}

/// <summary>
/// Pure layout geometry calculations and text clipping helpers for LevelUpMenu profession chooser.
/// Derived strictly from decompiled Stardew Valley 1.6.15 source coordinates with dynamic-font-safe bounds.
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
    /// Calculates layout coordinates with default baseline vanilla font heights (dialogueFont: 48px, smallFont: 24px).
    /// Preserves exact backward-compatible coordinates and gaps.
    /// </summary>
    public static LevelUpLayoutResult CalculateLayout(
        int xPositionOnScreen,
        int yPositionOnScreen,
        int width,
        int spaceToClearSideBorder,
        int spaceToClearTopBorder)
    {
        return CalculateLayout(
            xPositionOnScreen,
            yPositionOnScreen,
            width,
            spaceToClearSideBorder,
            spaceToClearTopBorder,
            measuredTitleHeight: 48f,
            measuredSubtitleHeight: 24f
        );
    }

    /// <summary>
    /// Calculates layout coordinates using measured font heights uniform across both columns.
    /// </summary>
    public static LevelUpLayoutResult CalculateLayout(
        int xPositionOnScreen,
        int yPositionOnScreen,
        int width,
        int spaceToClearSideBorder,
        int spaceToClearTopBorder,
        float measuredTitleHeight,
        float measuredSubtitleHeight)
    {
        return CalculateLayout(
            xPositionOnScreen,
            yPositionOnScreen,
            width,
            spaceToClearSideBorder,
            spaceToClearTopBorder,
            leftTitleHeight: measuredTitleHeight,
            leftSubtitleHeight: measuredSubtitleHeight,
            rightTitleHeight: measuredTitleHeight,
            rightSubtitleHeight: measuredSubtitleHeight
        );
    }

    /// <summary>
    /// Calculates layout coordinates with per-column dynamic font measurements,
    /// avoiding subtitle overlap with main titles (dialogueFont) or first bullets (smallFont).
    /// If available space is too narrow for legible text (&lt; 0.5f scale or &lt; 12px available height),
    /// the overlay is safely omitted rather than causing guaranteed text collision.
    /// </summary>
    public static LevelUpLayoutResult CalculateLayout(
        int xPositionOnScreen,
        int yPositionOnScreen,
        int width,
        int spaceToClearSideBorder,
        int spaceToClearTopBorder,
        float leftTitleHeight,
        float leftSubtitleHeight,
        float rightTitleHeight,
        float rightSubtitleHeight)
    {
        int leftX = xPositionOnScreen + spaceToClearSideBorder + 32;
        int rightX = xPositionOnScreen + spaceToClearSideBorder + width / 2;
        int titleY = yPositionOnScreen + spaceToClearTopBorder + TitleYOffset;
        int defaultSubtitleY = yPositionOnScreen + spaceToClearTopBorder + SubtitleYOffset;
        int bullet1Y = yPositionOnScreen + spaceToClearTopBorder + Bullet1YOffset;

        float leftMaxWidth = Math.Max(100f, width / 2f - 144f);
        float rightMaxWidth = Math.Max(100f, width / 2f - 128f);

        var (leftSubY, leftScale, leftVis) = CalculateColumnSubtitle(titleY, bullet1Y, defaultSubtitleY, leftTitleHeight, leftSubtitleHeight);
        var (rightSubY, rightScale, rightVis) = CalculateColumnSubtitle(titleY, bullet1Y, defaultSubtitleY, rightTitleHeight, rightSubtitleHeight);

        return new LevelUpLayoutResult
        {
            LeftTitleX = leftX,
            RightTitleX = rightX,
            TitleY = titleY,
            LeftSubtitleX = leftX,
            RightSubtitleX = rightX,
            SubtitleY = leftSubY,
            LeftSubtitleY = leftSubY,
            RightSubtitleY = rightSubY,
            Bullet1Y = bullet1Y,
            TotalTitleGap = bullet1Y - titleY,
            SubtitleToBulletGap = bullet1Y - leftSubY,
            LeftMaxWidth = leftMaxWidth,
            RightMaxWidth = rightMaxWidth,
            LeftScale = leftScale,
            RightScale = rightScale,
            LeftVisible = leftVis,
            RightVisible = rightVis
        };
    }

    private static (int SubtitleY, float Scale, bool IsVisible) CalculateColumnSubtitle(
        int titleY,
        int bullet1Y,
        int defaultSubtitleY,
        float titleHeight,
        float subtitleHeight)
    {
        // Subtitle must never start above the measured bottom of the main title
        int minSubtitleY = (int)Math.Ceiling(titleY + Math.Max(0f, titleHeight));
        int subtitleY = Math.Max(defaultSubtitleY, minSubtitleY);

        int availableGap = bullet1Y - subtitleY;

        // Explicit readability limit: text scaled below 0.5f or gap < 12px cannot be displayed legibly
        const float minReadableScale = 0.5f;
        const int minReadableGap = 12;

        if (availableGap < minReadableGap)
        {
            // Truly impossible space: main title fills the entire gap down to bullet 1.
            // Omit subtitle overlay to avoid guaranteed bullet collision.
            return (subtitleY, 0f, false);
        }

        float scale = 1f;
        if (subtitleHeight > 0f)
        {
            if (subtitleHeight > availableGap)
            {
                float fitScale = (float)availableGap / subtitleHeight;
                if (fitScale < minReadableScale)
                {
                    // Scale would be unreadable; skip overlay in impossible space
                    return (subtitleY, 0f, false);
                }
                scale = fitScale;
            }
        }

        return (subtitleY, scale, true);
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
