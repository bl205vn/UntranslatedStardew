using System;
using Microsoft.Xna.Framework;

namespace UntranslatedStardew.Core;

/// <summary>
/// Layout calculation result for CollectionsPage secret note / journal scrap image preview and hover tooltip header.
/// </summary>
public readonly struct CollectionsNoteImageLayoutResult
{
    public int HeaderX { get; init; }
    public int HeaderY { get; init; }
    public int HeaderWidth { get; init; }
    public int HeaderHeight { get; init; }
    public int BoxX { get; init; }
    public int BoxY { get; init; }
    public int BoxWidth { get; init; }
    public int BoxHeight { get; init; }
    public int ImageX { get; init; }
    public int ImageY { get; init; }
    public int ImageWidth { get; init; }
    public int ImageHeight { get; init; }
    public float ImageScale { get; init; }
    public int TooltipHeight { get; init; }
    public int ExtraHeight { get; init; }
    public int DeltaY { get; init; }
    public bool IsSideBySide { get; init; }
}

/// <summary>
/// Pure layout geometry calculations and safe-area clamping helpers for secret note and journal scrap
/// image previews in CollectionsPage.
/// Derived strictly from decompiled Stardew Valley 1.6.15 source coordinates and IClickableMenu.drawHoverText.
/// Coordinates header tooltip and image preview together as a disjoint group to prevent overlap at screen edges.
/// </summary>
public static class CollectionsNoteImageLayoutHelper
{
    public const int BoxWidth = 288;
    public const int BoxHeight = 288;
    public const int ImageWidth = 256;
    public const int ImageHeight = 256;
    public const int ImagePadding = 16;
    public const int VanillaBaselineTooltipHeight = 64;
    public const int VanillaBaselineBoxYOffset = 96;
    public const int VanillaBaselineImageYOffset = 112;

    /// <summary>
    /// Calculates coordinated screen coordinates for both the hover title header and the note image preview box/image.
    /// Clamps the entire group jointly inside safe area bounds so header bottom &lt;= box top, preventing any overlap.
    /// Preserves exact vanilla geometry when unclamped and single-line.
    /// </summary>
    public static CollectionsNoteImageLayoutResult CalculateLayout(
        int mouseX,
        int mouseY,
        string? hoverText,
        Func<string, Vector2>? measureString,
        Rectangle safeArea)
    {
        float textWidth = 0f;
        float textHeight = 0f;
        if (!string.IsNullOrEmpty(hoverText) && measureString != null)
        {
            Vector2 sz = measureString(hoverText);
            textWidth = sz.X;
            textHeight = sz.Y;
        }

        // Match vanilla IClickableMenu.drawHoverText dimensions:
        // width = max(font.MeasureString(text).X) + 32; boxWidth = width + 4 = textWidth + 36
        // height = Math.Max(20 * 3, (int)font.MeasureString(text).Y + 32)
        int headerWidth = (int)Math.Ceiling(textWidth) + 36;
        int headerHeight = Math.Max(60, (int)Math.Ceiling(textHeight) + 32);

        int extraHeight = Math.Max(0, headerHeight - VanillaBaselineTooltipHeight);

        // Vanilla unconstrained baseline positions:
        // Tooltip is at (mouseX + 32, mouseY + 32)
        // Image box is at (mouseX, mouseY + 96 + extraHeight)
        int headerX = mouseX + 32;
        int headerY = mouseY + 32;
        int boxX = mouseX;
        int boxY = Math.Max(mouseY + VanillaBaselineBoxYOffset, headerY + headerHeight);

        int boxW = BoxWidth;
        int boxH = BoxHeight;
        float imageScale = 4f;
        bool isSideBySide = false;

        bool hasSafeArea = safeArea.Width > 0 && safeArea.Height > 0;

        if (hasSafeArea)
        {
            int totalGroupH = headerHeight + boxH;

            // Check if group fits vertically within safeArea.Height
            if (totalGroupH <= safeArea.Height)
            {
                // Normal stacked placement: clamp vertical position jointly
                int groupBottom = boxY + boxH;
                if (groupBottom > safeArea.Bottom)
                {
                    int shiftY = groupBottom - safeArea.Bottom;
                    headerY -= shiftY;
                    boxY -= shiftY;
                }

                if (headerY < safeArea.Top)
                {
                    int shiftTop = safeArea.Top - headerY;
                    headerY += shiftTop;
                    boxY += shiftTop;
                }
            }
            else
            {
                // Whole group taller than safe area:
                // Check if side-by-side placement fits without overlapping
                int totalGroupW = headerWidth + boxW + 8;
                if (totalGroupW <= safeArea.Width && Math.Max(headerHeight, boxH) <= safeArea.Height)
                {
                    isSideBySide = true;
                    headerY = Math.Clamp(mouseY, safeArea.Top, safeArea.Bottom - headerHeight);
                    boxY = Math.Clamp(mouseY, safeArea.Top, safeArea.Bottom - boxH);

                    // Place image box to the right or left of header depending on mouse X
                    if (mouseX + headerWidth + boxW + 8 <= safeArea.Right)
                    {
                        headerX = mouseX;
                        boxX = headerX + headerWidth + 8;
                    }
                    else
                    {
                        boxX = Math.Max(safeArea.Left, safeArea.Right - boxW);
                        headerX = Math.Max(safeArea.Left, boxX - headerWidth - 8);
                    }
                }
                else
                {
                    // Fallback: proportionally scale image down to fit available height
                    int availBoxH = Math.Max(96, safeArea.Height - headerHeight - 8);
                    float fitScale = (availBoxH - 32) / 64f;
                    imageScale = Math.Clamp(fitScale, 1.5f, 4f);
                    boxH = (int)Math.Ceiling(64f * imageScale + 32f);
                    boxW = boxH;

                    headerY = safeArea.Top;
                    boxY = headerY + headerHeight;

                    int shiftBottom = (boxY + boxH) - safeArea.Bottom;
                    if (shiftBottom > 0)
                    {
                        headerY = Math.Max(safeArea.Top, headerY - shiftBottom);
                        boxY = headerY + headerHeight;
                    }
                }
            }

            // Clamp horizontal position jointly
            if (!isSideBySide)
            {
                int groupRight = Math.Max(headerX + headerWidth, boxX + boxW);
                if (groupRight > safeArea.Right)
                {
                    int shiftX = groupRight - safeArea.Right;
                    headerX -= shiftX;
                    boxX -= shiftX;
                }

                int groupLeft = Math.Min(headerX, boxX);
                if (groupLeft < safeArea.Left)
                {
                    int shiftLeft = safeArea.Left - groupLeft;
                    headerX += shiftLeft;
                    boxX += shiftLeft;
                }
            }
        }

        int imgW = (int)Math.Round(64f * imageScale);
        int imgH = (int)Math.Round(64f * imageScale);
        int imageX = boxX + (boxW - imgW) / 2;
        int imageY = boxY + (boxH - imgH) / 2;

        return new CollectionsNoteImageLayoutResult
        {
            HeaderX = headerX,
            HeaderY = headerY,
            HeaderWidth = headerWidth,
            HeaderHeight = headerHeight,
            BoxX = boxX,
            BoxY = boxY,
            BoxWidth = boxW,
            BoxHeight = boxH,
            ImageX = imageX,
            ImageY = imageY,
            ImageWidth = imgW,
            ImageHeight = imgH,
            ImageScale = imageScale,
            TooltipHeight = headerHeight,
            ExtraHeight = extraHeight,
            DeltaY = extraHeight,
            IsSideBySide = isSideBySide
        };
    }
}
