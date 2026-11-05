using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UntranslatedStardew.Core;

/// <summary>
/// Provides diacritic-insensitive string matching and search term generation,
/// including Vietnamese 'đ' / 'Đ' normalization.
/// </summary>
public static class SearchMatcher
{
    /// <summary>
    /// Removes accents / diacritics from text and replaces Vietnamese 'đ'/'Đ' with 'd'/'D'.
    /// </summary>
    public static string RemoveDiacritics(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Map Vietnamese specific characters first
        string replaced = text.Replace('đ', 'd').Replace('Đ', 'D');

        string normalized = replaced.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        for (int i = 0; i < normalized.Length; i++)
        {
            char c = normalized[i];
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Checks whether source contains query, with optional diacritic insensitivity.
    /// Comparison is case-insensitive.
    /// </summary>
    public static bool MatchesText(string? source, string? query, bool ignoreDiacritics = true)
    {
        if (string.IsNullOrEmpty(source) || query == null)
            return false;

        string trimmedQuery = query.Trim();
        if (trimmedQuery.Length == 0)
            return true;

        if (ignoreDiacritics)
        {
            string cleanSource = RemoveDiacritics(source);
            string cleanQuery = RemoveDiacritics(trimmedQuery);
            return cleanSource.IndexOf(cleanQuery, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        return source.IndexOf(trimmedQuery, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Generates distinct search terms for an item given its primary and secondary names.
    /// </summary>
    public static IEnumerable<string> GenerateSearchTerms(string? primaryName, string? secondaryName, bool includeNormalized = true)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(primaryName))
        {
            string p = primaryName.Trim();
            if (set.Add(p) && includeNormalized)
            {
                string normP = RemoveDiacritics(p);
                if (!string.IsNullOrWhiteSpace(normP))
                    set.Add(normP);
            }
        }

        if (!string.IsNullOrWhiteSpace(secondaryName))
        {
            string s = secondaryName.Trim();
            if (set.Add(s) && includeNormalized)
            {
                string normS = RemoveDiacritics(s);
                if (!string.IsNullOrWhiteSpace(normS))
                    set.Add(normS);
            }
        }

        return set;
    }
}
