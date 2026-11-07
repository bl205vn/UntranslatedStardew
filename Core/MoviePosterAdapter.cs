using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.GameData.Movies;

namespace UntranslatedStardew.Core;

/// <summary>Pairs movie poster dialogue pages for now-playing and coming-soon posters.</summary>
internal static class MoviePosterAdapter
{
    internal static bool TryParseLocalizedToken(string? text, out string asset, out string key)
    {
        asset = "";
        key = "";
        if (string.IsNullOrWhiteSpace(text)) return false;
        text = text.Trim();
        if (!text.StartsWith("[LocalizedText ", StringComparison.OrdinalIgnoreCase) || !text.EndsWith("]"))
            return false;
        string inner = text.Substring(15, text.Length - 16).Trim();
        if (inner.Contains(' ')) return false;
        return DialogueTranslation.TryParseSourceKey(inner, out asset, out key);
    }

    internal static string? FormatSafely(string? template, string? argument)
    {
        if (string.IsNullOrWhiteSpace(template) || argument == null)
            return null;
        if (!template.Contains("{0}"))
            return null;
        try
        {
            return string.Format(template, argument).Trim();
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static bool IsSafe(string text)
        => !text.Any(char.IsControl)
           && text.IndexOfAny(new[] { '$', '%', '[', ']', '{', '}', '¦', '\\' }) < 0;

    internal static string ResolvePrimaryText(string? raw, LocalizedContentManager? content)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        try
        {
            if (content != null && TryParseLocalizedToken(raw, out string asset, out string key))
            {
                string? loaded = content.LoadStringReturnNullIfNotFound(asset.Replace('/', '\\') + ":" + key);
                if (!string.IsNullOrWhiteSpace(loaded))
                    return loaded;
            }
            // The game has already expanded its primary tokens. Do not execute
            // random/query tokens again while reconstructing the expected text.
            return raw;
        }
        catch
        {
            return raw;
        }
    }

    private static bool TryLookupSecondaryString(ISecondaryContentProvider provider, string asset, string key, out string? value)
    {
        if (provider is RawContentProvider rcp)
            return rcp.TryGetDialogueString(asset, key, out value);
        return provider.TryGetString(asset, key, out value);
    }

    internal static string? ResolveSecondaryField(string? rawField, string movieId, bool isTitle, ISecondaryContentProvider provider)
    {
        string? patched = provider is RawContentProvider rcp
            ? (isTitle ? rcp.GetPatchedMovieTitle(movieId) : rcp.GetPatchedMovieDescription(movieId)) : null;
        string? field = patched ?? rawField;
        if (TryParseLocalizedToken(field, out string asset, out string key))
            return TryLookupSecondaryString(provider, asset, key, out string? secondary)
                && !string.IsNullOrWhiteSpace(secondary) ? secondary : null;
        return !string.IsNullOrWhiteSpace(patched) && IsSafe(patched) ? patched : null;
    }

    internal static DialogueTextPair[]? GetMoviePosterPages(
        MovieData movie,
        bool isUpcoming,
        ISecondaryContentProvider provider,
        LocalizedContentManager? primaryContent = null,
        IReadOnlyList<string>? visibleDialogues = null)
    {
        if (movie == null || provider == null || !provider.IsSecondaryAvailable)
            return null;

        primaryContent ??= Game1.content;

        string primaryTitle = ResolvePrimaryText(movie.Title, primaryContent);
        string? secondaryTitle = ResolveSecondaryField(movie.Title, movie.Id, isTitle: true, provider);
        if (string.IsNullOrWhiteSpace(secondaryTitle))
            return null;

        if (isUpcoming)
        {
            string primaryPage0;
            try
            {
                primaryPage0 = primaryContent?.LoadString("Strings\\Locations:Theater_Poster_Coming_Soon", primaryTitle)
                    ?? string.Format("Coming soon: \"{0}.\"", primaryTitle);
            }
            catch
            {
                return null;
            }

            if (visibleDialogues != null)
            {
                if (visibleDialogues.Count != 1 || visibleDialogues[0].Trim() != primaryPage0.Trim())
                    return null;
            }

            if (!TryLookupSecondaryString(provider, "Strings/Locations", "Theater_Poster_Coming_Soon", out string? secTemplate)
                || string.IsNullOrWhiteSpace(secTemplate))
                return null;

            string? secondaryPage0 = FormatSafely(secTemplate, secondaryTitle);
            if (string.IsNullOrWhiteSpace(secondaryPage0) || !IsSafe(secondaryPage0))
                return null;

            if (primaryPage0.Trim() == secondaryPage0.Trim())
                return null;

            return new[] { new DialogueTextPair(primaryPage0.Trim(), secondaryPage0.Trim().Replace('^', '\n').Normalize()) };
        }
        else
        {
            string primaryDescription = ResolvePrimaryText(movie.Description, primaryContent);
            string? secondaryDescription = ResolveSecondaryField(movie.Description, movie.Id, isTitle: false, provider);
            if (string.IsNullOrWhiteSpace(secondaryDescription))
                return null;

            string primaryPage0;
            string primaryPage1;
            try
            {
                primaryPage0 = primaryContent?.LoadString("Strings\\Locations:Theater_Poster_0", primaryTitle)
                    ?? string.Format("Now Playing: \"{0}.\"", primaryTitle);
                primaryPage1 = primaryContent?.LoadString("Strings\\Locations:Theater_Poster_1", primaryDescription)
                    ?? string.Format("\"{0}\"", primaryDescription);
            }
            catch
            {
                return null;
            }

            if (visibleDialogues != null)
            {
                if (visibleDialogues.Count != 2
                    || visibleDialogues[0].Trim() != primaryPage0.Trim()
                    || visibleDialogues[1].Trim() != primaryPage1.Trim())
                    return null;
            }

            if (!TryLookupSecondaryString(provider, "Strings/Locations", "Theater_Poster_0", out string? secTemplate0)
                || string.IsNullOrWhiteSpace(secTemplate0)
                || !TryLookupSecondaryString(provider, "Strings/Locations", "Theater_Poster_1", out string? secTemplate1)
                || string.IsNullOrWhiteSpace(secTemplate1))
                return null;

            string? secondaryPage0 = FormatSafely(secTemplate0, secondaryTitle);
            string? secondaryPage1 = FormatSafely(secTemplate1, secondaryDescription);

            if (string.IsNullOrWhiteSpace(secondaryPage0) || !IsSafe(secondaryPage0)
                || string.IsNullOrWhiteSpace(secondaryPage1) || !IsSafe(secondaryPage1))
                return null;

            if (primaryPage0.Trim() == secondaryPage0.Trim() && primaryPage1.Trim() == secondaryPage1.Trim())
                return null;

            return new[]
            {
                new DialogueTextPair(primaryPage0.Trim(), secondaryPage0.Trim().Replace('^', '\n').Normalize()),
                new DialogueTextPair(primaryPage1.Trim(), secondaryPage1.Trim().Replace('^', '\n').Normalize())
            };
        }
    }
}
