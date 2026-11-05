using System.Collections.Generic;
using StardewValley;

namespace UntranslatedStardew;

/// <summary>
/// Public API for Untranslated Stardew. Allows external mods and search interfaces
/// to retrieve bilingual names and perform multi-language search matching.
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
}
