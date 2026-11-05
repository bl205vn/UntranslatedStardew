using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace UntranslatedStardew.Core;

/// <summary>
/// Plans font run segmentation and exact geometric metrics for bilingual text blocks.
/// Enforces fixed font roles: primary chunks always target the live Game1 font (honoring FontSettings);
/// secondary chunks always target the secondary locale font, even for ASCII-only words.
/// Normalizes text to Unicode Form C, supports explicit role composition metadata,
/// and retains glyph fallback only when the selected role font itself lacks a glyph.
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
    /// without role specialization, or if no fallback/secondary font is needed.
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

        // 2. Retrieve explicit role composition if available
        bool hasComposition = BilingualStringTracker.TryGetComposition(text, out StructuredTextComposition? composition);
        if (!hasComposition)
        {
            hasComposition = BilingualStringTracker.TryGetComposition(normalized, out composition);
        }

        // 3. Resolve active or scoped role if composition is not already explicit
        FontRole? activeRole = BilingualRenderScope.ResolveActiveRole(text);
        if (!activeRole.HasValue && isDecomposed)
        {
            activeRole = BilingualRenderScope.ResolveActiveRole(normalized);
        }

        if (activeRole.HasValue)
        {
            composition = StructuredTextComposition.CreateSingle(normalized, activeRole.Value);
            hasComposition = true;
        }

        // 4. Inspect character roles and coverage
        bool hasSecondaryRole = false;
        bool needsGlyphFallback = false;

        for (int i = 0; i < normalized.Length; i++)
        {
            char c = normalized[i];
            if (c == '\r' || c == '\n' || c == '\t' || c == ' ')
                continue;

            FontRole role = composition?.GetRoleAt(i) ?? FontRole.Primary;

            if (role == FontRole.Secondary)
            {
                if (secondaryFont != null && !ReferenceEquals(secondaryFont, primaryFont))
                {
                    hasSecondaryRole = true;
                }
                else if (secondaryFont == null)
                {
                    bool inPrimary = fontManager?.ContainsCharacter(primaryFont, c) ?? primaryFont.Characters.Contains(c);
                    if (!inPrimary)
                    {
                        needsGlyphFallback = true;
                    }
                }
            }
            else // FontRole.Primary
            {
                bool inPrimary = fontManager?.ContainsCharacter(primaryFont, c) ?? primaryFont.Characters.Contains(c);
                if (!inPrimary)
                {
                    needsGlyphFallback = true;
                }
            }
        }

        // Fast path: pure primary role, all glyphs covered, and not decomposed
        if (!hasSecondaryRole && !needsGlyphFallback)
        {
            if (!isDecomposed)
                return null;

            // Input was decomposed (NFD) but precomposed characters exist in primary font
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

        string roleFingerprint = GetRoleFingerprint(composition, normalized.Length, activeRole);
        string cacheKey = $"{normalized}_{primaryFont.GetHashCode()}_{secondaryFont?.GetHashCode() ?? 0}_{roleFingerprint}";
        if (Cache.TryGetValue(cacheKey, out TextRunPlan? cachedPlan))
            return cachedPlan;

        TextRunPlan plan = BuildPlan(normalized, primaryFont, secondaryFont, fontManager, composition);

        if (Cache.Count >= MaxCacheEntries)
        {
            Cache.Clear();
        }
        Cache[cacheKey] = plan;

        return plan;
    }

    private static string GetRoleFingerprint(StructuredTextComposition? composition, int length, FontRole? activeRole)
    {
        if (composition == null || composition.Spans.Count == 0)
            return activeRole.HasValue ? (activeRole.Value == FontRole.Secondary ? "AR:S" : "AR:P") : "P";

        StringBuilder sb = new();
        if (activeRole.HasValue)
        {
            sb.Append("AR:").Append(activeRole.Value == FontRole.Secondary ? "S" : "P").Append('|');
        }
        for (int i = 0; i < composition.Spans.Count; i++)
        {
            RoleSpan s = composition.Spans[i];
            sb.Append(s.Role == FontRole.Secondary ? "S" : "P")
              .Append(s.Start)
              .Append('-')
              .Append(s.Length)
              .Append(';');
        }
        return sb.ToString();
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
        SpriteFont? secondaryFont,
        BilingualFontManager? fontManager,
        StructuredTextComposition? composition)
    {
        bool wasMeasuring = BilingualFontRenderer.IsMeasuringActive;
        BilingualFontRenderer.IsMeasuringActive = true;
        try
        {
            List<TextRun> runs = new();
            string[] rawLines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            float currentY = 0f;
            float maxLineWidth = 0f;
            int charOffset = 0;

            for (int lineIdx = 0; lineIdx < rawLines.Length; lineIdx++)
            {
                string line = rawLines[lineIdx];
                if (line.Length == 0)
                {
                    currentY += primaryFont.LineSpacing;
                    charOffset += GetNewlineLength(text, charOffset);
                    continue;
                }

                // Inspect line roles and coverage
                bool allSecondaryRole = composition != null;
                bool allPrimaryRole = true;
                bool lineHasMissingInPrimary = false;
                bool lineAllInSecondary = secondaryFont != null;
                bool lineAllInPrimary = true;

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    FontRole role = composition?.GetRoleAt(charOffset + i) ?? FontRole.Primary;

                    if (role == FontRole.Secondary)
                    {
                        allPrimaryRole = false;
                    }
                    else
                    {
                        allSecondaryRole = false;
                    }

                    if (c == '\t' || c == ' ')
                        continue;

                    bool inPrimary = fontManager?.ContainsCharacter(primaryFont, c) ?? primaryFont.Characters.Contains(c);
                    bool inSecondary = secondaryFont != null && (fontManager?.ContainsCharacter(secondaryFont, c) ?? secondaryFont.Characters.Contains(c));

                    if (!inPrimary)
                    {
                        lineHasMissingInPrimary = true;
                        lineAllInPrimary = false;
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

                // Whole-line optimization rules:
                // Rule 1: Line is explicitly designated as Secondary role and secondaryFont covers the characters
                if (allSecondaryRole && secondaryFont != null && lineAllInSecondary)
                {
                    Vector2 size = secondaryFont.MeasureString(line);
                    lineRuns.Add(new TextRun(line, secondaryFont, new Vector2(0f, currentY), size));
                }
                // Rule 2: Line is explicitly designated as Primary role (or untracked) and primaryFont covers the characters
                else if (allPrimaryRole && lineAllInPrimary)
                {
                    Vector2 size = primaryFont.MeasureString(line);
                    lineRuns.Add(new TextRun(line, primaryFont, new Vector2(0f, currentY), size));
                }
                // Rule 3: Whole-line fallback: line has characters missing in primary, but secondaryFont covers the whole line
                else if (lineHasMissingInPrimary && lineAllInSecondary && secondaryFont != null)
                {
                    Vector2 size = secondaryFont.MeasureString(line);
                    lineRuns.Add(new TextRun(line, secondaryFont, new Vector2(0f, currentY), size));
                }
                else
                {
                    // Mixed line: segment into contiguous runs based on explicit roles or character font coverage
                    float lineX = 0f;
                    StringBuilder runBuffer = new();
                    SpriteFont currentRunFont = primaryFont;

                    for (int i = 0; i < line.Length; i++)
                    {
                        char c = line[i];
                        FontRole role = composition?.GetRoleAt(charOffset + i) ?? FontRole.Primary;
                        bool inPrimary = fontManager?.ContainsCharacter(primaryFont, c) ?? primaryFont.Characters.Contains(c);
                        bool inSecondary = secondaryFont != null && (fontManager?.ContainsCharacter(secondaryFont, c) ?? secondaryFont.Characters.Contains(c));

                        SpriteFont targetFont;
                        if (composition != null)
                        {
                            if (role == FontRole.Secondary)
                            {
                                if (secondaryFont != null && inSecondary)
                                {
                                    targetFont = secondaryFont;
                                }
                                else if (inPrimary)
                                {
                                    targetFont = primaryFont;
                                }
                                else
                                {
                                    fontManager?.ReportMissingGlyphOnce(c, text);
                                    targetFont = secondaryFont ?? primaryFont;
                                }
                            }
                            else // FontRole.Primary
                            {
                                if (inPrimary)
                                {
                                    targetFont = primaryFont;
                                }
                                else if (secondaryFont != null && inSecondary)
                                {
                                    targetFont = secondaryFont;
                                }
                                else
                                {
                                    fontManager?.ReportMissingGlyphOnce(c, text);
                                    targetFont = primaryFont;
                                }
                            }
                        }
                        else
                        {
                            // Untracked text: contiguous segment rule
                            if (!inPrimary && inSecondary)
                            {
                                targetFont = secondaryFont!;
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
                        }

                        if (runBuffer.Length > 0 && !ReferenceEquals(targetFont, currentRunFont))
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

                charOffset += line.Length;
                if (charOffset < text.Length)
                {
                    if (text[charOffset] == '\r')
                    {
                        charOffset++;
                        if (charOffset < text.Length && text[charOffset] == '\n')
                        {
                            charOffset++;
                        }
                    }
                    else if (text[charOffset] == '\n')
                    {
                        charOffset++;
                    }
                }
            }

            Vector2 totalSize = new(maxLineWidth, currentY);
            return new TextRunPlan(runs, totalSize, rawLines.Length);
        }
        finally
        {
            BilingualFontRenderer.IsMeasuringActive = wasMeasuring;
        }
    }

    private static int GetNewlineLength(string text, int index)
    {
        if (index >= text.Length)
            return 0;

        if (text[index] == '\r')
        {
            return (index + 1 < text.Length && text[index + 1] == '\n') ? 2 : 1;
        }
        if (text[index] == '\n')
        {
            return 1;
        }
        return 0;
    }
}
