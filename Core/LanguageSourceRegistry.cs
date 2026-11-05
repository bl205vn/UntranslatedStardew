using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UntranslatedStardew.Core;

/// <summary>
/// Central registry for declarative language sources (both built-in and custom language packs).
/// Replaces brittle hardcoded per-locale provider switches with generic descriptors.
/// Enforces path boundary verification, deterministic generation order, and immediate cache invalidation.
/// </summary>
public sealed class LanguageSourceRegistry
{
    private sealed class RegistrationToken : IDisposable
    {
        private readonly LanguageSourceRegistry _registry;
        private readonly string _sourceModId;
        private readonly string _languageCode;
        private readonly long _generation;
        private int _disposed;

        public RegistrationToken(LanguageSourceRegistry registry, string sourceModId, string languageCode, long generation)
        {
            _registry = registry;
            _sourceModId = sourceModId;
            _languageCode = languageCode;
            _generation = generation;
        }

        public void Dispose()
        {
            if (System.Threading.Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _registry.UnregisterToken(_sourceModId, _languageCode, _generation);
            }
        }
    }

    private readonly object _syncLock = new();
    private readonly Dictionary<string, List<LanguageSourceDescriptor>> _sourcesByLanguage = new(StringComparer.OrdinalIgnoreCase);
    private long _generationCounter;

    /// <summary>
    /// Event fired whenever any language source is registered, unregistered, or reloaded.
    /// Immediately triggers invalidation of translation and font caches.
    /// </summary>
    public event Action? OnChanged;

    /// <summary>
    /// Optional resolver converting SMAPI Mod UniqueIDs to physical directories on disk.
    /// Used for resolving relative ContentRoot paths against the providing mod folder.
    /// </summary>
    public Func<string, string?>? ModDirectoryResolver { get; set; }

    /// <summary>
    /// Registers a custom language source. Returns an IDisposable token to safely unregister.
    /// Deterministic: last registration wins.
    /// </summary>
    public IDisposable Register(LanguageSourceDescriptor descriptor, LanguageSourceOrigin origin = LanguageSourceOrigin.Api)
    {
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));
        if (string.IsNullOrWhiteSpace(descriptor.LanguageCode))
            throw new ArgumentException("LanguageCode cannot be null or empty.", nameof(descriptor));
        if (string.IsNullOrWhiteSpace(descriptor.SourceModId))
            throw new ArgumentException("SourceModId cannot be null or empty.", nameof(descriptor));

        LanguageSourceDescriptor clone = descriptor.Clone();
        clone.Origin = origin;

        string canonicalLang = LanguageHelper.CanonicalizeLanguageCode(clone.LanguageCode);
        long gen;

        lock (_syncLock)
        {
            gen = ++_generationCounter;
            clone.Generation = gen;
            clone.LanguageCode = canonicalLang;

            if (!_sourcesByLanguage.TryGetValue(canonicalLang, out var list))
            {
                list = new List<LanguageSourceDescriptor>();
                _sourcesByLanguage[canonicalLang] = list;
            }

            // Remove any existing entry from the same source mod and origin for this language
            list.RemoveAll(s => s.SourceModId.Equals(clone.SourceModId, StringComparison.OrdinalIgnoreCase) && s.Origin == origin);
            list.Add(clone);

        }

        OnChanged?.Invoke();
        return new RegistrationToken(this, clone.SourceModId, canonicalLang, gen);
    }

    /// <summary>
    /// Unregisters all language sources registered by a given mod ID.
    /// </summary>
    public bool Unregister(string sourceModId, string? languageCode = null)
    {
        if (string.IsNullOrWhiteSpace(sourceModId))
            return false;

        bool removed = false;
        string? canonicalLang = !string.IsNullOrWhiteSpace(languageCode)
            ? LanguageHelper.CanonicalizeLanguageCode(languageCode)
            : null;

        lock (_syncLock)
        {
            if (canonicalLang != null)
            {
                if (_sourcesByLanguage.TryGetValue(canonicalLang, out var list))
                {
                    int before = list.Count;
                    list.RemoveAll(s => s.SourceModId.Equals(sourceModId, StringComparison.OrdinalIgnoreCase));
                    if (list.Count != before)
                    {
                        removed = true;
                        if (list.Count == 0)
                            _sourcesByLanguage.Remove(canonicalLang);
                    }
                }
            }
            else
            {
                foreach (var kvp in _sourcesByLanguage.ToList())
                {
                    int before = kvp.Value.Count;
                    kvp.Value.RemoveAll(s => s.SourceModId.Equals(sourceModId, StringComparison.OrdinalIgnoreCase));
                    if (kvp.Value.Count != before)
                    {
                        removed = true;
                        if (kvp.Value.Count == 0)
                            _sourcesByLanguage.Remove(kvp.Key);
                    }
                }
            }

        }

        if (removed)
        {
            OnChanged?.Invoke();
        }

        return removed;
    }

    private void UnregisterToken(string sourceModId, string canonicalLang, long generation)
    {
        bool removed = false;
        lock (_syncLock)
        {
            if (_sourcesByLanguage.TryGetValue(canonicalLang, out var list))
            {
                int count = list.RemoveAll(s => s.SourceModId.Equals(sourceModId, StringComparison.OrdinalIgnoreCase) && s.Generation == generation);
                if (count > 0)
                {
                    removed = true;
                    if (list.Count == 0)
                        _sourcesByLanguage.Remove(canonicalLang);
                }
            }
        }

        if (removed)
        {
            OnChanged?.Invoke();
        }
    }

    /// <summary>
    /// Attempts to retrieve the effective language source descriptor for a language code.
    /// Returns a defensive clone of the descriptor with the highest generation number.
    /// </summary>
    public bool TryGetSource(string? languageCode, out LanguageSourceDescriptor? descriptor)
    {
        descriptor = null;
        if (string.IsNullOrWhiteSpace(languageCode))
            return false;

        string canonical = LanguageHelper.CanonicalizeLanguageCode(languageCode);
        lock (_syncLock)
        {
            if (_sourcesByLanguage.TryGetValue(canonical, out var list) && list.Count > 0)
            {
                descriptor = list.OrderBy(s => s.DetectedFromGame).ThenByDescending(s => s.Generation).First();
                descriptor = descriptor.Clone();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Validates that a target relative path stays strictly within the specified base directory,
    /// rejecting directory traversal attempts (e.g. "../" escape).
    /// </summary>
    public static bool TryResolveSafePath(string baseDirectory, string relativePath, out string safeFullPath)
    {
        safeFullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(baseDirectory) || string.IsNullOrWhiteSpace(relativePath))
            return false;

        try
        {
            string fullBase = Path.GetFullPath(baseDirectory);
            if (!fullBase.EndsWith(Path.DirectorySeparatorChar.ToString()) && !fullBase.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                fullBase += Path.DirectorySeparatorChar;
            }

            string combined = Path.GetFullPath(Path.Combine(baseDirectory, relativePath));
            if (combined.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase) || combined.Equals(fullBase.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            {
                safeFullPath = combined;
                return true;
            }
        }
        catch
        {
            // Invalid path characters or path formatting
        }

        return false;
    }

    /// <summary>
    /// Safely resolves a descriptor's ContentRoot against its providing mod directory or explicit absolute path,
    /// verifying boundaries and directory existence.
    /// </summary>
    public bool TryResolveSourceContentRoot(LanguageSourceDescriptor desc, out string resolvedRoot)
    {
        resolvedRoot = string.Empty;
        if (desc == null || string.IsNullOrWhiteSpace(desc.ContentRoot))
            return false;

        // 1. Valid rooted/absolute path
        if (Path.IsPathRooted(desc.ContentRoot))
        {
            if (Directory.Exists(desc.ContentRoot))
            {
                resolvedRoot = Path.GetFullPath(desc.ContentRoot);
                return true;
            }
            return false;
        }

        // 2. Relative to providing mod folder through SMAPI mod registry resolver
        if (ModDirectoryResolver != null && !string.IsNullOrWhiteSpace(desc.SourceModId))
        {
            string? ownerDir = ModDirectoryResolver(desc.SourceModId);
            if (!string.IsNullOrEmpty(ownerDir) && TryResolveSafePath(ownerDir, desc.ContentRoot, out string safeModPath))
            {
                if (Directory.Exists(safeModPath))
                {
                    resolvedRoot = safeModPath;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Loads source entries from the Content Patcher dictionary asset Mods/GiaHung.UntranslatedStardew/LanguageSources.
    /// Completely replaces the prior asset dictionary snapshot while preserving API and bundled registrations.
    /// </summary>
    public void LoadFromAssetDictionary(IDictionary<string, LanguageSourceDescriptor>? entries)
    {
        lock (_syncLock)
        {
            // Remove all previous asset dictionary entries regardless of SourceModId
            foreach (var kvp in _sourcesByLanguage.ToList())
            {
                kvp.Value.RemoveAll(s => s.Origin == LanguageSourceOrigin.AssetDictionary);
                if (kvp.Value.Count == 0)
                {
                    _sourcesByLanguage.Remove(kvp.Key);
                }
            }

            if (entries != null)
            {
                foreach (var kvp in entries)
                {
                    LanguageSourceDescriptor rawDesc = kvp.Value;
                    if (rawDesc == null)
                        continue;

                    LanguageSourceDescriptor desc = rawDesc.Clone();
                    desc.Origin = LanguageSourceOrigin.AssetDictionary;

                    if (string.IsNullOrWhiteSpace(desc.LanguageCode))
                    {
                        desc.LanguageCode = kvp.Key;
                    }
                    if (string.IsNullOrWhiteSpace(desc.SourceModId))
                    {
                        desc.SourceModId = "ContentPatcher";
                    }

                    string canonical = LanguageHelper.CanonicalizeLanguageCode(desc.LanguageCode);
                    desc.LanguageCode = canonical;
                    desc.Generation = ++_generationCounter;

                    if (!_sourcesByLanguage.TryGetValue(canonical, out var list))
                    {
                        list = new List<LanguageSourceDescriptor>();
                        _sourcesByLanguage[canonical] = list;
                    }
                    list.Add(desc);
                }
            }

        }

        OnChanged?.Invoke();
    }

    /// <summary>
    /// Loads source entries discovered from Data/AdditionalLanguages.
    /// Completely replaces the prior AdditionalLanguages snapshot while preserving API and bundled registrations.
    /// </summary>
    public void LoadFromAdditionalLanguages(IEnumerable<LanguageSourceDescriptor>? entries)
    {
        lock (_syncLock)
        {
            // Remove all previous AdditionalLanguages entries regardless of SourceModId
            foreach (var kvp in _sourcesByLanguage.ToList())
            {
                kvp.Value.RemoveAll(s => s.Origin == LanguageSourceOrigin.AdditionalLanguages);
                if (kvp.Value.Count == 0)
                {
                    _sourcesByLanguage.Remove(kvp.Key);
                }
            }

            if (entries != null)
            {
                foreach (var rawDesc in entries)
                {
                    if (rawDesc == null)
                        continue;

                    LanguageSourceDescriptor desc = rawDesc.Clone();
                    desc.Origin = LanguageSourceOrigin.AdditionalLanguages;

                    string canonical = LanguageHelper.CanonicalizeLanguageCode(desc.LanguageCode);
                    desc.LanguageCode = canonical;
                    desc.Generation = ++_generationCounter;

                    if (!_sourcesByLanguage.TryGetValue(canonical, out var list))
                    {
                        list = new List<LanguageSourceDescriptor>();
                        _sourcesByLanguage[canonical] = list;
                    }
                    list.Add(desc);
                }
            }

        }

        OnChanged?.Invoke();
    }

    /// <summary>
    /// Clears all registered sources and cached resolutions.
    /// </summary>
    public void Clear()
    {
        lock (_syncLock)
        {
            _sourcesByLanguage.Clear();
        }
        OnChanged?.Invoke();
    }
}
