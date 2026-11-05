using System;
using System.Collections.Generic;

namespace UntranslatedStardew.Core;

/// <summary>
/// Identifies the provenance of a registered language source for clean lifecycle management.
/// </summary>
public enum LanguageSourceOrigin
{
    Bundled,
    Api,
    AssetDictionary,
    AdditionalLanguages
}

/// <summary>
/// Declarative descriptor for a custom or external language content source.
/// Exposes strings, asset layout patterns, and UI SpriteFont references without requiring hardcoded per-locale logic.
/// </summary>
public sealed class LanguageSourceDescriptor
{
    /// <summary>
    /// The language code (e.g. "vi", "vi-VN", "eo", "tl").
    /// </summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// The unique mod ID or content pack ID providing this language source.
    /// </summary>
    public string SourceModId { get; set; } = string.Empty;

    /// <summary>
    /// Content root path on disk (relative to the providing mod directory, or validated absolute path).
    /// </summary>
    public string? ContentRoot { get; set; }

    /// <summary>
    /// Pattern for mapping asset subpaths (e.g. "{asset}_vi", "{asset}.{locale}", or "{asset}").
    /// Defaults to "{asset}_{locale}".
    /// </summary>
    public string? AssetPattern { get; set; }

    /// <summary>
    /// Asset name for the secondary SmallFont (e.g. "Fonts/SmallFont_vi" or "Fonts/SmallFont").
    /// </summary>
    public string? SmallFont { get; set; }

    /// <summary>
    /// Asset name for the secondary DialogueFont (e.g. "Fonts/SpriteFont1_vi" or "Fonts/SpriteFont1").
    /// </summary>
    public string? DialogueFont { get; set; }
    internal Dictionary<string, (string Root, string Asset)>? SpriteFontAssets { get; set; }
    internal string? BitmapFontAsset { get; set; }
    internal float BitmapFontZoom { get; set; } = 1f;
    internal bool UseGameLatinFont { get; set; }
    internal int GameSmallFontLineSpacing { get; set; }
    internal bool DetectedFromGame { get; set; }

    /// <summary>
    /// Optional in-memory string dictionary overrides.
    /// </summary>
    public Dictionary<string, string>? Strings { get; set; }

    /// <summary>
    /// Optional metadata or custom fields (e.g. from Data/AdditionalLanguages CustomFields).
    /// </summary>
    public Dictionary<string, string>? CustomFields { get; set; }

    /// <summary>
    /// Registration sequence number for deterministic collision resolution (latest wins).
    /// </summary>
    public long Generation { get; set; }

    /// <summary>
    /// Provenance origin of this source registration.
    /// </summary>
    public LanguageSourceOrigin Origin { get; set; } = LanguageSourceOrigin.Api;

    /// <summary>
    /// Formats the asset subpath using the configured pattern.
    /// </summary>
    public string FormatAssetPath(string assetSubpath)
    {
        string pattern = !string.IsNullOrWhiteSpace(AssetPattern) ? AssetPattern : "{asset}_{locale}";
        return pattern
            .Replace("{asset}", assetSubpath)
            .Replace("{locale}", LanguageCode);
    }

    /// <summary>
    /// Creates a deep clone of the descriptor to prevent external mutation of registered state.
    /// </summary>
    public LanguageSourceDescriptor Clone()
    {
        return new LanguageSourceDescriptor
        {
            LanguageCode = LanguageCode,
            SourceModId = SourceModId,
            ContentRoot = ContentRoot,
            AssetPattern = AssetPattern,
            SmallFont = SmallFont,
            DialogueFont = DialogueFont,
            SpriteFontAssets = SpriteFontAssets == null ? null : new(SpriteFontAssets, StringComparer.OrdinalIgnoreCase),
            BitmapFontAsset = BitmapFontAsset,
            BitmapFontZoom = BitmapFontZoom,
            UseGameLatinFont = UseGameLatinFont,
            GameSmallFontLineSpacing = GameSmallFontLineSpacing,
            DetectedFromGame = DetectedFromGame,
            Generation = Generation,
            Origin = Origin,
            Strings = Strings != null ? new Dictionary<string, string>(Strings, StringComparer.OrdinalIgnoreCase) : null,
            CustomFields = CustomFields != null ? new Dictionary<string, string>(CustomFields, StringComparer.OrdinalIgnoreCase) : null
        };
    }
}
