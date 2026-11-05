using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace UntranslatedStardew.Core;

/// <summary>
/// Stores and resolves external translations contributed by Content Patcher content packs
/// (via the Mods/GiaHung.UntranslatedStardew/Names custom asset) and C# mods.
/// Supports SMAPI-style locale fallbacks: exact locale -> parent locale -> default.
/// If multiple C# mods register conflicting translations, last registration wins deterministically.
/// </summary>
public sealed class ExternalTranslationRegistry
{
    private sealed class ModRegistrationEntry
    {
        public long Generation { get; }
        public string SourceModId { get; }
        public Dictionary<string, string> Translations { get; }

        public ModRegistrationEntry(long generation, string sourceModId, Dictionary<string, string> translations)
        {
            Generation = generation;
            SourceModId = sourceModId;
            Translations = translations;
        }
    }

    private readonly object _syncLock = new();
    private readonly Dictionary<string, ModRegistrationEntry> _modRegistrations = new(StringComparer.OrdinalIgnoreCase);
    private ModRegistrationEntry[] _orderedModRegistrations = Array.Empty<ModRegistrationEntry>();
    private Dictionary<string, string> _cpEntries = new(StringComparer.OrdinalIgnoreCase);
    private long _generationCounter;

    private readonly ConcurrentDictionary<string, string?> _resolvedCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Event fired whenever any external translation is registered, unregistered, or reloaded.
    /// Allows ItemNameResolver to immediately invalidate its own cache.
    /// </summary>
    public event Action? OnChanged;

    public void LoadFromContentPackDictionary(IDictionary<string, string>? entries)
    {
        lock (_syncLock)
        {
            _cpEntries = entries != null
                ? new Dictionary<string, string>(entries, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _resolvedCache.Clear();
        }
        OnChanged?.Invoke();
    }

    public IDisposable RegisterModTranslations(string sourceModId, IDictionary<string, string>? translations)
    {
        if (string.IsNullOrWhiteSpace(sourceModId))
            throw new ArgumentException("Mod ID cannot be null or empty.", nameof(sourceModId));

        long gen;
        lock (_syncLock)
        {
            gen = ++_generationCounter;
            if (translations != null && translations.Count > 0)
            {
                var dict = new Dictionary<string, string>(translations, StringComparer.OrdinalIgnoreCase);
                _modRegistrations[sourceModId] = new ModRegistrationEntry(gen, sourceModId, dict);
            }
            else
            {
                _modRegistrations.Remove(sourceModId);
            }

            _orderedModRegistrations = _modRegistrations.Values.OrderByDescending(e => e.Generation).ToArray();
            _resolvedCache.Clear();
        }

        OnChanged?.Invoke();
        return new RegistrationToken(this, sourceModId, gen);
    }

    public void RegisterModTranslation(string sourceModId, string category, string id, string language, string translatedName)
    {
        if (string.IsNullOrWhiteSpace(sourceModId) || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(translatedName))
            return;

        string normalizedCategory = NormalizeCategory(category);
        string normalizedLang = string.IsNullOrWhiteSpace(language) ? "default" : language.Trim();
        string key = $"{normalizedCategory}/{id.Trim()}@{normalizedLang}";

        lock (_syncLock)
        {
            long gen = ++_generationCounter;
            Dictionary<string, string> dict;
            if (_modRegistrations.TryGetValue(sourceModId, out var existing))
            {
                dict = new Dictionary<string, string>(existing.Translations, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            dict[key] = translatedName;
            _modRegistrations[sourceModId] = new ModRegistrationEntry(gen, sourceModId, dict);
            _orderedModRegistrations = _modRegistrations.Values.OrderByDescending(e => e.Generation).ToArray();
            _resolvedCache.Clear();
        }

        OnChanged?.Invoke();
    }

    public bool UnregisterModTranslations(string sourceModId)
    {
        if (string.IsNullOrWhiteSpace(sourceModId))
            return false;

        bool removed;
        lock (_syncLock)
        {
            removed = _modRegistrations.Remove(sourceModId);
            if (removed)
            {
                _orderedModRegistrations = _modRegistrations.Values.OrderByDescending(e => e.Generation).ToArray();
                _resolvedCache.Clear();
            }
        }

        if (removed)
        {
            OnChanged?.Invoke();
        }
        return removed;
    }

    internal bool UnregisterToken(string sourceModId, long generation)
    {
        bool removed = false;
        lock (_syncLock)
        {
            if (_modRegistrations.TryGetValue(sourceModId, out var current) && current.Generation == generation)
            {
                removed = _modRegistrations.Remove(sourceModId);
                if (removed)
                {
                    _orderedModRegistrations = _modRegistrations.Values.OrderByDescending(e => e.Generation).ToArray();
                    _resolvedCache.Clear();
                }
            }
        }

        if (removed)
        {
            OnChanged?.Invoke();
        }
        return removed;
    }

    public void Invalidate()
    {
        _resolvedCache.Clear();
        OnChanged?.Invoke();
    }

    public void Clear()
    {
        lock (_syncLock)
        {
            _cpEntries.Clear();
            _modRegistrations.Clear();
            _orderedModRegistrations = Array.Empty<ModRegistrationEntry>();
            _resolvedCache.Clear();
        }
        OnChanged?.Invoke();
    }

    public string? GetTranslation(string category, string id, string targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        string normalizedCategory = NormalizeCategory(category);
        string normalizedLang = string.IsNullOrWhiteSpace(targetLanguage) ? "default" : targetLanguage.Trim();
        string cacheKey = $"{normalizedCategory}:{id}@{normalizedLang}";

        if (_resolvedCache.TryGetValue(cacheKey, out string? cached))
            return cached;

        string? resolved = ResolveInternal(normalizedCategory, id.Trim(), normalizedLang);
        _resolvedCache[cacheKey] = resolved;
        return resolved;
    }

    private string? ResolveInternal(string category, string id, string targetLanguage)
    {
        // 1. Check C# mod registrations first in deterministic order (last registration wins)
        ModRegistrationEntry[] mods = _orderedModRegistrations;
        foreach (var modEntry in mods)
        {
            string? found = TryLookupInDict(modEntry.Translations, category, id, targetLanguage);
            if (!string.IsNullOrWhiteSpace(found))
                return found;
        }

        // 2. Check Content Patcher entries
        Dictionary<string, string> cp = _cpEntries;
        string? cpFound = TryLookupInDict(cp, category, id, targetLanguage);
        if (!string.IsNullOrWhiteSpace(cpFound))
            return cpFound;

        return null;
    }

    public static readonly string[] KnownQualifiers =
    {
        "(O)", "(BC)", "(B)", "(FL)", "(F)", "(H)", "(M)", "(P)", "(S)", "(T)", "(TR)", "(WP)", "(W)"
    };

    private static string? TryLookupInDict(Dictionary<string, string> dict, string category, string id, string targetLanguage)
    {
        if (dict.Count == 0)
            return null;

        List<string> candidateLanguages = GetCandidateLanguages(targetLanguage);

        foreach (string lang in candidateLanguages)
        {
            // Format: "{Category}/{Id}@{Language}"
            if (dict.TryGetValue($"{category}/{id}@{lang}", out string? val) && !string.IsNullOrWhiteSpace(val))
                return val.Trim();

            // Format: "{Category}:{Id}@{Language}"
            if (dict.TryGetValue($"{category}:{id}@{lang}", out val) && !string.IsNullOrWhiteSpace(val))
                return val.Trim();

            // If lang == "default", also allow omitted language suffix
            if (lang.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                if (dict.TryGetValue($"{category}/{id}", out val) && !string.IsNullOrWhiteSpace(val))
                    return val.Trim();

                if (dict.TryGetValue($"{category}:{id}", out val) && !string.IsNullOrWhiteSpace(val))
                    return val.Trim();

                if (category.Equals("Item", StringComparison.OrdinalIgnoreCase))
                {
                    if (dict.TryGetValue(id, out val) && !string.IsNullOrWhiteSpace(val))
                        return val.Trim();
                }
            }

            // For items, allow qualified/unqualified variations for standard objects without cross-type alias collisions.
            // Specifically, (WP) and (FL) IDs must never alias to/from unqualified object IDs.
            if (category.Equals("Item", StringComparison.OrdinalIgnoreCase))
            {
                if (id.StartsWith("(") && id.IndexOf(')') > 0)
                {
                    string prefix = id.Substring(0, id.IndexOf(')') + 1);
                    string unq = id.Substring(id.IndexOf(')') + 1);

                    // Only allow unqualified alias if prefix is "(O)" (standard objects)
                    // (WP), (FL), (BC), (B), (W), etc. MUST NOT alias to unqualified "0", "1", etc.
                    if (!prefix.Equals("(WP)", StringComparison.OrdinalIgnoreCase) && !prefix.Equals("(FL)", StringComparison.OrdinalIgnoreCase))
                    {
                        if (dict.TryGetValue($"Item/{unq}@{lang}", out val) && !string.IsNullOrWhiteSpace(val))
                            return val.Trim();

                        if (dict.TryGetValue($"Item:{unq}@{lang}", out val) && !string.IsNullOrWhiteSpace(val))
                            return val.Trim();

                        if (lang.Equals("default", StringComparison.OrdinalIgnoreCase))
                        {
                            if (dict.TryGetValue($"Item/{unq}", out val) && !string.IsNullOrWhiteSpace(val))
                                return val.Trim();

                            if (dict.TryGetValue($"Item:{unq}", out val) && !string.IsNullOrWhiteSpace(val))
                                return val.Trim();
                        }
                    }
                }
                else
                {
                    string? match = null;
                    foreach (string q in KnownQualifiers)
                    {
                        if (q == "(WP)" || q == "(FL)") continue;
                        string? candidate = null;
                        if (dict.TryGetValue($"Item/{q}{id}@{lang}", out val) || dict.TryGetValue($"Item:{q}{id}@{lang}", out val)) candidate = val;
                        if (string.IsNullOrWhiteSpace(candidate) && lang.Equals("default", StringComparison.OrdinalIgnoreCase)
                            && (dict.TryGetValue($"Item/{q}{id}", out val) || dict.TryGetValue($"Item:{q}{id}", out val))) candidate = val;
                        if (string.IsNullOrWhiteSpace(candidate)) continue;
                        if (q == "(O)") return candidate.Trim();
                        if (match != null) return null; // ambiguous legacy ID: require qualification
                        match = candidate.Trim();
                    }
                    if (match != null) return match;
                }
            }
        }

        return null;
    }

    private static List<string> GetCandidateLanguages(string targetLanguage)
    {
        var list = new List<string>();
        string lower = targetLanguage.ToLowerInvariant().Trim();

        // If the caller explicitly requested "default", only search "default"
        if (lower.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            list.Add("default");
            return list;
        }

        // 1. Exact locale (e.g. "zh-cn", "fr-fr", "en-us")
        list.Add(lower);

        // 2. Canonical language code if different (e.g. "zh" for "zh-cn", "en" for "en-us")
        string canonical = LanguageHelper.CanonicalizeLanguageCode(lower);
        if (!list.Contains(canonical))
            list.Add(canonical);

        // 3. Parent locale (e.g. "fr-fr" -> "fr", "zh-cn" -> "zh")
        int hyphenIdx = lower.IndexOf('-');
        if (hyphenIdx > 0)
        {
            string parent = lower.Substring(0, hyphenIdx);
            if (!list.Contains(parent))
                list.Add(parent);
        }

        // 4. "default" fallback according to SMAPI Translator semantics.
        // SMAPI Translator.GetRaw falls back to default for ANY requested locale (including "en"),
        // because "default" represents the mod's base/unlocalized dictionary.
        // Note: The base fallback translation in default.json may be non-English (e.g. French/Chinese).
        // We do NOT fall back to "en" for other languages (e.g. fr -> default, NOT fr -> en).
        if (!list.Contains("default"))
        {
            list.Add("default");
        }

        return list;
    }

    public static string NormalizeCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return "Item";

        string c = category.Trim();
        if (c.StartsWith("(", StringComparison.Ordinal))
            return "Item";

        if (c.Equals("Item", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("Items", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("Object", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("Weapon", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("BigCraftable", StringComparison.OrdinalIgnoreCase))
            return "Item";

        if (c.Equals("Mail", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("Letter", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("Letters", StringComparison.OrdinalIgnoreCase))
            return "Mail";

        if (c.Equals("Power", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("Powers", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("SpecialPower", StringComparison.OrdinalIgnoreCase))
            return "Power";

        if (c.Equals("Achievement", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("Achievements", StringComparison.OrdinalIgnoreCase))
            return "Achievement";

        if (c.Equals("SecretNote", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("SecretNotes", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("Note", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("Notes", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("Journal", StringComparison.OrdinalIgnoreCase))
            return "SecretNote";

        if (c.Equals("SideTab", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("SideTabs", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("Tab", StringComparison.OrdinalIgnoreCase))
            return "SideTab";

        return c;
    }

    private sealed class RegistrationToken : IDisposable
    {
        private readonly ExternalTranslationRegistry _registry;
        private readonly string _sourceModId;
        private readonly long _generation;
        private int _disposed;

        public RegistrationToken(ExternalTranslationRegistry registry, string sourceModId, long generation)
        {
            _registry = registry;
            _sourceModId = sourceModId;
            _generation = generation;
        }

        public void Dispose()
        {
            if (System.Threading.Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _registry.UnregisterToken(_sourceModId, _generation);
            }
        }
    }
}
