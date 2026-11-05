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
/// Supports per-key fallback to English and lazy service provider initialization.
/// </summary>
public sealed class RawContentProvider : ISecondaryContentProvider, ITrinketDataProvider, IPowerDataProvider
{
    private readonly Func<IServiceProvider?> _getServiceProvider;
    private readonly string _gameContentRoot;
    private readonly string? _modContentRoot;
    private readonly Action<string, string>? _onFallbackWarning;

    private ContentManager? _vanillaContentManager;
    private ContentManager? _modContentManager;

    private readonly ConcurrentDictionary<string, Dictionary<string, string>?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Dictionary<string, TrinketData>?> _trinketDataCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Dictionary<string, StardewValley.GameData.Powers.PowersData>?> _powersDataCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Dictionary<string, StardewValley.GameData.Objects.ObjectData>?> _objectDataCache = new(StringComparer.OrdinalIgnoreCase);
    private bool _warned;
    private bool _disposed;

    public string RequestedLanguage { get; }
    public string EffectiveLanguage { get; }

    public RawContentProvider(
        Func<IServiceProvider?> getServiceProvider,
        string gameContentRoot,
        string? requestedLanguage,
        string? modContentRoot = null,
        Action<string, string>? onFallbackWarning = null)
    {
        _getServiceProvider = getServiceProvider ?? (() => null);
        _gameContentRoot = gameContentRoot;
        _modContentRoot = modContentRoot;
        _onFallbackWarning = onFallbackWarning;

        RequestedLanguage = string.IsNullOrWhiteSpace(requestedLanguage)
            ? "default"
            : requestedLanguage.Trim();

        string canonical = LanguageHelper.CanonicalizeLanguageCode(RequestedLanguage);

        // Determine effective language and check availability
        if (canonical == "default")
        {
            EffectiveLanguage = "default";
        }
        else if (canonical == "en")
        {
            EffectiveLanguage = "en";
        }
        else if (LanguageHelper.IsVietnamese(canonical))
        {
            if (!string.IsNullOrEmpty(_modContentRoot) && Directory.Exists(_modContentRoot))
            {
                EffectiveLanguage = "vi";
            }
            else
            {
                EffectiveLanguage = "default";
                TriggerWarningOnce(RequestedLanguage, "default");
            }
        }
        else if (LanguageHelper.IsBuiltInLanguage(canonical))
        {
            EffectiveLanguage = canonical;
        }
        else
        {
            EffectiveLanguage = "default";
            TriggerWarningOnce(RequestedLanguage, "default");
        }
    }

    public RawContentProvider(
        IServiceProvider? serviceProvider,
        string gameContentRoot,
        string? requestedLanguage,
        string? modContentRoot = null,
        Action<string, string>? onFallbackWarning = null)
        : this(() => serviceProvider, gameContentRoot, requestedLanguage, modContentRoot, onFallbackWarning)
    {
    }

    public bool TryGetString(string assetSubpath, string key, out string? value)
    {
        value = null;
        if (_disposed || string.IsNullOrEmpty(assetSubpath) || string.IsNullOrEmpty(key))
            return false;

        string normalizedPath = NormalizeAssetPath(assetSubpath);

        // 1. Try localized dictionary if not base / English
        if (!LanguageHelper.IsBaseOrEnglish(EffectiveLanguage))
        {
            Dictionary<string, string>? localizedDict = GetOrLoadDictionary(normalizedPath, EffectiveLanguage);
            if (localizedDict != null && localizedDict.TryGetValue(key, out string? locVal) && !string.IsNullOrWhiteSpace(locVal))
            {
                value = locVal;
                return true;
            }
        }

        // 2. Per-key fallback: Check base default/English dictionary
        Dictionary<string, string>? baseDict = GetOrLoadDictionary(normalizedPath, LanguageHelper.IsBaseOrEnglish(EffectiveLanguage) ? EffectiveLanguage : "default");
        if (baseDict != null && baseDict.TryGetValue(key, out string? engVal) && !string.IsNullOrWhiteSpace(engVal))
        {
            value = engVal;
            return true;
        }

        return false;
    }

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
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cache.Clear();
        _trinketDataCache.Clear();
        _powersDataCache.Clear();
        _objectDataCache.Clear();
        _vanillaContentManager?.Dispose();
        _modContentManager?.Dispose();
        _vanillaContentManager = null;
        _modContentManager = null;
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

    private Dictionary<string, string>? LoadDictionaryFromDisk(string assetSubpath, string langCode)
    {
        if (assetSubpath.Equals("Data/Achievements", StringComparison.OrdinalIgnoreCase) ||
            assetSubpath.Equals("Data/SecretNotes", StringComparison.OrdinalIgnoreCase))
        {
            return LoadIntStringDictionaryAsStrings(assetSubpath, langCode);
        }

        if (langCode == "vi")
        {
            ContentManager? modManager = GetModContentManager();
            if (modManager != null && !string.IsNullOrEmpty(_modContentRoot))
            {
                string viRelativePath = $"{assetSubpath}_vi";
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

        // Vanilla base / default / English (unsuffixed file)
        string engFilePath = Path.Combine(_gameContentRoot, $"{assetSubpath}.xnb");
        if (File.Exists(engFilePath))
        {
            try
            {
                return vanillaManager.Load<Dictionary<string, string>>(assetSubpath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOAD ERROR] {assetSubpath}: {ex}");
                // Ignore and return null
            }
        }

        return null;
    }

    private Dictionary<string, string>? LoadIntStringDictionaryAsStrings(string assetSubpath, string langCode)
    {
        ContentManager? manager = langCode == "vi" ? GetModContentManager() : GetVanillaContentManager();
        if (manager == null)
            return null;

        string relPath = assetSubpath;
        if (langCode == "vi")
        {
            relPath = $"{assetSubpath}_vi";
            if (string.IsNullOrEmpty(_modContentRoot) || !File.Exists(Path.Combine(_modContentRoot, $"{relPath}.xnb")))
                return null;
        }
        else if (!LanguageHelper.IsBaseOrEnglish(langCode))
        {
            string? suffix = LanguageHelper.GetLanguageSuffix(langCode);
            if (string.IsNullOrEmpty(suffix))
                return null;
            relPath = $"{assetSubpath}.{suffix}";
            if (!File.Exists(Path.Combine(_gameContentRoot, $"{relPath}.xnb")))
                return null;
        }
        else
        {
            if (!File.Exists(Path.Combine(_gameContentRoot, $"{relPath}.xnb")))
                return null;
        }

        try
        {
            Dictionary<int, string>? intDict = manager.Load<Dictionary<int, string>>(relPath);
            if (intDict != null)
            {
                var res = new Dictionary<string, string>(intDict.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in intDict)
                    res[kvp.Key.ToString()] = kvp.Value;
                return res;
            }
        }
        catch (Exception)
        {
            try
            {
                return manager.Load<Dictionary<string, string>>(relPath);
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        return null;
    }

    private ContentManager? GetVanillaContentManager()
    {
        if (_vanillaContentManager != null)
            return _vanillaContentManager;

        IServiceProvider? sp = _getServiceProvider();
        if (sp != null && Directory.Exists(_gameContentRoot))
        {
            _vanillaContentManager = new ContentManager(sp, _gameContentRoot);
        }

        return _vanillaContentManager;
    }

    private ContentManager? GetModContentManager()
    {
        if (_modContentManager != null)
            return _modContentManager;

        IServiceProvider? sp = _getServiceProvider();
        if (sp != null && !string.IsNullOrEmpty(_modContentRoot) && Directory.Exists(_modContentRoot))
        {
            _modContentManager = new ContentManager(sp, _modContentRoot);
        }

        return _modContentManager;
    }

    private void TriggerWarningOnce(string requested, string fallback)
    {
        if (!_warned)
        {
            _warned = true;
            _onFallbackWarning?.Invoke(requested, fallback);
        }
    }
}
