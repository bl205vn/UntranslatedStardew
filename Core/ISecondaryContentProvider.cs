using System;

namespace UntranslatedStardew.Core;

/// <summary>
/// Interface for reading string dictionary assets in the secondary language.
/// Decouples item resolution from raw disk/MonoGame content loading.
/// </summary>
public interface ISecondaryContentProvider : IDisposable
{
    /// <summary>
    /// The language code requested by user configuration.
    /// </summary>
    string RequestedLanguage { get; }

    /// <summary>
    /// The actual language being loaded after checking built-in support and mod assets.
    /// Falls back to "en" if requested language assets are not available on disk.
    /// </summary>
    string EffectiveLanguage { get; }

    /// <summary>
    /// Attempts to retrieve a translated string from a given asset dictionary and key.
    /// Implements per-key fallback: if the key is missing in the localized dictionary,
    /// it falls back to the English base dictionary.
    /// </summary>
    /// <param name="assetSubpath">The normalized asset path, e.g. "Strings/Objects" or "Data/Boots".</param>
    /// <param name="key">The dictionary key, e.g. "128_Name".</param>
    /// <param name="value">The translated string if found, else null.</param>
    /// <returns>True if found in localized or fallback dictionary, false otherwise.</returns>
    bool TryGetString(string assetSubpath, string key, out string? value);

    /// <summary>
    /// Retrieves a translated string, or null if not found.
    /// </summary>
    string? GetString(string assetSubpath, string key);

    /// <summary>
    /// Clears any cached dictionaries in memory.
    /// </summary>
    void Invalidate();
}
