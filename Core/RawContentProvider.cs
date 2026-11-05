using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Content;
using StardewValley;
using StardewValley.GameData;

namespace UntranslatedStardew.Core;

/// <summary>
/// Loads and caches string dictionaries directly from game or mod XNB files
/// using isolated MonoGame ContentManager instances.
/// Does not mutate game locale or pollute global vanilla asset caches.
/// Supports generic custom language sources via LanguageSourceRegistry,
/// per-key fallback to English, and lazy service provider initialization.
/// </summary>
public sealed class RawContentProvider : ISecondaryContentProvider, ITrinketDataProvider, IPowerDataProvider
{
    private readonly Func<IServiceProvider?> _getServiceProvider;
    private readonly string _gameContentRoot;
    private readonly string? _modContentRoot;
    private readonly LanguageSourceRegistry? _sourceRegistry;
    private LanguageSourceDescriptor? _activeSource;
    private readonly Action<string, string>? _onFallbackWarning;

    private ContentManager? _vanillaContentManager;
    private ContentManager? _modContentManager;
    private readonly Dictionary<string, ContentManager> _customModContentManagers = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, Dictionary<string, string>?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Dictionary<string, TrinketData>?> _trinketDataCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Dictionary<string, StardewValley.GameData.Powers.PowersData>?> _powersDataCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Dictionary<string, StardewValley.GameData.Objects.ObjectData>?> _objectDataCache = new(StringComparer.OrdinalIgnoreCase);
    private bool _warned;
    private bool _disposed;

    public string RequestedLanguage { get; private set; }
    public string EffectiveLanguage { get; private set; }
    public bool IsSecondaryAvailable { get; private set; }
    public LanguageSourceRegistry? SourceRegistry => _sourceRegistry;
    private int _isRebinding;

    public RawContentProvider(
        Func<IServiceProvider?> getServiceProvider,
        string gameContentRoot,
        string? requestedLanguage,
        string? modContentRoot = null,
        Action<string, string>? onFallbackWarning = null,
        LanguageSourceRegistry? sourceRegistry = null)
    {
        _getServiceProvider = getServiceProvider ?? (() => null);
        _gameContentRoot = gameContentRoot;
        _modContentRoot = modContentRoot;
        _onFallbackWarning = onFallbackWarning;
        _sourceRegistry = sourceRegistry;

        RequestedLanguage = string.IsNullOrWhiteSpace(requestedLanguage)
            ? "default"
            : requestedLanguage.Trim();
        EffectiveLanguage = "default";

        Rebind();

        if (_sourceRegistry != null)
        {
            _sourceRegistry.OnChanged += OnSourceRegistryChanged;
        }
    }

    /// <summary>
    /// Rebinds requested and effective language state against the current source registry and disk assets.
    /// Safely supports late source registrations and removals without recreating or disposing the provider.
    /// </summary>
    public void Rebind(string? requestedLanguage = null)
    {
        if (_disposed || System.Threading.Interlocked.CompareExchange(ref _isRebinding, 1, 0) != 0)
            return;

        try
        {
            if (requestedLanguage != null)
            {
                RequestedLanguage = string.IsNullOrWhiteSpace(requestedLanguage)
                    ? "default"
                    : requestedLanguage.Trim();
            }

            string canonical = LanguageHelper.CanonicalizeLanguageCode(RequestedLanguage);
            _warned = false;
            _activeSource = _sourceRegistry != null && _sourceRegistry.TryGetSource(canonical, out var descriptor) ? descriptor : null;

            if (canonical == "default" || canonical == "en")
            {
                EffectiveLanguage = canonical;
                IsSecondaryAvailable = true;
            }
            else if (_sourceRegistry != null && _activeSource is { } source)
            {
                bool hasContent = false;
                if (!string.IsNullOrEmpty(source.ContentRoot) && _sourceRegistry.TryResolveSourceContentRoot(source, out string resolvedRoot) && Directory.Exists(resolvedRoot))
                {
                    hasContent = true;
                }
                if (source.Strings != null && source.Strings.Count > 0)
                {
                    hasContent = true;
                }

                if (hasContent)
                {
                    EffectiveLanguage = canonical;
                    IsSecondaryAvailable = true;
                }
                else
                {
                    EffectiveLanguage = canonical;
                    IsSecondaryAvailable = false;
                    TriggerWarningOnce(RequestedLanguage, "primary-only (missing content)");
                }
            }
            else if (!LanguageHelper.IsBuiltInLanguage(canonical) && !string.IsNullOrEmpty(_modContentRoot))
            {
                if (!string.IsNullOrEmpty(_modContentRoot) && Directory.Exists(_modContentRoot))
                {
                    EffectiveLanguage = canonical;
                    IsSecondaryAvailable = true;
                }
                else
                {
                    EffectiveLanguage = canonical;
                    IsSecondaryAvailable = false;
                    TriggerWarningOnce(RequestedLanguage, "primary-only (missing pack)");
                }
            }
            else if (LanguageHelper.IsBuiltInLanguage(canonical))
            {
                EffectiveLanguage = canonical;
                IsSecondaryAvailable = true;
            }
            else
            {
                EffectiveLanguage = canonical;
                IsSecondaryAvailable = false;
                TriggerWarningOnce(RequestedLanguage, "primary-only (unregistered source)");
            }

            Invalidate();
        }
        finally
        {
            _isRebinding = 0;
        }
    }

    private void OnSourceRegistryChanged()
    {
        Rebind();
    }

    public RawContentProvider(
        IServiceProvider? serviceProvider,
        string gameContentRoot,
        string? requestedLanguage,
        string? modContentRoot = null,
        Action<string, string>? onFallbackWarning = null,
        LanguageSourceRegistry? sourceRegistry = null)
        : this(() => serviceProvider, gameContentRoot, requestedLanguage, modContentRoot, onFallbackWarning, sourceRegistry)
    {
    }

    public bool TryGetString(string assetSubpath, string key, out string? value)
    {
        value = null;
        if (_disposed || string.IsNullOrEmpty(assetSubpath) || string.IsNullOrEmpty(key) || !IsSecondaryAvailable)
            return false;

        string normalizedPath = NormalizeAssetPath(assetSubpath);

        // 1. Check in-memory descriptor string overrides if available
        if (_activeSource?.Strings is { } strings)
        {
            string dictKey = $"{normalizedPath}:{key}";
            if (strings.TryGetValue(dictKey, out string? overrideVal) && !string.IsNullOrWhiteSpace(overrideVal))
            {
                value = overrideVal;
                return true;
            }
            if (strings.TryGetValue(key, out overrideVal) && !string.IsNullOrWhiteSpace(overrideVal))
            {
                value = overrideVal;
                return true;
            }
        }

        // 2. Try localized dictionary if not base / English
        if (!LanguageHelper.IsBaseOrEnglish(EffectiveLanguage))
        {
            Dictionary<string, string>? localizedDict = GetOrLoadDictionary(normalizedPath, EffectiveLanguage);
            if (localizedDict != null && localizedDict.TryGetValue(key, out string? locVal) && !string.IsNullOrWhiteSpace(locVal))
            {
                value = locVal;
                return true;
            }
        }

        // 3. Per-key fallback: Check base default/English dictionary
        Dictionary<string, string>? baseDict = GetOrLoadDictionary(normalizedPath, LanguageHelper.IsBaseOrEnglish(EffectiveLanguage) ? EffectiveLanguage : "default");
        if (baseDict != null && baseDict.TryGetValue(key, out string? engVal) && !string.IsNullOrWhiteSpace(engVal))
        {
            value = engVal;
            return true;
        }

        return false;
    }

    internal string? GetPatchedDisplayName(string asset, string id)
        => IsSecondaryAvailable && _activeSource?.Strings is { } strings
            && strings.TryGetValue($"{asset}/DisplayName:{id}", out var title) ? title : null;

    public string? GetString(string assetSubpath, string key)
    {
        TryGetString(assetSubpath, key, out string? value);
        return value;
    }

    /// <summary>
    /// Attempts to retrieve TrinketData for a given trinket ID.
    /// Checks live DataLoader first if game content is active, then falls back to vanilla ContentManager.
    /// </summary>
    public bool TryGetTrinketData(string itemId, out TrinketData? trinketData)
    {
        trinketData = null;
        if (_disposed || string.IsNullOrEmpty(itemId))
            return false;

        // 1. Live game DataLoader if Game1.content is active
        if (Game1.content != null)
        {
            try
            {
                Dictionary<string, TrinketData>? sheet = DataLoader.Trinkets(Game1.content);
                if (sheet != null && sheet.TryGetValue(itemId, out trinketData))
                    return true;
            }
            catch
            {
                // Ignore and fall back
            }
        }

        // 2. Isolated vanilla ContentManager from disk
        ContentManager? vanillaManager = GetVanillaContentManager();
        if (vanillaManager != null)
        {
            try
            {
                if (!_trinketDataCache.TryGetValue("Data/Trinkets", out Dictionary<string, TrinketData>? dict))
                {
                    dict = vanillaManager.Load<Dictionary<string, TrinketData>>("Data/Trinkets");
                    _trinketDataCache["Data/Trinkets"] = dict;
                }

                if (dict != null && dict.TryGetValue(itemId, out trinketData))
                    return true;
            }
            catch (ContentLoadException)
            {
                // Ignore and return false
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to retrieve PowersData for a given power key.
    /// Checks live DataLoader first if game content is active, then falls back to vanilla ContentManager.
    /// </summary>
    public bool TryGetPowerData(string powerKey, out StardewValley.GameData.Powers.PowersData? powerData)
    {
        powerData = null;
        if (_disposed || string.IsNullOrEmpty(powerKey))
            return false;

        // 1. Live game DataLoader if Game1.content is active
        if (Game1.content != null)
        {
            try
            {
                Dictionary<string, StardewValley.GameData.Powers.PowersData>? sheet = DataLoader.Powers(Game1.content);
                if (sheet != null && sheet.TryGetValue(powerKey, out powerData))
                    return true;
            }
            catch
            {
                // Ignore and fall back
            }
        }

        // 2. Isolated vanilla ContentManager from disk
        ContentManager? vanillaManager = GetVanillaContentManager();
        if (vanillaManager != null)
        {
            try
            {
                if (!_powersDataCache.TryGetValue("Data/Powers", out Dictionary<string, StardewValley.GameData.Powers.PowersData>? dict))
                {
                    dict = vanillaManager.Load<Dictionary<string, StardewValley.GameData.Powers.PowersData>>("Data/Powers");
                    _powersDataCache["Data/Powers"] = dict;
                }

                if (dict != null && dict.TryGetValue(powerKey, out powerData))
                    return true;
            }
            catch (ContentLoadException)
            {
                // Ignore and return false
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to retrieve ObjectData for a given item ID.
    /// Checks live DataLoader first if game content is active, then falls back to vanilla ContentManager.
    /// </summary>
    public bool TryGetObjectData(string itemId, out StardewValley.GameData.Objects.ObjectData? objectData)
    {
        objectData = null;
        if (_disposed || string.IsNullOrEmpty(itemId))
            return false;

        string unqualified = itemId.StartsWith("(O)", StringComparison.OrdinalIgnoreCase) ? itemId.Substring(3) : itemId;

        if (Game1.content != null)
        {
            try
            {
                Dictionary<string, StardewValley.GameData.Objects.ObjectData>? sheet = DataLoader.Objects(Game1.content);
                if (sheet != null && sheet.TryGetValue(unqualified, out objectData))
                    return true;
            }
            catch
            {
                // Ignore and fall back
            }
        }

        ContentManager? vanillaManager = GetVanillaContentManager();
        if (vanillaManager != null)
        {
            try
            {
                if (!_objectDataCache.TryGetValue("Data/Objects", out Dictionary<string, StardewValley.GameData.Objects.ObjectData>? dict))
                {
                    dict = vanillaManager.Load<Dictionary<string, StardewValley.GameData.Objects.ObjectData>>("Data/Objects");
                    _objectDataCache["Data/Objects"] = dict;
                }

                if (dict != null && dict.TryGetValue(unqualified, out objectData))
                    return true;
            }
            catch (ContentLoadException)
            {
                // Ignore and return false
            }
        }

        return false;
    }

    public void Invalidate()
    {
        _cache.Clear();
        _trinketDataCache.Clear();
        _powersDataCache.Clear();
        _objectDataCache.Clear();
        _vanillaContentManager?.Unload();
        _modContentManager?.Unload();
        foreach (var cm in _customModContentManagers.Values)
        {
            cm.Unload();
        }
        _customModContentManagers.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_sourceRegistry != null)
        {
            _sourceRegistry.OnChanged -= OnSourceRegistryChanged;
        }
        _cache.Clear();
        _trinketDataCache.Clear();
        _powersDataCache.Clear();
        _objectDataCache.Clear();
        _vanillaContentManager?.Dispose();
        _modContentManager?.Dispose();
        _vanillaContentManager = null;
        _modContentManager = null;
        foreach (var cm in _customModContentManagers.Values)
        {
            cm.Dispose();
        }
        _customModContentManagers.Clear();
    }

    private static string NormalizeAssetPath(string path)
    {
        string normalized = path.Replace('\\', '/').TrimStart('/');
        if (normalized.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - 4);
        }
        return normalized;
    }

    private Dictionary<string, string>? GetOrLoadDictionary(string assetSubpath, string langCode)
    {
        string cacheKey = $"{assetSubpath}@{langCode}";
        if (_cache.TryGetValue(cacheKey, out Dictionary<string, string>? cached))
            return cached;

        Dictionary<string, string>? loaded = LoadDictionaryFromDisk(assetSubpath, langCode);
        _cache[cacheKey] = loaded;
        return loaded;
    }

    private LanguageSourceDescriptor? GetSourceForAsset(string langCode)
    {
        if (string.Equals(langCode, EffectiveLanguage, StringComparison.OrdinalIgnoreCase))
            return _activeSource;
        return _sourceRegistry != null && _sourceRegistry.TryGetSource(langCode, out var source) ? source : null;
    }

    private Dictionary<string, string>? LoadDictionaryFromDisk(string assetSubpath, string langCode)
    {
        if (assetSubpath.Equals("Data/Achievements", StringComparison.OrdinalIgnoreCase) ||
            assetSubpath.Equals("Data/SecretNotes", StringComparison.OrdinalIgnoreCase))
        {
            return LoadIntStringDictionaryAsStrings(assetSubpath, langCode);
        }

        // 1. Check custom language source registry
        if (_sourceRegistry != null && GetSourceForAsset(langCode) is { } source)
        {
            if (_sourceRegistry.TryResolveSourceContentRoot(source, out string resolvedRoot) && Directory.Exists(resolvedRoot))
            {
                string localizedRelativePath = source.FormatAssetPath(assetSubpath);
                if (!LanguageSourceRegistry.TryResolveSafePath(resolvedRoot, $"{localizedRelativePath}.xnb", out string localizedFilePath)) return null;
                if (File.Exists(localizedFilePath))
                {
                    try
                    {
                        ContentManager cm = GetCustomModContentManager(resolvedRoot);
                        return cm.Load<Dictionary<string, string>>(localizedRelativePath);
                    }
                    catch (ContentLoadException)
                    {
                        // Ignore and fall through
                    }
                }
            }
        }

        // 2. Explicit locale content root
        if (!LanguageHelper.IsBuiltInLanguage(langCode))
        {
            ContentManager? modManager = GetModContentManager();
            if (modManager != null && !string.IsNullOrEmpty(_modContentRoot))
            {
                string viRelativePath = $"{assetSubpath}_{langCode}";
                string viFilePath = Path.Combine(_modContentRoot, $"{viRelativePath}.xnb");
                if (File.Exists(viFilePath))
                {
                    try
                    {
                        return modManager.Load<Dictionary<string, string>>(viRelativePath);
                    }
                    catch (ContentLoadException)
                    {
                        // Ignore and return null
                    }
                }
            }
            return null;
        }

        // 3. Built-in languages
        ContentManager? vanillaManager = GetVanillaContentManager();
        if (vanillaManager == null)
            return null;

        if (!LanguageHelper.IsBaseOrEnglish(langCode))
        {
            string? suffix = LanguageHelper.GetLanguageSuffix(langCode);
            if (!string.IsNullOrEmpty(suffix))
            {
                string localizedRelativePath = $"{assetSubpath}.{suffix}";
                string localizedFilePath = Path.Combine(_gameContentRoot, $"{localizedRelativePath}.xnb");
                if (File.Exists(localizedFilePath))
                {
                    try
                    {
                        return vanillaManager.Load<Dictionary<string, string>>(localizedRelativePath);
                    }
                    catch (ContentLoadException)
                    {
                        // Ignore and return null
                    }
                }
            }
            return null;
        }

        // 4. Default / English
        string basePath = Path.Combine(_gameContentRoot, $"{assetSubpath}.xnb");
        if (File.Exists(basePath))
        {
            try
            {
                return vanillaManager.Load<Dictionary<string, string>>(assetSubpath);
            }
            catch (ContentLoadException)
            {
                // Ignore and return null
            }
        }

        return null;
    }

    private Dictionary<string, string>? LoadIntStringDictionaryAsStrings(string assetSubpath, string langCode)
    {
        // 1. Check custom language source registry
        if (_sourceRegistry != null && GetSourceForAsset(langCode) is { } source)
        {
            if (_sourceRegistry.TryResolveSourceContentRoot(source, out string resolvedRoot) && Directory.Exists(resolvedRoot))
            {
                string localizedRelativePath = source.FormatAssetPath(assetSubpath);
                if (!LanguageSourceRegistry.TryResolveSafePath(resolvedRoot, $"{localizedRelativePath}.xnb", out string localizedFilePath)) return null;
                if (File.Exists(localizedFilePath))
                {
                    try
                    {
                        ContentManager cm = GetCustomModContentManager(resolvedRoot);
                        var intDict = cm.Load<Dictionary<int, string>>(localizedRelativePath);
                        if (intDict != null)
                        {
                            var stringDict = new Dictionary<string, string>(intDict.Count, StringComparer.OrdinalIgnoreCase);
                            foreach (var kvp in intDict)
                            {
                                stringDict[kvp.Key.ToString()] = kvp.Value;
                            }
                            return stringDict;
                        }
                    }
                    catch (ContentLoadException)
                    {
                        // Ignore and fall through
                    }
                }
            }
        }

        // 2. Explicit locale content root
        if (!LanguageHelper.IsBuiltInLanguage(langCode))
        {
            ContentManager? modManager = GetModContentManager();
            if (modManager != null && !string.IsNullOrEmpty(_modContentRoot))
            {
                string viRelativePath = $"{assetSubpath}_{langCode}";
                string viFilePath = Path.Combine(_modContentRoot, $"{viRelativePath}.xnb");
                if (File.Exists(viFilePath))
                {
                    try
                    {
                        var intDict = modManager.Load<Dictionary<int, string>>(viRelativePath);
                        if (intDict != null)
                        {
                            var stringDict = new Dictionary<string, string>(intDict.Count, StringComparer.OrdinalIgnoreCase);
                            foreach (var kvp in intDict)
                            {
                                stringDict[kvp.Key.ToString()] = kvp.Value;
                            }
                            return stringDict;
                        }
                    }
                    catch (ContentLoadException)
                    {
                        // Ignore and return null
                    }
                }
            }
            return null;
        }

        // 3. Built-in languages
        ContentManager? vanillaManager = GetVanillaContentManager();
        if (vanillaManager == null)
            return null;

        if (!LanguageHelper.IsBaseOrEnglish(langCode))
        {
            string? suffix = LanguageHelper.GetLanguageSuffix(langCode);
            if (!string.IsNullOrEmpty(suffix))
            {
                string localizedRelativePath = $"{assetSubpath}.{suffix}";
                string localizedFilePath = Path.Combine(_gameContentRoot, $"{localizedRelativePath}.xnb");
                if (File.Exists(localizedFilePath))
                {
                    try
                    {
                        var intDict = vanillaManager.Load<Dictionary<int, string>>(localizedRelativePath);
                        if (intDict != null)
                        {
                            var stringDict = new Dictionary<string, string>(intDict.Count, StringComparer.OrdinalIgnoreCase);
                            foreach (var kvp in intDict)
                            {
                                stringDict[kvp.Key.ToString()] = kvp.Value;
                            }
                            return stringDict;
                        }
                    }
                    catch (ContentLoadException)
                    {
                        // Ignore and return null
                    }
                }
            }
            return null;
        }

        // 4. Default / English
        string basePath = Path.Combine(_gameContentRoot, $"{assetSubpath}.xnb");
        if (File.Exists(basePath))
        {
            try
            {
                var intDict = vanillaManager.Load<Dictionary<int, string>>(assetSubpath);
                if (intDict != null)
                {
                    var stringDict = new Dictionary<string, string>(intDict.Count, StringComparer.OrdinalIgnoreCase);
                    foreach (var kvp in intDict)
                    {
                        stringDict[kvp.Key.ToString()] = kvp.Value;
                    }
                    return stringDict;
                }
            }
            catch (ContentLoadException)
            {
                // Ignore and return null
            }
        }

        return null;
    }

    private ContentManager? GetVanillaContentManager()
    {
        if (_vanillaContentManager != null)
            return _vanillaContentManager;

        IServiceProvider? sp = _getServiceProvider();
        if (sp == null)
            return null;

        _vanillaContentManager = new ContentManager(sp, _gameContentRoot);
        return _vanillaContentManager;
    }

    private ContentManager? GetModContentManager()
    {
        if (_modContentManager != null)
            return _modContentManager;

        if (string.IsNullOrEmpty(_modContentRoot))
            return null;

        IServiceProvider? sp = _getServiceProvider();
        if (sp == null)
            return null;

        _modContentManager = new ContentManager(sp, _modContentRoot);
        return _modContentManager;
    }

    private ContentManager GetCustomModContentManager(string contentRoot)
    {
        if (_customModContentManagers.TryGetValue(contentRoot, out ContentManager? cm))
            return cm;

        IServiceProvider sp = _getServiceProvider() ?? throw new InvalidOperationException("Service provider unavailable for content manager.");
        cm = new ContentManager(sp, contentRoot);
        _customModContentManagers[contentRoot] = cm;
        return cm;
    }

    private void TriggerWarningOnce(string requested, string fallback)
    {
        if (_warned)
            return;

        _warned = true;
        _onFallbackWarning?.Invoke(requested, fallback);
    }
}
