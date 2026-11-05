using System;
using System.Text.RegularExpressions;

namespace UntranslatedStardew.Core;

/// <summary>
/// Handles formatting and duplicate suppression for tooltip titles,
/// automatically registering explicit role composition metadata.
/// </summary>
public static class TooltipFormatter
{
    private static readonly Regex StackSuffixRegex = new(@"\s+x\d+$", RegexOptions.Compiled);

    /// <summary>
    /// Formats the hover title to include the secondary item name if appropriate.
    /// Preserves existing suffixes (e.g., " x5" or recipe labels) and suppresses duplicate titles.
    /// Binds structured role composition metadata so secondary and primary fonts are explicitly honored.
    /// </summary>
    /// <param name="hoverTitle">The current hoverTitle passed into drawToolTip.</param>
    /// <param name="primaryName">The item's primary display name.</param>
    /// <param name="secondaryName">The resolved secondary display name.</param>
    /// <param name="secondaryFirst">Whether the secondary name should appear above the primary name.</param>
    /// <returns>The formatted title, or the original title if unchanged.</returns>
    public static string FormatTitle(string hoverTitle, string? primaryName, string? secondaryName, bool secondaryFirst = false)
    {
        if (string.IsNullOrEmpty(hoverTitle) || string.IsNullOrWhiteSpace(secondaryName))
            return hoverTitle;

        // If the title already contains a newline, it was already formatted or multiline; avoid double modification.
        if (hoverTitle.Contains('\n'))
            return hoverTitle;

        // Suppress exact or case-insensitive duplicate names
        if (!string.IsNullOrEmpty(primaryName) && string.Equals(primaryName.Trim(), secondaryName.Trim(), StringComparison.OrdinalIgnoreCase))
            return hoverTitle;

        // Check if hoverTitle is identical to secondary name
        if (string.Equals(hoverTitle.Trim(), secondaryName.Trim(), StringComparison.OrdinalIgnoreCase))
            return hoverTitle;

        string suffix = string.Empty;
        if (!string.IsNullOrEmpty(primaryName) && hoverTitle.StartsWith(primaryName, StringComparison.OrdinalIgnoreCase))
        {
            suffix = hoverTitle.Substring(primaryName.Length);
        }
        else
        {
            Match match = StackSuffixRegex.Match(hoverTitle);
            if (match.Success)
            {
                suffix = match.Value;
            }
        }

        string secondaryWithSuffix = $"{secondaryName}{suffix}";

        // If the secondary title with suffix matches hoverTitle, suppress duplicate
        if (string.Equals(hoverTitle.Trim(), secondaryWithSuffix.Trim(), StringComparison.OrdinalIgnoreCase))
            return hoverTitle;

        StructuredTextComposition composition = StructuredTextComposition.CreateBilingual(
            primaryChunk: hoverTitle,
            secondaryChunk: secondaryWithSuffix,
            secondaryFirst: secondaryFirst,
            delimiter: "\n"
        );

        BilingualStringTracker.RegisterComposition(composition);

        return composition.FullText;
    }
}
