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
    private readonly Func<string, ItemNameResolver?>? _getResolverForLanguage;
    private readonly Func<LanguageSourceRegistry?>? _getLanguageSourceRegistry;
    private readonly HashSet<string> _loggedErrors = new(StringComparer.OrdinalIgnoreCase);

    public UntranslatedStardewApi(
        Func<ItemNameResolver?> getResolver,
        Func<ModConfig> getConfig,
        Func<string, ItemNameResolver?>? getResolverForLanguage = null,
        Func<LanguageSourceRegistry?>? getLanguageSourceRegistry = null)
    {
        _getResolver = getResolver ?? (() => null);
        _getConfig = getConfig ?? (() => new ModConfig());
        _getResolverForLanguage = getResolverForLanguage;
        _getLanguageSourceRegistry = getLanguageSourceRegistry;
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

    public string? GetSecondaryName(string category, string id, string? language = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        ItemNameResolver? defaultResolver = _getResolver();
        if (defaultResolver == null)
            return null;

        string normalizedCategory = ExternalTranslationRegistry.NormalizeCategory(category);
        bool isExplicitLanguage = !string.IsNullOrWhiteSpace(language);
        string targetLanguage = isExplicitLanguage ? language!.Trim() : defaultResolver.EffectiveLanguage;

        // 1. Check external registry first for any category (honors targetLanguage)
        string? fromExternal = defaultResolver.ExternalRegistry.GetTranslation(normalizedCategory, id, targetLanguage);
        if (!string.IsNullOrWhiteSpace(fromExternal))
            return fromExternal;

        // 2. Select appropriate resolver for targetLanguage
        ItemNameResolver? targetResolver = GetResolverForTargetLanguage(targetLanguage, defaultResolver);
        if (targetResolver == null)
            return null;

        // When an explicit language is requested, ensure the targetResolver actually supports that language
        // rather than having fallen back to an unrelated language (e.g. French fallback when Vietnamese was requested)
        if (isExplicitLanguage && !IsLanguageMatch(targetResolver.EffectiveLanguage, targetLanguage))
            return null;

        // 3. Category-specific fallbacks using targetResolver
        return normalizedCategory switch
        {
            "Item" => targetResolver.GetSecondaryNameById(id),
            "Mail" => targetResolver.GetSecondaryMailTitle(id),
            "Power" => targetResolver.GetSecondaryPowerName(id),
            "Achievement" => targetResolver.GetSecondaryAchievementTitle(id),
            "SecretNote" => int.TryParse(id, out int noteId) ? targetResolver.GetSecondarySecretNoteTitle(noteId) : null,
            "SideTab" => int.TryParse(id, out int tabId) ? targetResolver.GetSecondarySideTabName(tabId) : null,
            _ => null
        };
    }

    private ItemNameResolver? GetResolverForTargetLanguage(string targetLanguage, ItemNameResolver defaultResolver)
    {
        if (_getResolverForLanguage != null)
        {
            ItemNameResolver? custom = _getResolverForLanguage(targetLanguage);
            if (custom != null)
                return custom;
        }

        if (IsLanguageMatch(defaultResolver.EffectiveLanguage, targetLanguage))
            return defaultResolver;

        return null;
    }

    private static bool IsLanguageMatch(string effectiveLanguage, string targetLanguage)
    {
        if (string.Equals(effectiveLanguage.Trim(), targetLanguage.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        string can1 = LanguageHelper.CanonicalizeLanguageCode(effectiveLanguage);
        string can2 = LanguageHelper.CanonicalizeLanguageCode(targetLanguage);

        return string.Equals(can1, can2, StringComparison.OrdinalIgnoreCase);
    }

    public IDisposable RegisterTranslations(string sourceModId, IDictionary<string, string> translations)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourceModId))
                throw new ArgumentException("sourceModId cannot be null or empty.", nameof(sourceModId));

            ItemNameResolver? resolver = _getResolver();
            if (resolver == null)
                return new EmptyDisposable();

            IDisposable token = resolver.ExternalRegistry.RegisterModTranslations(sourceModId, translations);
            resolver.Invalidate();
            return token;
        }
        catch (Exception ex)
        {
            LogErrorOnce(sourceModId, $"Failed to register translations for mod '{sourceModId}': {ex.Message}");
            return new EmptyDisposable();
        }
    }

    public void RegisterTranslation(string sourceModId, string category, string id, string language, string translatedName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourceModId))
                return;

            ItemNameResolver? resolver = _getResolver();
            if (resolver == null)
                return;

            resolver.ExternalRegistry.RegisterModTranslation(sourceModId, category, id, language, translatedName);
            resolver.Invalidate();
        }
        catch (Exception ex)
        {
            LogErrorOnce(sourceModId, $"Failed to register single translation for mod '{sourceModId}': {ex.Message}");
        }
    }

    public bool UnregisterTranslations(string sourceModId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourceModId))
                return false;

            ItemNameResolver? resolver = _getResolver();
            if (resolver == null)
                return false;

            bool result = resolver.ExternalRegistry.UnregisterModTranslations(sourceModId);
            if (result)
            {
                resolver.Invalidate();
            }
            return result;
        }
        catch (Exception ex)
        {
            LogErrorOnce(sourceModId, $"Failed to unregister translations for mod '{sourceModId}': {ex.Message}");
            return false;
        }
    }

    public IDisposable RegisterLanguageSource(string sourceModId, string languageCode, string? contentRoot, string? assetPattern = null, string? smallFont = null, string? dialogueFont = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourceModId))
                throw new ArgumentException("sourceModId cannot be null or empty.", nameof(sourceModId));
            if (string.IsNullOrWhiteSpace(languageCode))
                throw new ArgumentException("languageCode cannot be null or empty.", nameof(languageCode));

            LanguageSourceRegistry? registry = _getLanguageSourceRegistry?.Invoke() ?? ModEntry.Instance?.SourceRegistry;
            if (registry == null)
                return new EmptyDisposable();

            var desc = new LanguageSourceDescriptor
            {
                SourceModId = sourceModId,
                LanguageCode = languageCode,
                ContentRoot = contentRoot,
                AssetPattern = assetPattern,
                SmallFont = smallFont,
                DialogueFont = dialogueFont
            };

            IDisposable token = registry.Register(desc);
            _getResolver()?.Invalidate();
            ModEntry.Instance?.FontManager?.Invalidate();
            return token;
        }
        catch (Exception ex)
        {
            LogErrorOnce(sourceModId, $"Failed to register language source for mod '{sourceModId}': {ex.Message}");
            return new EmptyDisposable();
        }
    }

    public bool UnregisterLanguageSource(string sourceModId, string? languageCode = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourceModId))
                return false;

            LanguageSourceRegistry? registry = _getLanguageSourceRegistry?.Invoke() ?? ModEntry.Instance?.SourceRegistry;
            if (registry == null)
                return false;

            bool removed = registry.Unregister(sourceModId, languageCode);
            if (removed)
            {
                _getResolver()?.Invalidate();
                ModEntry.Instance?.FontManager?.Invalidate();
            }
            return removed;
        }
        catch (Exception ex)
        {
            LogErrorOnce(sourceModId, $"Failed to unregister language source for mod '{sourceModId}': {ex.Message}");
            return false;
        }
    }

    private void LogErrorOnce(string? modKey, string message)
    {
        string key = modKey ?? "Unknown";
        if (_loggedErrors.Add(key))
        {
            ModEntry.Instance?.Monitor.Log(message, StardewModdingAPI.LogLevel.Warn);
        }
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
