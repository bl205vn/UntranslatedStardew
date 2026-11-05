using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using UntranslatedStardew.Core;

namespace UntranslatedStardew;

/// <summary>
/// Stable implementation of IUntranslatedStardewApi exposed via ModEntry.GetApi().
/// References the active resolver and configuration dynamically to avoid stale state.
/// </summary>
public sealed class UntranslatedStardewApi : IUntranslatedStardewApi
{
    private readonly Func<ItemNameResolver?> _getResolver;
    private readonly Func<ModConfig> _getConfig;

    public UntranslatedStardewApi(Func<ItemNameResolver?> getResolver, Func<ModConfig> getConfig)
    {
        _getResolver = getResolver ?? (() => null);
        _getConfig = getConfig ?? (() => new ModConfig());
    }

    public (string? PrimaryName, string? SecondaryName) GetNames(Item? item)
    {
        if (item == null)
            return (null, null);

        string primary = item.DisplayName;
        ItemNameResolver? resolver = _getResolver();
        string? secondary = resolver?.GetSecondaryName(item);

        return (primary, secondary);
    }

    public IEnumerable<string> GetSearchTerms(Item? item, bool includeNormalized = true)
    {
        if (item == null)
            return Enumerable.Empty<string>();

        string primary = item.DisplayName;
        ItemNameResolver? resolver = _getResolver();
        string? secondary = resolver?.GetSecondaryName(item);

        return SearchMatcher.GenerateSearchTerms(primary, secondary, includeNormalized);
    }

    public bool Matches(Item? item, string? query, bool? ignoreDiacritics = null)
    {
        if (item == null || query == null)
            return false;

        string trimmedQuery = query.Trim();
        if (trimmedQuery.Length == 0)
            return true;

        bool effectiveIgnore = ignoreDiacritics ?? _getConfig().DiacriticInsensitiveSearch;

        string primary = item.DisplayName;
        if (SearchMatcher.MatchesText(primary, trimmedQuery, effectiveIgnore))
            return true;

        ItemNameResolver? resolver = _getResolver();
        string? secondary = resolver?.GetSecondaryName(item);
        if (!string.IsNullOrEmpty(secondary) && SearchMatcher.MatchesText(secondary, trimmedQuery, effectiveIgnore))
            return true;

        return false;
    }
}
