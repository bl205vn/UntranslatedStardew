using System;
using System.Collections.Generic;
using StardewValley;

namespace UntranslatedStardew;

/// <summary>
/// Public API for Untranslated Stardew. Allows external mods and search interfaces
/// to retrieve bilingual names, register custom translations, and perform multi-language search matching.
/// </summary>
public interface IUntranslatedStardewApi
{
    /// <summary>
    /// Gets both the primary (active game language) and secondary (configured language)
    /// display names for the given item.
    /// </summary>
    /// <param name="item">The item instance to inspect.</param>
    /// <returns>A tuple of (PrimaryName, SecondaryName).</returns>
    (string? PrimaryName, string? SecondaryName) GetNames(Item? item);

    /// <summary>
    /// Gets all search terms/aliases for the item, including primary name, secondary name,
    /// and normalized versions (without diacritics / with 'đ' mapped to 'd').
    /// </summary>
    /// <param name="item">The item instance to generate terms for.</param>
    /// <param name="includeNormalized">Whether to include diacritic-stripped variants.</param>
    /// <returns>An enumeration of unique search terms.</returns>
    IEnumerable<string> GetSearchTerms(Item? item, bool includeNormalized = true);

    /// <summary>
    /// Checks whether the item matches the given search query in either its primary or secondary language.
    /// </summary>
    /// <param name="item">The item to test.</param>
    /// <param name="query">The search term to match against.</param>
    /// <param name="ignoreDiacritics">
    /// Optional override. If null, uses the user's configured DiacriticInsensitiveSearch setting.
    /// If true, diacritics and Vietnamese 'đ'/'Đ' are normalized during comparison.
    /// </param>
    /// <returns>True if the query matches either name, false otherwise.</returns>
    bool Matches(Item? item, string? query, bool? ignoreDiacritics = null);

    /// <summary>
    /// Resolves the secondary name for a given category and ID using the configured secondary language.
    /// Supported categories: "Item", "Mail", "Power", "Achievement", "SecretNote", "SideTab".
    /// </summary>
    /// <param name="category">The content category (e.g. "Item", "Mail", "Power", "Achievement", "SecretNote").</param>
    /// <param name="id">The stable ID of the entry.</param>
    /// <param name="language">Optional language override. If omitted, uses the configured secondary language.</param>
    /// <returns>The secondary display name, or null if no translation exists.</returns>
    string? GetSecondaryName(string category, string id, string? language = null);

    /// <summary>
    /// Registers a dictionary of secondary names provided by another C# mod.
    /// Returns an IDisposable token that unregisters the entries when disposed.
    /// </summary>
    /// <param name="sourceModId">The unique ID of the mod providing translations.</param>
    /// <param name="translations">
    /// Dictionary mapping "{category}/{id}@{language}" (or "{category}:{id}") to translated display name.
    /// </param>
    /// <returns>A token that unregisters the translations when disposed.</returns>
    IDisposable RegisterTranslations(string sourceModId, IDictionary<string, string> translations);

    /// <summary>
    /// Registers a single translation for a given category, ID, and language.
    /// </summary>
    /// <param name="sourceModId">The unique ID of the mod providing the translation.</param>
    /// <param name="category">The category (e.g. "Item", "Mail", "Power", "Achievement", "SecretNote").</param>
    /// <param name="id">The stable ID.</param>
    /// <param name="language">The language code (e.g. "en", "vi", "default").</param>
    /// <param name="translatedName">The translated display name.</param>
    void RegisterTranslation(string sourceModId, string category, string id, string language, string translatedName);

    /// <summary>
    /// Unregisters all translations registered by a given mod ID.
    /// </summary>
    /// <param name="sourceModId">The unique ID of the mod to unregister.</param>
    /// <returns>True if translations were found and removed; otherwise false.</returns>
    bool UnregisterTranslations(string sourceModId);

    /// <summary>
    /// Registers a custom language content source descriptor provided by another C# mod.
    /// Returns an IDisposable token that unregisters the source when disposed.
    /// </summary>
    /// <param name="sourceModId">The unique ID of the mod providing the language source.</param>
    /// <param name="languageCode">The target language code (e.g. "vi", "eo", "tl").</param>
    /// <param name="contentRoot">Validated path to the mod's Content folder.</param>
    /// <param name="assetPattern">Asset mapping pattern (e.g. "{asset}_vi" or "{asset}.{locale}").</param>
    /// <param name="smallFont">Secondary SmallFont asset name (e.g. "Fonts/SmallFont_vi").</param>
    /// <param name="dialogueFont">Secondary DialogueFont asset name (e.g. "Fonts/SpriteFont1_vi").</param>
    /// <returns>An IDisposable token to safely unregister the source.</returns>
    IDisposable RegisterLanguageSource(string sourceModId, string languageCode, string? contentRoot, string? assetPattern = null, string? smallFont = null, string? dialogueFont = null);

    /// <summary>
    /// Unregisters language source(s) registered by a given mod ID.
    /// </summary>
    /// <param name="sourceModId">The unique ID of the mod to unregister.</param>
    /// <param name="languageCode">Optional specific language code to unregister.</param>
    /// <returns>True if sources were found and removed; otherwise false.</returns>
    bool UnregisterLanguageSource(string sourceModId, string? languageCode = null);
}
