using System;
using System.Collections.Generic;

namespace UntranslatedStardew.Core;

/// <summary>
/// Represents structured text with explicit language font role spans.
/// Ensures role metadata survives line wrapping, defaults, and multi-line compositions.
/// </summary>
public sealed class StructuredTextComposition
{
    public string FullText { get; }
    public IReadOnlyList<RoleSpan> Spans { get; }

    public StructuredTextComposition(string fullText, IReadOnlyList<RoleSpan> spans)
    {
        string raw = fullText ?? string.Empty;
        FullText = raw.Normalize(System.Text.NormalizationForm.FormC);
        var normalizedSpans = new List<RoleSpan>();
        if (spans != null)
        {
            foreach (var span in spans)
            {
                if (span.Start < 0 || span.Length < 0 || span.Start > raw.Length - span.Length)
                    throw new ArgumentOutOfRangeException(nameof(spans));
                int start = raw.Substring(0, span.Start).Normalize(System.Text.NormalizationForm.FormC).Length;
                int end = raw.Substring(0, span.Start + span.Length).Normalize(System.Text.NormalizationForm.FormC).Length;
                normalizedSpans.Add(new RoleSpan(start, end - start, span.Role));
            }
        }
        Spans = normalizedSpans.AsReadOnly();
    }

    /// <summary>
    /// Gets the assigned font role for the character at the specified index.
    /// Unannotated characters default to FontRole.Primary.
    /// </summary>
    public FontRole GetRoleAt(int charIndex)
    {
        for (int i = 0; i < Spans.Count; i++)
        {
            if (Spans[i].ContainsIndex(charIndex))
                return Spans[i].Role;
        }
        return FontRole.Primary;
    }

    /// <summary>
    /// Creates a bilingual composition from primary and secondary text chunks.
    /// Chunks are normalized to Unicode Form C before computing span offsets,
    /// guaranteeing exact character alignment for decomposed (NFD) inputs.
    /// SecondaryFirst swaps line positions only, never role-to-font association.
    /// Optionally appends an unannotated or primary body remainder (e.g. for Collections descriptions).
    /// </summary>
    public static StructuredTextComposition CreateBilingual(
        string primaryChunk,
        string secondaryChunk,
        bool secondaryFirst,
        string delimiter = "\n",
        string? body = null,
        string bodyDelimiter = "\n\n")
    {
        primaryChunk = (primaryChunk ?? string.Empty).Normalize(System.Text.NormalizationForm.FormC);
        secondaryChunk = (secondaryChunk ?? string.Empty).Normalize(System.Text.NormalizationForm.FormC);
        delimiter = (delimiter ?? "\n").Normalize(System.Text.NormalizationForm.FormC);
        if (body != null)
        {
            body = body.Normalize(System.Text.NormalizationForm.FormC);
        }
        if (bodyDelimiter != null)
        {
            bodyDelimiter = bodyDelimiter.Normalize(System.Text.NormalizationForm.FormC);
        }

        List<RoleSpan> spans = new();

        string fullText;
        if (secondaryFirst)
        {
            // Chunk 0: secondary
            int secStart = 0;
            int secLen = secondaryChunk.Length;
            if (secLen > 0)
            {
                spans.Add(new RoleSpan(secStart, secLen, FontRole.Secondary));
            }

            int primStart = secLen + delimiter.Length;
            int primLen = primaryChunk.Length;
            if (primLen > 0)
            {
                spans.Add(new RoleSpan(primStart, primLen, FontRole.Primary));
            }

            fullText = $"{secondaryChunk}{delimiter}{primaryChunk}";
        }
        else
        {
            // Chunk 0: primary
            int primStart = 0;
            int primLen = primaryChunk.Length;
            if (primLen > 0)
            {
                spans.Add(new RoleSpan(primStart, primLen, FontRole.Primary));
            }

            int secStart = primLen + delimiter.Length;
            int secLen = secondaryChunk.Length;
            if (secLen > 0)
            {
                spans.Add(new RoleSpan(secStart, secLen, FontRole.Secondary));
            }

            fullText = $"{primaryChunk}{delimiter}{secondaryChunk}";
        }

        if (!string.IsNullOrEmpty(body))
        {
            bodyDelimiter ??= "\n\n";
            int bodyStart = fullText.Length + bodyDelimiter.Length;
            int bodyLen = body.Length;
            spans.Add(new RoleSpan(bodyStart, bodyLen, FontRole.Primary));
            fullText = $"{fullText}{bodyDelimiter}{body}";
        }

        return new StructuredTextComposition(fullText, spans);
    }

    /// <summary>
    /// Creates a composition for a single text chunk with an explicit role,
    /// normalized to Unicode Form C.
    /// </summary>
    public static StructuredTextComposition CreateSingle(string text, FontRole role)
    {
        text = (text ?? string.Empty).Normalize(System.Text.NormalizationForm.FormC);
        RoleSpan[] spans = text.Length > 0
            ? new[] { new RoleSpan(0, text.Length, role) }
            : Array.Empty<RoleSpan>();

        return new StructuredTextComposition(text, spans);
    }
}
