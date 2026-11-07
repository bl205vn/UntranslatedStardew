using System;
using System.Collections.Generic;
using System.IO;
using BmFont;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>
/// Resolves and caches <see cref="DialogueNativeFont"/> instances by canonical language, locale-specific runtime options
/// (Russian alternate font, Chinese smooth font and dialogue scale), and custom <see cref="LanguageSourceRegistry"/> generation.
/// Never mutates global <see cref="StardewValley.BellsAndWhistles.SpriteText"/> state and never disposes content-owned textures.
/// </summary>
internal sealed class DialogueNativeFontProvider : IDisposable
{
    internal const string FontBoldAsset = "LooseSprites/font_bold";
    internal const string FontColoredAsset = "LooseSprites/font_colored";

    private enum PolicyKind
    {
        Unsupported = 0,
        NativeLatin = 1,
        NativeBitmap = 2
    }

    private readonly record struct FontPolicySpec(
        PolicyKind Kind,
        string CanonicalLanguage,
        string? FontAsset,
        float BaseZoom,
        float EffectiveZoom,
        bool UseRussianAlternate,
        bool UseChineseSmooth,
        int QuantizedChineseScale,
        long SourceGeneration,
        string? DiagnosticReason);

    private readonly record struct CacheKey(
        string CanonicalLanguage,
        PolicyKind Kind,
        string FontAsset,
        int QuantizedEffectiveZoom,
        bool UseRussianAlternate,
        bool UseChineseSmooth,
        int QuantizedChineseScale,
        long SourceGeneration);

    private readonly object _syncLock = new();
    private readonly LanguageSourceRegistry _registry;
    private readonly Action<string>? _log;
    private readonly bool _useMetricStubs;
    private readonly Func<string, string?>? _xmlLoader;
    private readonly Func<string, DialogueNativeFont.TextureDimensions?>? _metricTextureLoader;
    private readonly Func<bool>? _useAlternateRussianFont;
    private readonly Func<bool>? _useChineseSmoothFont;
    private readonly Func<float>? _chineseDialogueFontScale;

    private readonly Dictionary<CacheKey, DialogueNativeFont?> _cache = new();
    private readonly Dictionary<string, LanguageSourceDescriptor?> _descriptors = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loggedDiagnostics = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public DialogueNativeFontProvider(LanguageSourceRegistry registry, Action<string>? log = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _log = log;
        _useMetricStubs = false;
        _registry.OnChanged += Invalidate;
    }

    internal DialogueNativeFontProvider(
        LanguageSourceRegistry registry,
        Action<string>? log,
        Func<string, string?>? xmlLoader,
        Func<string, DialogueNativeFont.TextureDimensions?>? metricTextureLoader,
        Func<bool>? useAlternateRussianFont = null,
        Func<bool>? useChineseSmoothFont = null,
        Func<float>? chineseDialogueFontScale = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _log = log;
        _useMetricStubs = true;
        _xmlLoader = xmlLoader;
        _metricTextureLoader = metricTextureLoader;
        _useAlternateRussianFont = useAlternateRussianFont;
        _useChineseSmoothFont = useChineseSmoothFont;
        _chineseDialogueFontScale = chineseDialogueFontScale;
        _registry.OnChanged += Invalidate;
    }

    /// <summary>
    /// Resolves the native dialogue font for <paramref name="language"/>, or returns <c>null</c> if the locale
    /// lacks a valid native dialogue font declaration or its assets cannot be loaded.
    /// </summary>
    public DialogueNativeFont? Get(string language)
    {
        if (_disposed || string.IsNullOrWhiteSpace(language))
            return null;

        FontPolicySpec spec = ResolvePolicySpec(language);
        if (spec.Kind == PolicyKind.Unsupported)
        {
            LogOnce(
                $"unsupported:{spec.CanonicalLanguage}:{spec.SourceGeneration}:{spec.DiagnosticReason}",
                spec.DiagnosticReason ?? $"No native dialogue font declaration for locale '{spec.CanonicalLanguage}'.");
            return null;
        }

        var key = new CacheKey(
            CanonicalLanguage: spec.CanonicalLanguage,
            Kind: spec.Kind,
            FontAsset: spec.FontAsset ?? string.Empty,
            QuantizedEffectiveZoom: QuantizeFloat(spec.EffectiveZoom),
            UseRussianAlternate: spec.UseRussianAlternate,
            UseChineseSmooth: spec.UseChineseSmooth,
            QuantizedChineseScale: spec.QuantizedChineseScale,
            SourceGeneration: spec.SourceGeneration);

        lock (_syncLock)
        {
            if (_disposed)
                return null;

            if (_cache.TryGetValue(key, out DialogueNativeFont? cached))
                return cached;
        }

        DialogueNativeFont? loaded = LoadFontForSpec(spec);

        lock (_syncLock)
        {
            if (_disposed)
                return null;

            _cache[key] = loaded;
            return loaded;
        }
    }

    /// <summary>
    /// Clears cached font metadata references without disposing any content-owned textures.
    /// </summary>
    public void Invalidate()
    {
        lock (_syncLock)
        {
            _cache.Clear();
            _descriptors.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _registry.OnChanged -= Invalidate;
        Invalidate();
    }

    private FontPolicySpec ResolvePolicySpec(string rawLanguage)
    {
        string canonical = NormalizeCanonicalLanguage(rawLanguage);

        LanguageSourceDescriptor? source;
        lock (_syncLock)
        {
            if (!_descriptors.TryGetValue(canonical, out source))
            {
                _registry.TryGetSource(canonical, out source);
                _descriptors[canonical] = source;
            }
        }
        if (source != null)
        {
            if (source.UseGameLatinFont)
            {
                return new FontPolicySpec(
                    Kind: PolicyKind.NativeLatin,
                    CanonicalLanguage: canonical,
                    FontAsset: FontBoldAsset,
                    BaseZoom: DialogueNativeFont.DefaultLatinZoom,
                    EffectiveZoom: DialogueNativeFont.DefaultLatinZoom,
                    UseRussianAlternate: false,
                    UseChineseSmooth: false,
                    QuantizedChineseScale: 0,
                    SourceGeneration: source.Generation,
                    DiagnosticReason: null);
            }

            if (!string.IsNullOrWhiteSpace(source.BitmapFontAsset))
            {
                float customZoom = source.BitmapFontZoom;
                if (customZoom <= 0f || float.IsNaN(customZoom) || float.IsInfinity(customZoom))
                {
                    return new FontPolicySpec(
                        Kind: PolicyKind.Unsupported,
                        CanonicalLanguage: canonical,
                        FontAsset: null,
                        BaseZoom: 0f,
                        EffectiveZoom: 0f,
                        UseRussianAlternate: false,
                        UseChineseSmooth: false,
                        QuantizedChineseScale: 0,
                        SourceGeneration: source.Generation,
                        DiagnosticReason: $"Custom language '{canonical}' declared invalid BitmapFontZoom ({customZoom}).");
                }

                return new FontPolicySpec(
                    Kind: PolicyKind.NativeBitmap,
                    CanonicalLanguage: canonical,
                    FontAsset: source.BitmapFontAsset!.Trim(),
                    BaseZoom: customZoom,
                    EffectiveZoom: customZoom,
                    UseRussianAlternate: false,
                    UseChineseSmooth: false,
                    QuantizedChineseScale: 0,
                    SourceGeneration: source.Generation,
                    DiagnosticReason: null);
            }

            if (!LanguageHelper.IsBuiltInLanguage(canonical))
            {
                return new FontPolicySpec(
                    Kind: PolicyKind.Unsupported,
                    CanonicalLanguage: canonical,
                    FontAsset: null,
                    BaseZoom: 0f,
                    EffectiveZoom: 0f,
                    UseRussianAlternate: false,
                    UseChineseSmooth: false,
                    QuantizedChineseScale: 0,
                    SourceGeneration: source.Generation,
                    DiagnosticReason: $"Custom language '{canonical}' (mod '{source.SourceModId}') has no native dialogue font declaration (UseGameLatinFont or BitmapFontAsset); refusing silent fallback to UI SpriteFont1.");
            }
        }

        switch (canonical)
        {
            case "en":
            case "es":
            case "de":
            case "fr":
            case "pt":
            case "it":
            case "tr":
            case "hu":
                return new FontPolicySpec(
                    Kind: PolicyKind.NativeLatin,
                    CanonicalLanguage: canonical,
                    FontAsset: FontBoldAsset,
                    BaseZoom: DialogueNativeFont.DefaultLatinZoom,
                    EffectiveZoom: DialogueNativeFont.DefaultLatinZoom,
                    UseRussianAlternate: false,
                    UseChineseSmooth: false,
                    QuantizedChineseScale: 0,
                    SourceGeneration: 0,
                    DiagnosticReason: null);

            case "ru":
            {
                bool useAlternate = ReadUseAlternateRussianFont();
                if (useAlternate)
                {
                    return new FontPolicySpec(
                        Kind: PolicyKind.NativeBitmap,
                        CanonicalLanguage: "ru",
                        FontAsset: "Fonts/Russian",
                        BaseZoom: 3f,
                        EffectiveZoom: 3f,
                        UseRussianAlternate: true,
                        UseChineseSmooth: false,
                        QuantizedChineseScale: 0,
                        SourceGeneration: 0,
                        DiagnosticReason: null);
                }

                return new FontPolicySpec(
                    Kind: PolicyKind.NativeLatin,
                    CanonicalLanguage: "ru",
                    FontAsset: FontBoldAsset,
                    BaseZoom: DialogueNativeFont.DefaultLatinZoom,
                    EffectiveZoom: DialogueNativeFont.DefaultLatinZoom,
                    UseRussianAlternate: false,
                    UseChineseSmooth: false,
                    QuantizedChineseScale: 0,
                    SourceGeneration: 0,
                    DiagnosticReason: null);
            }

            case "ja":
                return new FontPolicySpec(
                    Kind: PolicyKind.NativeBitmap,
                    CanonicalLanguage: "ja",
                    FontAsset: "Fonts/Japanese",
                    BaseZoom: 1.75f,
                    EffectiveZoom: 1.75f,
                    UseRussianAlternate: false,
                    UseChineseSmooth: false,
                    QuantizedChineseScale: 0,
                    SourceGeneration: 0,
                    DiagnosticReason: null);

            case "ko":
                return new FontPolicySpec(
                    Kind: PolicyKind.NativeBitmap,
                    CanonicalLanguage: "ko",
                    FontAsset: "Fonts/Korean",
                    BaseZoom: 1.5f,
                    EffectiveZoom: 1.5f,
                    UseRussianAlternate: false,
                    UseChineseSmooth: false,
                    QuantizedChineseScale: 0,
                    SourceGeneration: 0,
                    DiagnosticReason: null);

            case "zh":
            {
                bool smooth = ReadUseChineseSmoothFont();
                float dialogueScale = ReadChineseDialogueFontScale();
                string fontAsset = smooth ? "Fonts/Chinese_round/Chinese" : "Fonts/Chinese";
                float baseZoom = smooth ? 1f : 1.5f;
                float effectiveZoom = ComputeChineseEffectiveZoom(smooth, dialogueScale);

                return new FontPolicySpec(
                    Kind: PolicyKind.NativeBitmap,
                    CanonicalLanguage: "zh",
                    FontAsset: fontAsset,
                    BaseZoom: baseZoom,
                    EffectiveZoom: effectiveZoom,
                    UseRussianAlternate: false,
                    UseChineseSmooth: smooth,
                    QuantizedChineseScale: QuantizeFloat(dialogueScale),
                    SourceGeneration: 0,
                    DiagnosticReason: null);
            }

            case "th":
                return new FontPolicySpec(
                    Kind: PolicyKind.NativeBitmap,
                    CanonicalLanguage: "th",
                    FontAsset: "Fonts/Thai",
                    BaseZoom: 1.5f,
                    EffectiveZoom: 1.5f,
                    UseRussianAlternate: false,
                    UseChineseSmooth: false,
                    QuantizedChineseScale: 0,
                    SourceGeneration: 0,
                    DiagnosticReason: null);

            default:
                return new FontPolicySpec(
                    Kind: PolicyKind.Unsupported,
                    CanonicalLanguage: canonical,
                    FontAsset: null,
                    BaseZoom: 0f,
                    EffectiveZoom: 0f,
                    UseRussianAlternate: false,
                    UseChineseSmooth: false,
                    QuantizedChineseScale: 0,
                    SourceGeneration: 0,
                    DiagnosticReason: $"Locale '{canonical}' is unknown or lacks a registered native dialogue font declaration.");
        }
    }

    internal static float ComputeChineseEffectiveZoom(bool useSmoothFont, float dialogueFontScale)
    {
        float baseZoom = useSmoothFont ? 1f : 1.5f;
        float divisor = useSmoothFont ? 4f : 2f;
        float effective = baseZoom + ((dialogueFontScale - 1f) / divisor);
        return effective > 0.05f ? effective : baseZoom;
    }

    private DialogueNativeFont? LoadFontForSpec(FontPolicySpec spec)
    {
        if (spec.Kind == PolicyKind.NativeLatin)
        {
            return LoadLatinFont(spec);
        }

        if (spec.Kind == PolicyKind.NativeBitmap)
        {
            return LoadBitmapFont(spec);
        }

        return null;
    }

    private DialogueNativeFont? LoadLatinFont(FontPolicySpec spec)
    {
        if (_useMetricStubs)
        {
            DialogueNativeFont.TextureDimensions? boldDims = _metricTextureLoader != null
                ? _metricTextureLoader(FontBoldAsset)
                : new DialogueNativeFont.TextureDimensions(DialogueNativeFont.DefaultSpriteSheetWidth, DialogueNativeFont.DefaultSpriteSheetHeight);
            DialogueNativeFont.TextureDimensions? coloredDims = _metricTextureLoader != null
                ? _metricTextureLoader(FontColoredAsset)
                : new DialogueNativeFont.TextureDimensions(DialogueNativeFont.DefaultSpriteSheetWidth, DialogueNativeFont.DefaultSpriteSheetHeight);

            if (boldDims == null || !boldDims.Value.IsValid || coloredDims == null || !coloredDims.Value.IsValid)
            {
                LogOnce(
                    $"latin-missing:{spec.CanonicalLanguage}:{spec.SourceGeneration}",
                    $"Failed to load shared Latin sprite sheets ('{FontBoldAsset}' / '{FontColoredAsset}') for locale '{spec.CanonicalLanguage}'.");
                return null;
            }

            return DialogueNativeFont.CreateLatinFromMetrics(
                languageCode: spec.CanonicalLanguage,
                zoom: spec.EffectiveZoom,
                sheetWidth: boldDims.Value.Width,
                sheetHeight: boldDims.Value.Height,
                coloredWidth: coloredDims.Value.Width,
                coloredHeight: coloredDims.Value.Height);
        }

        Texture2D? fontBold = TryLoadContentTexture(FontBoldAsset);
        Texture2D? fontColored = TryLoadContentTexture(FontColoredAsset);
        DialogueNativeFont? font = DialogueNativeFont.CreateRuntimeLatin(
            languageCode: spec.CanonicalLanguage,
            fontBoldTexture: fontBold,
            fontColoredTexture: fontColored,
            zoom: spec.EffectiveZoom);

        if (font == null)
        {
            LogOnce(
                $"latin-runtime-missing:{spec.CanonicalLanguage}:{spec.SourceGeneration}",
                $"Failed to load runtime Latin sprite sheets ('{FontBoldAsset}' / '{FontColoredAsset}') for locale '{spec.CanonicalLanguage}'.");
        }

        return font;
    }

    private DialogueNativeFont? LoadBitmapFont(FontPolicySpec spec)
    {
        if (!TryNormalizeRelativeAssetPath(spec.FontAsset, stripKnownFontExtension: true, out string normalizedFontAsset))
        {
            LogOnce(
                $"bmfont-invalid-path:{spec.CanonicalLanguage}:{spec.SourceGeneration}:{spec.FontAsset}",
                $"Rejected unsafe or invalid BMFont asset path '{spec.FontAsset}' for locale '{spec.CanonicalLanguage}'.");
            return null;
        }

        string? xmlSource = TryLoadXmlSource(normalizedFontAsset);
        if (string.IsNullOrWhiteSpace(xmlSource))
        {
            LogOnce(
                $"bmfont-xml-missing:{spec.CanonicalLanguage}:{spec.SourceGeneration}:{normalizedFontAsset}",
                $"Failed to load BMFont XML asset '{normalizedFontAsset}' for locale '{spec.CanonicalLanguage}'.");
            return null;
        }

        FontFile? fontFile;
        try
        {
            fontFile = FontLoader.Parse(xmlSource);
        }
        catch (Exception ex)
        {
            LogOnce(
                $"bmfont-xml-parse:{spec.CanonicalLanguage}:{spec.SourceGeneration}:{normalizedFontAsset}",
                $"Failed to parse BMFont XML asset '{normalizedFontAsset}' for locale '{spec.CanonicalLanguage}': {ex.Message}");
            return null;
        }

        if (fontFile?.Common == null || fontFile.Common.LineHeight <= 0 || fontFile.Pages == null || fontFile.Pages.Count == 0)
        {
            LogOnce(
                $"bmfont-invalid-descriptor:{spec.CanonicalLanguage}:{spec.SourceGeneration}:{normalizedFontAsset}",
                $"BMFont descriptor '{normalizedFontAsset}' for locale '{spec.CanonicalLanguage}' is missing valid Common.LineHeight or Pages.");
            return null;
        }

        IReadOnlyList<FontPage>? orderedPages = OrderPages(fontFile.Pages);
        if (orderedPages == null)
        {
            LogOnce(
                $"bmfont-invalid-pages:{spec.CanonicalLanguage}:{spec.SourceGeneration}:{normalizedFontAsset}",
                $"BMFont descriptor '{normalizedFontAsset}' for locale '{spec.CanonicalLanguage}' has invalid or non-contiguous page IDs.");
            return null;
        }

        bool isRussianAlt = string.Equals(spec.CanonicalLanguage, "ru", StringComparison.OrdinalIgnoreCase) && spec.UseRussianAlternate;

        if (_useMetricStubs)
        {
            DialogueNativeFont.TextureDimensions? boldDims = _metricTextureLoader != null
                ? _metricTextureLoader(FontBoldAsset)
                : new DialogueNativeFont.TextureDimensions(DialogueNativeFont.DefaultSpriteSheetWidth, DialogueNativeFont.DefaultSpriteSheetHeight);

            if (boldDims == null || !boldDims.Value.IsValid)
            {
                LogOnce(
                    $"bmfont-iconsheet-missing:{spec.CanonicalLanguage}:{spec.SourceGeneration}:{normalizedFontAsset}",
                    $"Shared icon sprite sheet '{FontBoldAsset}' is unavailable for BMFont locale '{spec.CanonicalLanguage}'.");
                return null;
            }

            var pageDims = new List<DialogueNativeFont.TextureDimensions>(orderedPages.Count);
            var resolvedAssets = new List<string>(orderedPages.Count);

            for (int i = 0; i < orderedPages.Count; i++)
            {
                FontPage page = orderedPages[i];
                if (!TryBuildPageAssetCandidates(normalizedFontAsset, page.File, out IReadOnlyList<string> candidates))
                {
                    LogOnce(
                        $"bmfont-page-unsafe:{spec.CanonicalLanguage}:{spec.SourceGeneration}:{normalizedFontAsset}:{page.File}",
                        $"Rejected unsafe or invalid BMFont page file '{page.File}' in '{normalizedFontAsset}'.");
                    return null;
                }

                DialogueNativeFont.TextureDimensions? matchedDim = null;
                string? matchedAsset = null;
                foreach (string candidate in candidates)
                {
                    DialogueNativeFont.TextureDimensions? dim = _metricTextureLoader != null
                        ? _metricTextureLoader(candidate)
                        : new DialogueNativeFont.TextureDimensions(512, 512);
                    if (dim != null && dim.Value.IsValid)
                    {
                        matchedDim = dim.Value;
                        matchedAsset = candidate;
                        break;
                    }
                }

                if (matchedDim == null || matchedAsset == null)
                {
                    LogOnce(
                        $"bmfont-page-missing:{spec.CanonicalLanguage}:{spec.SourceGeneration}:{normalizedFontAsset}:{page.File}",
                        $"Failed to resolve BMFont page texture '{page.File}' for '{normalizedFontAsset}' (locale '{spec.CanonicalLanguage}').");
                    return null;
                }

                pageDims.Add(matchedDim.Value);
                resolvedAssets.Add(matchedAsset);
            }

            return DialogueNativeFont.CreateBitmapFromFontFile(
                fontFile: fontFile,
                baseZoom: spec.BaseZoom,
                effectiveZoom: spec.EffectiveZoom,
                pageDimensions: pageDims,
                iconSheetWidth: boldDims.Value.Width,
                iconSheetHeight: boldDims.Value.Height,
                languageCode: spec.CanonicalLanguage,
                fontAsset: normalizedFontAsset,
                resolvedPageAssets: resolvedAssets,
                isRussianAlternateBmFont: isRussianAlt);
        }

        Texture2D? runtimeFontBold = TryLoadContentTexture(FontBoldAsset);
        if (runtimeFontBold == null)
        {
            LogOnce(
                $"bmfont-runtime-iconsheet-missing:{spec.CanonicalLanguage}:{spec.SourceGeneration}:{normalizedFontAsset}",
                $"Failed to load shared icon sprite sheet '{FontBoldAsset}' for BMFont locale '{spec.CanonicalLanguage}'.");
            return null;
        }

        var runtimePages = new List<Texture2D?>(orderedPages.Count);
        var runtimeResolvedAssets = new List<string>(orderedPages.Count);

        for (int i = 0; i < orderedPages.Count; i++)
        {
            FontPage page = orderedPages[i];
            if (!TryBuildPageAssetCandidates(normalizedFontAsset, page.File, out IReadOnlyList<string> candidates))
            {
                LogOnce(
                    $"bmfont-runtime-page-unsafe:{spec.CanonicalLanguage}:{spec.SourceGeneration}:{normalizedFontAsset}:{page.File}",
                    $"Rejected unsafe or invalid BMFont page file '{page.File}' in '{normalizedFontAsset}'.");
                return null;
            }

            Texture2D? matchedTex = null;
            string? matchedAsset = null;
            foreach (string candidate in candidates)
            {
                Texture2D? tex = TryLoadContentTexture(candidate);
                if (tex != null)
                {
                    matchedTex = tex;
                    matchedAsset = candidate;
                    break;
                }
            }

            if (matchedTex == null || matchedAsset == null)
            {
                LogOnce(
                    $"bmfont-runtime-page-missing:{spec.CanonicalLanguage}:{spec.SourceGeneration}:{normalizedFontAsset}:{page.File}",
                    $"Failed to load BMFont page texture '{page.File}' for '{normalizedFontAsset}' (locale '{spec.CanonicalLanguage}').");
                return null;
            }

            runtimePages.Add(matchedTex);
            runtimeResolvedAssets.Add(matchedAsset);
        }

        DialogueNativeFont? runtimeFont = DialogueNativeFont.CreateRuntimeBitmap(
            languageCode: spec.CanonicalLanguage,
            fontAsset: normalizedFontAsset,
            fontFile: fontFile,
            pageTextures: runtimePages,
            resolvedPageAssets: runtimeResolvedAssets,
            fontBoldTexture: runtimeFontBold,
            baseZoom: spec.BaseZoom,
            effectiveZoom: spec.EffectiveZoom,
            isRussianAlternateBmFont: isRussianAlt);

        if (runtimeFont == null)
        {
            LogOnce(
                $"bmfont-runtime-create-failed:{spec.CanonicalLanguage}:{spec.SourceGeneration}:{normalizedFontAsset}",
                $"Failed to construct native BMFont renderer for '{normalizedFontAsset}' (locale '{spec.CanonicalLanguage}').");
        }

        return runtimeFont;
    }

    private static IReadOnlyList<FontPage>? OrderPages(List<FontPage> pages)
    {
        if (pages == null || pages.Count == 0)
            return null;

        var ordered = new FontPage?[pages.Count];
        bool allIndexed = true;

        for (int i = 0; i < pages.Count; i++)
        {
            FontPage? p = pages[i];
            if (p == null || string.IsNullOrWhiteSpace(p.File))
                return null;

            if (p.ID >= 0 && p.ID < pages.Count && ordered[p.ID] == null)
            {
                ordered[p.ID] = p;
            }
            else
            {
                allIndexed = false;
            }
        }

        if (allIndexed)
        {
            var result = new FontPage[pages.Count];
            for (int i = 0; i < pages.Count; i++)
            {
                if (ordered[i] == null)
                    return null;
                result[i] = ordered[i]!;
            }
            return result;
        }

        return pages;
    }

    /// <summary>
    /// Validates and normalizes a relative content asset path, rejecting rooted paths, drive letters,
    /// UNC prefixes, empty segments, and directory traversal (<c>.</c> or <c>..</c>).
    /// </summary>
    internal static bool TryNormalizeRelativeAssetPath(string? rawPath, bool stripKnownFontExtension, out string normalizedPath)
    {
        normalizedPath = string.Empty;
        if (string.IsNullOrWhiteSpace(rawPath))
            return false;

        string trimmed = rawPath.Trim();
        if (trimmed.StartsWith("/", StringComparison.Ordinal)
            || trimmed.StartsWith("\\", StringComparison.Ordinal)
            || trimmed.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            if (Path.IsPathRooted(trimmed))
                return false;
        }
        catch
        {
            return false;
        }

        string slashNormalized = trimmed.Replace('\\', '/');
        string[] segments = slashNormalized.Split('/');
        if (segments.Length == 0)
            return false;

        char[] invalidNameChars = Path.GetInvalidFileNameChars();
        for (int i = 0; i < segments.Length; i++)
        {
            string seg = segments[i];
            if (string.IsNullOrWhiteSpace(seg) || seg == "." || seg == "..")
                return false;

            if (seg.IndexOfAny(invalidNameChars) >= 0)
                return false;
        }

        string joined = string.Join("/", segments);
        if ( joined.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
        {
            joined = joined.Substring(0, joined.Length - 4);
        }

        if (stripKnownFontExtension)
        {
            if (joined.EndsWith(".fnt", StringComparison.OrdinalIgnoreCase))
            {
                joined = joined.Substring(0, joined.Length - 4);
            }
            else if (joined.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                joined = joined.Substring(0, joined.Length - 4);
            }
        }

        if (string.IsNullOrWhiteSpace(joined) || joined.EndsWith("/", StringComparison.Ordinal))
            return false;

        normalizedPath = joined;
        return true;
    }

    /// <summary>
    /// Builds ordered candidate content asset names for a BMFont page file, handling <c>.png</c> vs unsuffixed names,
    /// slash/backslash normalization, custom folders, and standard <c>Fonts/</c> fallback while rejecting rooted/traversal paths.
    /// </summary>
    internal static bool TryBuildPageAssetCandidates(string fontAsset, string pageFile, out IReadOnlyList<string> candidates)
    {
        candidates = Array.Empty<string>();
        if (!TryNormalizeRelativeAssetPath(fontAsset, stripKnownFontExtension: true, out string normFontAsset))
            return false;

        if (!TryNormalizeRelativeAssetPath(pageFile, stripKnownFontExtension: false, out string normPageFile))
            return false;

        int lastSlash = normFontAsset.LastIndexOf('/');
        string fontDir = lastSlash > 0 ? normFontAsset.Substring(0, lastSlash) : string.Empty;

        var basePaths = new List<string>(3);
        if (!string.IsNullOrEmpty(fontDir))
        {
            if (normPageFile.StartsWith(fontDir + "/", StringComparison.OrdinalIgnoreCase))
            {
                basePaths.Add(normPageFile);
            }
            else
            {
                basePaths.Add(fontDir + "/" + normPageFile);
            }

            if (!string.Equals(fontDir, "Fonts", StringComparison.OrdinalIgnoreCase)
                && !normPageFile.StartsWith("Fonts/", StringComparison.OrdinalIgnoreCase))
            {
                basePaths.Add("Fonts/" + normPageFile);
            }
        }
        else
        {
            if (!normPageFile.StartsWith("Fonts/", StringComparison.OrdinalIgnoreCase))
            {
                basePaths.Add("Fonts/" + normPageFile);
            }
            basePaths.Add(normPageFile);
        }

        var result = new List<string>(basePaths.Count * 2);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string basePath in basePaths)
        {
            AddCandidateIfUnique(result, seen, basePath);

            if (basePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                string withoutPng = basePath.Substring(0, basePath.Length - 4);
                if (!string.IsNullOrWhiteSpace(withoutPng))
                {
                    AddCandidateIfUnique(result, seen, withoutPng);
                }
            }
            else
            {
                AddCandidateIfUnique(result, seen, basePath + ".png");
            }
        }

        if (result.Count == 0)
            return false;

        candidates = result;
        return true;
    }

    private static void AddCandidateIfUnique(List<string> list, HashSet<string> seen, string candidate)
    {
        if (seen.Add(candidate))
        {
            list.Add(candidate);
        }
    }

    private string? TryLoadXmlSource(string normalizedFontAsset)
    {
        if (_useMetricStubs)
        {
            if (_xmlLoader == null)
                return null;

            return _xmlLoader(normalizedFontAsset) ?? _xmlLoader(normalizedFontAsset + ".fnt");
        }

        LocalizedContentManager? content = Game1.content;
        if (content == null)
            return null;

        try
        {
            XmlSource? xml = content.Load<XmlSource>(normalizedFontAsset);
            if (!string.IsNullOrWhiteSpace(xml?.Source))
                return xml.Source;
        }
        catch
        {
            // Try .fnt extension fallback below
        }

        try
        {
            XmlSource? xml = content.Load<XmlSource>(normalizedFontAsset + ".fnt");
            if (!string.IsNullOrWhiteSpace(xml?.Source))
                return xml.Source;
        }
        catch
        {
            // Fail closed
        }

        return null;
    }

    private static Texture2D? TryLoadContentTexture(string assetName)
    {
        LocalizedContentManager? content = Game1.content;
        if (content == null)
            return null;

        try
        {
            Texture2D? texture = content.Load<Texture2D>(assetName);
            if (texture != null && !texture.IsDisposed && texture.Width > 0 && texture.Height > 0)
                return texture;
        }
        catch
        {
            // Fail closed
        }

        return null;
    }

    private bool ReadUseAlternateRussianFont()
    {
        if (_useAlternateRussianFont != null)
            return _useAlternateRussianFont();

        try
        {
            return Game1.options?.useAlternateFont ?? false;
        }
        catch
        {
            return false;
        }
    }

    private bool ReadUseChineseSmoothFont()
    {
        if (_useChineseSmoothFont != null)
            return _useChineseSmoothFont();

        try
        {
            return Game1.options?.useChineseSmoothFont ?? false;
        }
        catch
        {
            return false;
        }
    }

    private float ReadChineseDialogueFontScale()
    {
        if (_chineseDialogueFontScale != null)
            return _chineseDialogueFontScale();

        try
        {
            return Game1.options?.dialogueFontScale ?? 1f;
        }
        catch
        {
            return 1f;
        }
    }

    private static string NormalizeCanonicalLanguage(string rawLanguage)
    {
        string canonical = LanguageHelper.CanonicalizeLanguageCode(rawLanguage);
        if (LanguageHelper.IsBaseOrEnglish(canonical))
            return "en";

        if (string.Equals(canonical, "th-th", StringComparison.OrdinalIgnoreCase))
            return "th";

        return canonical;
    }

    private static int QuantizeFloat(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return 0;

        return (int)MathF.Round(value * 10000f);
    }

    private void LogOnce(string diagnosticKey, string message)
    {
        bool shouldLog;
        lock (_syncLock)
        {
            shouldLog = _loggedDiagnostics.Add(diagnosticKey);
        }

        if (shouldLog)
        {
            _log?.Invoke(message);
        }
    }
}
