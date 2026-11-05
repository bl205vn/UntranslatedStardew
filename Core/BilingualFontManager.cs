using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>
/// Manages secondary font asset loading with isolated MonoGame ContentManager instances.
/// Does not mutate LocalizedContentManager static locale cache or CurrentLanguageCode.
/// Preserves Game1 primary font and lazily loads secondary SpriteFonts on demand.
/// Enforces fixed font roles across all built-in and generic custom languages.
/// </summary>
public sealed class BilingualFontManager : IDisposable
{
    public static BilingualFontManager? Instance { get; set; }

    private readonly Func<IServiceProvider?> _getServiceProvider;
    private readonly string _gameContentRoot;
    private readonly string? _modContentRoot;
    private string _effectiveLanguage;
    private readonly LanguageSourceRegistry? _sourceRegistry;
    private readonly Action<char, string, string>? _onMissingGlyphWarning;
    private readonly Action<string>? _onDiagnosticWarning;
    private int _isRebinding;

    private ContentManager? _vanillaFontContentManager;
    private ContentManager? _modFontContentManager;
    private readonly Dictionary<string, ContentManager> _customModContentManagers = new(StringComparer.OrdinalIgnoreCase);

    private SpriteFont? _bitmapFont;
    private Texture2D? _bitmapAtlas;
    private SpriteFont? _secondarySmallFont;
    private SpriteFont? _secondaryDialogueFont;

    private bool _attemptedSmallFont;
    private bool _attemptedDialogueFont;
    private bool _loggedMissingSmallFont;
    private bool _loggedMissingDialogueFont;

    private const int MaxMissingGlyphEntries = 512;
    private readonly ConditionalWeakTable<SpriteFont, HashSet<char>> _charSetCache = new();
    private readonly ConcurrentDictionary<string, byte> _missingGlyphsWarned = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<SpriteFont, SpriteFont> _testSecondaryFonts = new();

    private bool _disposed;

    public string EffectiveLanguage => _effectiveLanguage;
    public bool IsSecondaryAvailable { get; private set; }
    public LanguageSourceRegistry? SourceRegistry => _sourceRegistry;

    public BilingualFontManager(
        Func<IServiceProvider?> getServiceProvider,
        string gameContentRoot,
        string? modContentRoot,
        string effectiveLanguage,
        Action<char, string, string>? onMissingGlyphWarning = null,
        Action<string>? onDiagnosticWarning = null,
        LanguageSourceRegistry? sourceRegistry = null)
    {
        _getServiceProvider = getServiceProvider ?? (() => null);
        _gameContentRoot = gameContentRoot;
        _modContentRoot = modContentRoot;
        _effectiveLanguage = string.IsNullOrWhiteSpace(effectiveLanguage) ? "default" : effectiveLanguage.Trim();
        _onMissingGlyphWarning = onMissingGlyphWarning;
        _onDiagnosticWarning = onDiagnosticWarning;
        _sourceRegistry = sourceRegistry;

        Instance = this;

        Rebind();

        if (_sourceRegistry != null)
        {
            _sourceRegistry.OnChanged += OnSourceRegistryChanged;
        }
    }

    /// <summary>
    /// Rebinds the effective locale and font source safely after source changes, including late registrations and removals.
    /// Resets internal font references and content managers without disposing the instance.
    /// </summary>
    public void Rebind(string? newEffectiveLanguage = null)
    {
        if (_disposed || System.Threading.Interlocked.CompareExchange(ref _isRebinding, 1, 0) != 0)
            return;

        try
        {
            if (newEffectiveLanguage != null)
            {
                _effectiveLanguage = string.IsNullOrWhiteSpace(newEffectiveLanguage) ? "default" : newEffectiveLanguage.Trim();
            }

            string canonical = LanguageHelper.CanonicalizeLanguageCode(_effectiveLanguage);

            if (canonical == "default" || canonical == "en" || LanguageHelper.IsBuiltInLanguage(canonical))
            {
                IsSecondaryAvailable = true;
            }
            else if (_sourceRegistry != null && _sourceRegistry.TryGetSource(canonical, out LanguageSourceDescriptor? source) && source != null)
            {
                IsSecondaryAvailable = true;
            }
            else if (!string.IsNullOrEmpty(_modContentRoot))
            {
                IsSecondaryAvailable = true;
            }
            else
            {
                IsSecondaryAvailable = false;
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

    /// <summary>
    /// Explicitly registers a secondary font for unit testing in headless environments.
    /// </summary>
    public void SetSecondaryFontForTesting(SpriteFont primaryFont, SpriteFont secondaryFont)
    {
        _testSecondaryFonts[primaryFont] = secondaryFont;
    }

    /// <summary>
    /// Checks whether the given SpriteFont contains the specified character.
    /// Uses a cached HashSet for fast O(1) containment checks.
    /// </summary>
    public bool ContainsCharacter(SpriteFont font, char c)
    {
        if (font == null)
            return false;

        HashSet<char> set = _charSetCache.GetValue(font, f =>
        {
            HashSet<char> chars = new(f.Characters.Count);
            foreach (char ch in f.Characters)
            {
                chars.Add(ch);
            }
            return chars;
        });

        return set.Contains(c);
    }

    /// <summary>
    /// Retrieves the matching secondary SpriteFont for the specified primary font.
    /// Enforces fixed font roles: loads unsuffixed font for English/default secondary chunks,
    /// loads locale-suffixed font for built-ins, or loads descriptor font for custom languages.
    /// Returns null only if secondary assets are genuinely absent from disk.
    /// </summary>
    public SpriteFont? GetSecondaryFont(SpriteFont primaryFont)
    {
        if (_disposed || primaryFont == null || !IsSecondaryAvailable)
            return null;

        if (_testSecondaryFonts.TryGetValue(primaryFont, out SpriteFont? testFont))
            return testFont;

        bool isDialogue = IsDialogueFont(primaryFont);

        if (isDialogue)
        {
            if (_secondaryDialogueFont != null)
                return _secondaryDialogueFont;

            if (_attemptedDialogueFont)
                return null;

            _secondaryDialogueFont = LoadSecondaryFontFromDisk("Fonts/SpriteFont1", isDialogue: true);
            return _secondaryDialogueFont;
        }
        else
        {
            if (_secondarySmallFont != null)
                return _secondarySmallFont;

            if (_attemptedSmallFont)
                return null;

            _secondarySmallFont = LoadSecondaryFontFromDisk("Fonts/SmallFont", isDialogue: false);
            return _secondarySmallFont;
        }
    }

    /// <summary>
    /// Emits a localized warning if a glyph is unavailable in both primary and secondary fonts.
    /// Deduplicated strictly per glyph and language (bounded cache) to avoid unbounded keys.
    /// </summary>
    public void ReportMissingGlyphOnce(char c, string sourceContext)
    {
        string key = $"{c}_{(int)c}_{_effectiveLanguage}";
        if (_missingGlyphsWarned.Count < MaxMissingGlyphEntries && _missingGlyphsWarned.TryAdd(key, 1))
        {
            _onMissingGlyphWarning?.Invoke(c, sourceContext, _effectiveLanguage);
        }
    }

    public static bool IsDialogueFont(SpriteFont font)
    {
        if (Game1.dialogueFont != null && ReferenceEquals(font, Game1.dialogueFont))
            return true;

        if (Game1.smallFont != null && ReferenceEquals(font, Game1.smallFont))
            return false;

        // Fallback: dialogueFont in Stardew is typically LineSpacing >= 36
        return font.LineSpacing >= 36;
    }

    private SpriteFont? LoadSecondaryFontFromDisk(string assetBase, bool isDialogue)
    {
        IServiceProvider? sp = _getServiceProvider();
        if (sp == null)
            return null;

        // Mark that an attempt was made with an active service provider
        if (isDialogue)
            _attemptedDialogueFont = true;
        else
            _attemptedSmallFont = true;

        string canonical = LanguageHelper.CanonicalizeLanguageCode(_effectiveLanguage);

        // 1. Check custom language source registry
        if (_sourceRegistry != null && _sourceRegistry.TryGetSource(canonical, out LanguageSourceDescriptor? source) && source != null)
        {
            string? targetFontAsset = isDialogue ? source.DialogueFont : source.SmallFont;
            if (string.IsNullOrWhiteSpace(targetFontAsset) && !string.IsNullOrWhiteSpace(source.AssetPattern))
            {
                targetFontAsset = source.FormatAssetPath(assetBase);
            }

            if (!string.IsNullOrWhiteSpace(targetFontAsset))
            {
                string? root = null;
                if (_sourceRegistry.TryResolveSourceContentRoot(source, out string resolvedRoot) && Directory.Exists(resolvedRoot))
                {
                    root = resolvedRoot;
                }


                if (root != null)
                {
                    string relativePath = targetFontAsset.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                    if (!LanguageSourceRegistry.TryResolveSafePath(root, $"{relativePath}.xnb", out string fullPath))
                        return null;
                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            ContentManager cm = GetCustomModContentManager(sp, root);
                            SpriteFont font = cm.Load<SpriteFont>(targetFontAsset);
                            ApplyFontLineSpacing(font, canonical, isDialogue, source);
                            return font;
                        }
                        catch (Exception ex)
                        {
                            LogMissingFontDiagnostic(assetBase, isDialogue, ex.Message);
                            return null;
                        }
                    }
                }
            }
            try
            {
                if (source.SpriteFontAssets?.TryGetValue(assetBase, out var patch) == true)
                {
                    SpriteFont font = GetCustomModContentManager(sp, patch.Root).Load<SpriteFont>(patch.Asset);
                    if (!isDialogue && source.GameSmallFontLineSpacing > 0)
                        font.LineSpacing = source.GameSmallFontLineSpacing;
                    return font;
                }
                if (!string.IsNullOrWhiteSpace(source.BitmapFontAsset))
                {
                    if (_bitmapFont == null)
                    {
                        (_bitmapFont, _bitmapAtlas) = CustomBitmapFont.Load(source.BitmapFontAsset.Replace('\\', '/'), source.BitmapFontZoom);
                        if (source.GameSmallFontLineSpacing > 0)
                            _bitmapFont.LineSpacing = Math.Max(_bitmapFont.LineSpacing, source.GameSmallFontLineSpacing);
                    }
                    return _bitmapFont;
                }
                if (source.UseGameLatinFont)
                {
                    SpriteFont font = GetVanillaFontContentManager(sp).Load<SpriteFont>(assetBase);
                    if (!isDialogue && source.GameSmallFontLineSpacing > 0)
                        font.LineSpacing = source.GameSmallFontLineSpacing;
                    return font;
                }
            }
            catch (Exception ex)
            {
                LogMissingFontDiagnostic(assetBase, isDialogue, ex.Message);
            }
        }

        // 2. Explicit locale content root
        if (!LanguageHelper.IsBuiltInLanguage(canonical) && !string.IsNullOrEmpty(_modContentRoot))
        {
            if (!string.IsNullOrEmpty(_modContentRoot) && Directory.Exists(_modContentRoot))
            {
                ContentManager modManager = GetModFontContentManager(sp);
                string viAsset = $"{assetBase}_{canonical}";
                string viPath = Path.Combine(_modContentRoot, $"{viAsset}.xnb");

                if (File.Exists(viPath))
                {
                    try
                    {
                        return modManager.Load<SpriteFont>(viAsset);
                    }
                    catch (Exception ex)
                    {
                        LogMissingFontDiagnostic(assetBase, isDialogue, ex.Message);
                        return null;
                    }
                }
            }

            LogMissingFontDiagnostic(assetBase, isDialogue);
            return null;
        }

        // 3. Built-in languages and default / English
        if (LanguageHelper.IsBuiltInLanguage(canonical) || LanguageHelper.IsBaseOrEnglish(canonical))
        {
            if (!Directory.Exists(_gameContentRoot))
            {
                LogMissingFontDiagnostic(assetBase, isDialogue);
                return null;
            }

            ContentManager vanillaManager = GetVanillaFontContentManager(sp);
            string? suffix = LanguageHelper.GetLanguageSuffix(canonical);

            if (!string.IsNullOrEmpty(suffix))
            {
                // Locale-specific asset (e.g. Fonts/SmallFont.ja-JP)
                string localizedAsset = $"{assetBase}.{suffix}";
                string localizedPath = Path.Combine(_gameContentRoot, $"{localizedAsset}.xnb");

                if (File.Exists(localizedPath))
                {
                    try
                    {
                        SpriteFont font = vanillaManager.Load<SpriteFont>(localizedAsset);
                        ApplyFontLineSpacing(font, canonical, isDialogue);
                        return font;
                    }
                    catch (Exception ex)
                    {
                        LogMissingFontDiagnostic(assetBase, isDialogue, ex.Message);
                        return null;
                    }
                }

                // If localized asset doesn't exist (e.g. it-IT uses standard Latin font), fall through to unsuffixed base
            }

            // Unsuffixed asset (Fonts/SmallFont, Fonts/SpriteFont1) for English, default, or Latin built-ins
            string baseAsset = assetBase;
            string basePath = Path.Combine(_gameContentRoot, $"{baseAsset}.xnb");

            if (File.Exists(basePath))
            {
                try
                {
                    SpriteFont font = vanillaManager.Load<SpriteFont>(baseAsset);
                    ApplyFontLineSpacing(font, canonical, isDialogue);
                    return font;
                }
                catch (Exception ex)
                {
                    LogMissingFontDiagnostic(assetBase, isDialogue, ex.Message);
                    return null;
                }
            }

            LogMissingFontDiagnostic(assetBase, isDialogue);
            return null;
        }

        LogMissingFontDiagnostic(assetBase, isDialogue);
        return null;
    }

    private static void ApplyFontLineSpacing(SpriteFont font, string canonicalLang, bool isDialogue, LanguageSourceDescriptor? source = null)
    {
        if (isDialogue || font == null)
            return;

        // Built-in language adjustments matching vanilla Game1.cs
        if (canonicalLang == "ko")
        {
            font.LineSpacing += 16;
        }
        else if (canonicalLang == "tr")
        {
            font.LineSpacing += 4;
        }

        // Custom fields override
        if (source?.CustomFields != null &&
            source.CustomFields.TryGetValue("UntranslatedStardew.SmallFontLineSpacing", out string? spacingStr) &&
            int.TryParse(spacingStr, out int spacing) && spacing > 0)
        {
            font.LineSpacing = spacing;
        }
    }

    private void LogMissingFontDiagnostic(string assetBase, bool isDialogue, string? detail = null)
    {
        if (isDialogue)
        {
            if (!_loggedMissingDialogueFont)
            {
                _loggedMissingDialogueFont = true;
                string msg = $"Secondary dialogue font asset '{assetBase}' for locale '{_effectiveLanguage}' is unavailable. Falling back to primary font." + (detail != null ? $" ({detail})" : "");
                _onDiagnosticWarning?.Invoke(msg);
            }
        }
        else
        {
            if (!_loggedMissingSmallFont)
            {
                _loggedMissingSmallFont = true;
                string msg = $"Secondary small font asset '{assetBase}' for locale '{_effectiveLanguage}' is unavailable. Falling back to primary font." + (detail != null ? $" ({detail})" : "");
                _onDiagnosticWarning?.Invoke(msg);
            }
        }
    }

    private ContentManager GetVanillaFontContentManager(IServiceProvider sp)
    {
        if (_vanillaFontContentManager != null)
            return _vanillaFontContentManager;

        _vanillaFontContentManager = new ContentManager(sp, _gameContentRoot);
        return _vanillaFontContentManager;
    }

    private ContentManager GetModFontContentManager(IServiceProvider sp)
    {
        if (_modFontContentManager != null)
            return _modFontContentManager;

        _modFontContentManager = new ContentManager(sp, _modContentRoot!);
        return _modFontContentManager;
    }

    private ContentManager GetCustomModContentManager(IServiceProvider sp, string contentRoot)
    {
        if (_customModContentManagers.TryGetValue(contentRoot, out ContentManager? cm))
            return cm;

        cm = new ContentManager(sp, contentRoot);
        _customModContentManagers[contentRoot] = cm;
        return cm;
    }

    public void Invalidate()
    {
        _bitmapAtlas?.Dispose();
        _bitmapAtlas = null;
        _bitmapFont = null;
        _secondarySmallFont = null;
        _secondaryDialogueFont = null;
        _attemptedSmallFont = false;
        _attemptedDialogueFont = false;
        _loggedMissingSmallFont = false;
        _loggedMissingDialogueFont = false;
        _vanillaFontContentManager?.Unload();
        _modFontContentManager?.Unload();
        foreach (var cm in _customModContentManagers.Values)
        {
            cm.Unload();
        }
        _customModContentManagers.Clear();
        _missingGlyphsWarned.Clear();
        _testSecondaryFonts.Clear();
        BilingualRunPlanner.ClearCache();
        BilingualStringTracker.Clear();
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
        _bitmapAtlas?.Dispose();
        _bitmapAtlas = null;
        _bitmapFont = null;
        _secondarySmallFont = null;
        _secondaryDialogueFont = null;
        _attemptedSmallFont = false;
        _attemptedDialogueFont = false;
        _loggedMissingSmallFont = false;
        _loggedMissingDialogueFont = false;
        _vanillaFontContentManager?.Dispose();
        _modFontContentManager?.Dispose();
        _vanillaFontContentManager = null;
        _modFontContentManager = null;
        foreach (var cm in _customModContentManagers.Values)
        {
            cm.Dispose();
        }
        _customModContentManagers.Clear();
        _missingGlyphsWarned.Clear();
        _testSecondaryFonts.Clear();
        BilingualRunPlanner.ClearCache();
        BilingualStringTracker.Clear();

        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }
}
