using System;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace UntranslatedStardew.Core;

/// <summary>
/// Tracks own formatted bilingual strings across active mod surfaces.
/// Allows the bilingual renderer to recognize intended bilingual targets without heuristics.
/// </summary>
public static class BilingualStringTracker
{
    private const int MaxTrackedStrings = 512;
    private static readonly ConcurrentDictionary<string, byte> TrackedStrings = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a formatted bilingual string for scoped fallback rendering.
    /// </summary>
    public static void Register(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        string normalized = text.Normalize(NormalizationForm.FormC);
        if (TrackedStrings.Count >= MaxTrackedStrings)
        {
            TrackedStrings.Clear();
        }
        TrackedStrings[normalized] = 1;
    }

    /// <summary>
    /// Checks whether the string was registered as an own bilingual string.
    /// </summary>
    public static bool IsTracked(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        string normalized = text.Normalize(NormalizationForm.FormC);
        return TrackedStrings.ContainsKey(normalized);
    }

    /// <summary>
    /// Clears all tracked strings (e.g. upon locale change or invalidation).
    /// </summary>
    public static void Clear()
    {
        TrackedStrings.Clear();
    }
}
