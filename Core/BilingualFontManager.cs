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
/// </summary>
public sealed class BilingualFontManager : IDisposable
{
    public static BilingualFontManager? Instance { get; set; }

    private readonly Func<IServiceProvider?> _getServiceProvider;
    private readonly string _gameContentRoot;
    private readonly string? _modContentRoot;
    private readonly string _effectiveLanguage;
    private readonly Action<char, string, string>? _onMissingGlyphWarning;
    private readonly Action<string>? _onDiagnosticWarning;

    private ContentManager? _vanillaFontContentManager;
    private ContentManager? _modFontContentManager;

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

    public BilingualFontManager(
        Func<IServiceProvider?> getServiceProvider,
        string gameContentRoot,
        string? modContentRoot,
        string effectiveLanguage,
        Action<char, string, string>? onMissingGlyphWarning = null,
        Action<string>? onDiagnosticWarning = null)
    {
        _getServiceProvider = getServiceProvider ?? (() => null);
        _gameContentRoot = gameContentRoot;
        _modContentRoot = modContentRoot;
        _effectiveLanguage = string.IsNullOrWhiteSpace(effectiveLanguage) ? "default" : effectiveLanguage.Trim();
        _onMissingGlyphWarning = onMissingGlyphWarning;
        _onDiagnosticWarning = onDiagnosticWarning;

        Instance = this;
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
    /// Returns null if secondary language is English/default or if assets are unavailable.
    /// </summary>
    public SpriteFont? GetSecondaryFont(SpriteFont primaryFont)
    {
        if (_disposed || primaryFont == null)
            return null;

        if (_testSecondaryFonts.TryGetValue(primaryFont, out SpriteFont? testFont))
            return testFont;

        if (LanguageHelper.IsBaseOrEnglish(_effectiveLanguage))
            return null;

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

    private static bool IsDialogueFont(SpriteFont font)
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

        if (LanguageHelper.IsVietnamese(_effectiveLanguage))
        {
            if (string.IsNullOrEmpty(_modContentRoot) || !Directory.Exists(_modContentRoot))
            {
                LogMissingFontDiagnostic(assetBase, isDialogue);
                return null;
            }

            ContentManager modManager = GetModFontContentManager(sp);
            string viAsset = $"{assetBase}_vi";
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

            LogMissingFontDiagnostic(assetBase, isDialogue);
            return null;
        }

        if (LanguageHelper.IsBuiltInLanguage(_effectiveLanguage))
        {
            string? suffix = LanguageHelper.GetLanguageSuffix(_effectiveLanguage);
            if (string.IsNullOrEmpty(suffix) || !Directory.Exists(_gameContentRoot))
            {
                LogMissingFontDiagnostic(assetBase, isDialogue);
                return null;
            }

            ContentManager vanillaManager = GetVanillaFontContentManager(sp);
            string localizedAsset = $"{assetBase}.{suffix}";
            string localizedPath = Path.Combine(_gameContentRoot, $"{localizedAsset}.xnb");

            if (File.Exists(localizedPath))
            {
                try
                {
                    return vanillaManager.Load<SpriteFont>(localizedAsset);
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

        return null;
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

    public void Invalidate()
    {
        _secondarySmallFont = null;
        _secondaryDialogueFont = null;
        _attemptedSmallFont = false;
        _attemptedDialogueFont = false;
        _loggedMissingSmallFont = false;
        _loggedMissingDialogueFont = false;
        _vanillaFontContentManager?.Unload();
        _modFontContentManager?.Unload();
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
