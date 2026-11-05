using System;
using System.Collections.Concurrent;
using System.Text;

namespace UntranslatedStardew.Core;

/// <summary>
/// Tracks own formatted bilingual strings and explicit role composition metadata across active mod surfaces.
/// Allows the bilingual renderer to recognize intended role spans and font associations without heuristics.
/// </summary>
public static class BilingualStringTracker
{
    private const int MaxTrackedStrings = 512;
    private static readonly ConcurrentDictionary<string, StructuredTextComposition> TrackedCompositions = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, byte> TrackedStrings = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a formatted bilingual string for scoped fallback rendering with default primary role.
    /// </summary>
    public static void Register(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        string normalized = text.Normalize(NormalizationForm.FormC);
        if (TrackedStrings.Count >= MaxTrackedStrings)
        {
            TrackedStrings.Clear();
            TrackedCompositions.Clear();
        }

        TrackedStrings[normalized] = 1;
        TrackedStrings[text] = 1;
    }

    /// <summary>
    /// Registers structured text with explicit language font role spans.
    /// </summary>
    public static void RegisterComposition(StructuredTextComposition composition)
    {
        if (composition == null || string.IsNullOrEmpty(composition.FullText))
            return;

        string text = composition.FullText;
        string normalized = text.Normalize(NormalizationForm.FormC);

        if (TrackedCompositions.Count >= MaxTrackedStrings)
        {
            TrackedStrings.Clear();
            TrackedCompositions.Clear();
        }

        TrackedCompositions[text] = composition;
        TrackedCompositions[normalized] = composition;
        TrackedStrings[text] = 1;
        TrackedStrings[normalized] = 1;
    }

    /// <summary>
    /// Registers a single text chunk with an explicit font role (e.g. LevelUpMenu titles or subtitles).
    /// </summary>
    public static void RegisterRole(string? text, FontRole role)
    {
        if (string.IsNullOrEmpty(text))
            return;

        BilingualRenderScope.RegisterScopedRole(text, role);
    }

    /// <summary>
    /// Checks whether the string was registered as an own bilingual string.
    /// </summary>
    public static bool IsTracked(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        if (TrackedStrings.ContainsKey(text))
            return true;

        string normalized = text.Normalize(NormalizationForm.FormC);
        return TrackedStrings.ContainsKey(normalized);
    }

    /// <summary>
    /// Attempts to retrieve explicit role composition metadata for a given string.
    /// </summary>
    public static bool TryGetComposition(string? text, out StructuredTextComposition? composition)
    {
        composition = null;
        if (string.IsNullOrEmpty(text))
            return false;

        if (TrackedCompositions.TryGetValue(text, out composition))
            return true;

        string normalized = text.Normalize(NormalizationForm.FormC);
        return TrackedCompositions.TryGetValue(normalized, out composition);
    }

    /// <summary>
    /// Clears all tracked strings and role compositions (e.g. upon locale change or invalidation).
    /// </summary>
    public static void Clear()
    {
        TrackedStrings.Clear();
        TrackedCompositions.Clear();
    }
}
