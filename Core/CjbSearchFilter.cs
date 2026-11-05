using System;
using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>
/// Pure testable matching logic for CJB Item Spawner item filtering.
/// Combines CJB's original name substring matching with diacritic-insensitive Vietnamese matching,
/// secondary name matching, and item ID matching.
/// Preserves category tab and sorting filters.
/// </summary>
public static class CjbSearchFilter
{
    /// <summary>
    /// Evaluates whether an item matches a search query under the active configuration and resolver.
    /// </summary>
    /// <param name="item">The Stardew Valley Item instance (if available).</param>
    /// <param name="name">The item's internal name.</param>
    /// <param name="displayName">The item's primary display name in the current language.</param>
    /// <param name="id">The item's unqualified ID.</param>
    /// <param name="qualifiedItemId">The item's qualified ID (e.g. "(TR)FrogEgg" or "(O)128").</param>
    /// <param name="query">The search text typed by the player.</param>
    /// <param name="resolver">The secondary item name resolver.</param>
    /// <param name="ignoreDiacritics">Whether to normalize diacritics and Vietnamese 'đ'/'Đ'.</param>
    /// <returns>True if the item matches the search query; otherwise, false.</returns>
    public static bool Matches(
        Item? item,
        string? name,
        string? displayName,
        string? id,
        string? qualifiedItemId,
        string query,
        ItemNameResolver? resolver,
        bool ignoreDiacritics = true)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        string trimmed = query.Trim();

        // 1. Original CJB search behavior: case-insensitive substring on Name and DisplayName
        if (name?.Contains(trimmed, StringComparison.InvariantCultureIgnoreCase) == true
            || displayName?.Contains(trimmed, StringComparison.InvariantCultureIgnoreCase) == true)
        {
            return true;
        }

        // 2. ID matching: exact match against unqualified ID or qualified item ID
        if (!string.IsNullOrEmpty(id) && string.Equals(id, trimmed, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrEmpty(qualifiedItemId) && string.Equals(qualifiedItemId, trimmed, StringComparison.OrdinalIgnoreCase))
            return true;

        // 3. Diacritic-insensitive matching on primary display name (e.g. query "ca noc" matches "Cá nóc")
        if (!string.IsNullOrEmpty(displayName) && SearchMatcher.MatchesText(displayName, trimmed, ignoreDiacritics))
            return true;

        // 4. Diacritic-insensitive matching on Name
        if (!string.IsNullOrEmpty(name) && SearchMatcher.MatchesText(name, trimmed, ignoreDiacritics))
            return true;

        // 5. Secondary name matching (e.g. English, French, Vietnamese secondary name)
        if (resolver != null)
        {
            string? secondaryName = null;
            if (item != null)
            {
                secondaryName = resolver.GetSecondaryName(item);
            }
            else if (!string.IsNullOrEmpty(qualifiedItemId))
            {
                secondaryName = resolver.GetSecondaryNameById(qualifiedItemId);
            }
            else if (!string.IsNullOrEmpty(id))
            {
                secondaryName = resolver.GetSecondaryNameById(id);
            }

            if (!string.IsNullOrEmpty(secondaryName))
            {
                if (secondaryName.Contains(trimmed, StringComparison.InvariantCultureIgnoreCase)
                    || SearchMatcher.MatchesText(secondaryName, trimmed, ignoreDiacritics))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
