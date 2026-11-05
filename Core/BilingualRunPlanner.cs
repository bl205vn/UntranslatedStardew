using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace UntranslatedStardew.Core;

/// <summary>
/// Plans font run segmentation and exact geometric metrics for bilingual text blocks.
/// Prefers whole text segments or lines from the same font to ensure consistent baselines,
/// normalizes text to Unicode Form C, and tracks line-aware maximum heights.
/// </summary>
public static class BilingualRunPlanner
{
    private const int MaxCacheEntries = 512;
    private static readonly ConcurrentDictionary<string, TextRunPlan?> Cache = new(StringComparer.Ordinal);

    /// <summary>
    /// Clears the cached text run plans.
    /// </summary>
    public static void ClearCache()
    {
        Cache.Clear();
    }

    /// <summary>
    /// Plans runs for the given text. Returns null if all characters exist in the primary font
    /// or if no fallback font is available.
    /// </summary>
    public static TextRunPlan? Plan(
        string? text,
        SpriteFont primaryFont,
        SpriteFont? secondaryFont,
        BilingualFontManager? fontManager = null)
    {
        if (string.IsNullOrEmpty(text) || primaryFont == null)
            return null;

        fontManager ??= BilingualFontManager.Instance;

        // 1. Normalize input to Unicode Form C (precomposed accents for MonoGame)
        string normalized = text.Normalize(NormalizationForm.FormC);
        bool isDecomposed = !string.Equals(text, normalized, StringComparison.Ordinal);

        // 2. Check if primary font covers all non-control characters (fast path)
        bool needsFallback = false;
        for (int i = 0; i < normalized.Length; i++)
        {
            char c = normalized[i];
            if (c == '\r' || c == '\n' || c == '\t' || c == ' ')
                continue;

            if (fontManager != null)
            {
                if (!fontManager.ContainsCharacter(primaryFont, c))
                {
                    needsFallback = true;
                    break;
                }
            }
            else
            {
                if (!primaryFont.Characters.Contains(c))
                {
                    needsFallback = true;
                    break;
                }
            }
        }

        if (!needsFallback)
        {
            if (!isDecomposed)
                return null;

            // Input was decomposed (NFD) but precomposed (NFC) characters are all supported by primary font.
            // We must return a normalized plan using primaryFont so renderer draws the precomposed glyphs.
            string nfcKey = $"{normalized}_{primaryFont.GetHashCode()}_nfc";
            if (Cache.TryGetValue(nfcKey, out TextRunPlan? cachedNfcPlan))
                return cachedNfcPlan;

            TextRunPlan nfcPlan = BuildNormalizedPrimaryPlan(normalized, primaryFont);
            if (Cache.Count >= MaxCacheEntries)
            {
                Cache.Clear();
            }
            Cache[nfcKey] = nfcPlan;
            return nfcPlan;
        }

        if (secondaryFont == null)
        {
            // Report missing glyphs if no secondary font is available
            if (fontManager != null)
            {
                for (int i = 0; i < normalized.Length; i++)
                {
                    char c = normalized[i];
                    if (c != '\r' && c != '\n' && c != '\t' && c != ' ' && !fontManager.ContainsCharacter(primaryFont, c))
                    {
                        fontManager.ReportMissingGlyphOnce(c, normalized);
                    }
                }
            }
            return null;
        }

        string cacheKey = $"{normalized}_{primaryFont.GetHashCode()}_{secondaryFont.GetHashCode()}";
        if (Cache.TryGetValue(cacheKey, out TextRunPlan? cachedPlan))
            return cachedPlan;

        TextRunPlan plan = BuildPlan(normalized, primaryFont, secondaryFont, fontManager);

        if (Cache.Count >= MaxCacheEntries)
        {
            Cache.Clear();
        }
        Cache[cacheKey] = plan;

        return plan;
    }

    private static TextRunPlan BuildNormalizedPrimaryPlan(string text, SpriteFont primaryFont)
    {
        bool wasMeasuring = BilingualFontRenderer.IsMeasuringActive;
        BilingualFontRenderer.IsMeasuringActive = true;
        try
        {
            List<TextRun> runs = new();
            string[] rawLines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            float currentY = 0f;
            float maxLineWidth = 0f;

            for (int lineIdx = 0; lineIdx < rawLines.Length; lineIdx++)
            {
                string line = rawLines[lineIdx];
                if (line.Length == 0)
                {
                    currentY += primaryFont.LineSpacing;
                    continue;
                }

                Vector2 size = primaryFont.MeasureString(line);
                runs.Add(new TextRun(line, primaryFont, new Vector2(0f, currentY), size));
                if (size.X > maxLineWidth)
                {
                    maxLineWidth = size.X;
                }
                currentY += primaryFont.LineSpacing;
            }

            Vector2 totalSize = new(maxLineWidth, currentY);
            return new TextRunPlan(runs, totalSize, rawLines.Length);
        }
        finally
        {
            BilingualFontRenderer.IsMeasuringActive = wasMeasuring;
        }
    }

    private static TextRunPlan BuildPlan(
        string text,
        SpriteFont primaryFont,
        SpriteFont secondaryFont,
        BilingualFontManager? fontManager)
    {
        bool wasMeasuring = BilingualFontRenderer.IsMeasuringActive;
        BilingualFontRenderer.IsMeasuringActive = true;
        try
        {
            List<TextRun> runs = new();
            string[] rawLines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            float currentY = 0f;
            float maxLineWidth = 0f;

            for (int lineIdx = 0; lineIdx < rawLines.Length; lineIdx++)
            {
                string line = rawLines[lineIdx];
                if (line.Length == 0)
                {
                    // Empty newline
                    currentY += primaryFont.LineSpacing;
                    continue;
                }

                // Inspect line character coverage
                bool lineHasMissingInPrimary = false;
                bool lineAllInSecondary = true;

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    if (c == '\t' || c == ' ')
                        continue;

                    bool inPrimary = fontManager?.ContainsCharacter(primaryFont, c) ?? primaryFont.Characters.Contains(c);
                    bool inSecondary = fontManager?.ContainsCharacter(secondaryFont, c) ?? secondaryFont.Characters.Contains(c);

                    if (!inPrimary)
                    {
                        lineHasMissingInPrimary = true;
                        if (!inSecondary)
                        {
                            fontManager?.ReportMissingGlyphOnce(c, text);
                        }
                    }

                    if (!inSecondary)
                    {
                        lineAllInSecondary = false;
                    }
                }

                List<TextRun> lineRuns = new();

                // Segment preference rule:
                // If the whole line can be rendered by secondary font and has secondary-specific glyphs,
                // render the entire line with secondary font to keep accent baselines and kerning uniform.
                if (lineHasMissingInPrimary && lineAllInSecondary)
                {
                    Vector2 size = secondaryFont.MeasureString(line);
                    lineRuns.Add(new TextRun(line, secondaryFont, new Vector2(0f, currentY), size));
                }
                else if (!lineHasMissingInPrimary)
                {
                    // Pure primary line
                    Vector2 size = primaryFont.MeasureString(line);
                    lineRuns.Add(new TextRun(line, primaryFont, new Vector2(0f, currentY), size));
                }
                else
                {
                    // Mixed line: segment into contiguous runs
                    float lineX = 0f;
                    StringBuilder runBuffer = new();
                    SpriteFont currentRunFont = primaryFont;

                    for (int i = 0; i < line.Length; i++)
                    {
                        char c = line[i];
                        bool inPrimary = fontManager?.ContainsCharacter(primaryFont, c) ?? primaryFont.Characters.Contains(c);
                        bool inSecondary = fontManager?.ContainsCharacter(secondaryFont, c) ?? secondaryFont.Characters.Contains(c);

                        SpriteFont targetFont;
                        if (!inPrimary && inSecondary)
                        {
                            targetFont = secondaryFont;
                        }
                        else if (inPrimary && !inSecondary)
                        {
                            targetFont = primaryFont;
                        }
                        else
                        {
                            // Both support it (or whitespace/neither): keep current run font to avoid needless segmentation
                            targetFont = currentRunFont;
                        }

                        if (runBuffer.Length > 0 && targetFont != currentRunFont)
                        {
                            string runText = runBuffer.ToString();
                            Vector2 runSize = currentRunFont.MeasureString(runText);
                            lineRuns.Add(new TextRun(runText, currentRunFont, new Vector2(lineX, currentY), runSize));
                            lineX += runSize.X;
                            runBuffer.Clear();
                        }

                        currentRunFont = targetFont;
                        runBuffer.Append(c);
                    }

                    if (runBuffer.Length > 0)
                    {
                        string runText = runBuffer.ToString();
                        Vector2 runSize = currentRunFont.MeasureString(runText);
                        lineRuns.Add(new TextRun(runText, currentRunFont, new Vector2(lineX, currentY), runSize));
                        lineX += runSize.X;
                    }
                }

                // Calculate line max height and total line width
                float lineWidth = 0f;
                float lineHeight = lineRuns.Count > 0 ? 0f : primaryFont.LineSpacing;

                foreach (TextRun r in lineRuns)
                {
                    lineWidth += r.Size.X;
                    if (r.Font.LineSpacing > lineHeight)
                    {
                        lineHeight = r.Font.LineSpacing;
                    }
                }

                if (lineWidth > maxLineWidth)
                {
                    maxLineWidth = lineWidth;
                }

                runs.AddRange(lineRuns);
                currentY += lineHeight;
            }

            Vector2 totalSize = new(maxLineWidth, currentY);
            return new TextRunPlan(runs, totalSize, rawLines.Length);
        }
        finally
        {
            BilingualFontRenderer.IsMeasuringActive = wasMeasuring;
        }
    }
}
