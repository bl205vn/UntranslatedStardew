using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BmFont;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;

[assembly: InternalsVisibleTo("UntranslatedStardew.Tests")]

namespace UntranslatedStardew.Core;

/// <summary>
/// Bounded native secondary dialogue font renderer and metric calculator.
/// Supports both vanilla Latin sprite-sheet fonts (<c>LooseSprites/font_bold</c> and <c>LooseSprites/font_colored</c>)
/// and multi-page BMFont descriptors while preserving shared sprite-sheet icons (<c>&lt; = &gt; @ $ ` +</c> and <c>♡</c>).
/// Does not mutate global <see cref="SpriteText"/> static state or allocate/dispose content-owned GPU textures.
/// </summary>
internal sealed class DialogueNativeFont
{
    internal const int SpriteSheetCharWidth = 8;
    internal const int SpriteSheetCharHeight = 16;
    internal const int VerticalSpaceBetweenCharacters = 2;
    internal const float DefaultLatinZoom = 3f;
    internal const float IconPixelZoom = 3f;
    internal const float RussianShadowAlpha = 0.15f;
    internal const int DefaultSpriteSheetWidth = 128;
    internal const int DefaultSpriteSheetHeight = 608;

    internal readonly record struct TextureDimensions(int Width, int Height)
    {
        public bool IsValid => Width > 0 && Height > 0;
    }

    private static readonly Func<char, bool, Rectangle>? s_vanillaGetSourceRectForChar = CreateVanillaSourceRectDelegate();

    private readonly bool _isBitmapFont;
    private readonly Dictionary<char, FontChar>? _characterMap;
    private readonly Texture2D?[] _pageTextures;
    private readonly TextureDimensions[] _pageDimensions;
    private readonly Texture2D? _fontBoldTexture;
    private readonly Texture2D? _fontColoredTexture;
    private readonly TextureDimensions _fontBoldDimensions;
    private readonly TextureDimensions _fontColoredDimensions;
    private readonly bool _isRussianAlternateBmFont;

    private DialogueNativeFont(
        string languageCode,
        bool isBitmapFont,
        string? fontAsset,
        float baseZoom,
        float effectiveZoom,
        float lineHeight,
        Dictionary<char, FontChar>? characterMap,
        Texture2D?[] pageTextures,
        TextureDimensions[] pageDimensions,
        IReadOnlyList<string> resolvedPageAssets,
        Texture2D? fontBoldTexture,
        TextureDimensions fontBoldDimensions,
        Texture2D? fontColoredTexture,
        TextureDimensions fontColoredDimensions,
        bool isRussianAlternateBmFont)
    {
        LanguageCode = languageCode;
        _isBitmapFont = isBitmapFont;
        FontAsset = fontAsset;
        BaseZoom = baseZoom;
        EffectiveZoom = effectiveZoom;
        LineHeight = lineHeight;
        _characterMap = characterMap;
        _pageTextures = pageTextures;
        _pageDimensions = pageDimensions;
        ResolvedPageAssets = resolvedPageAssets;
        _fontBoldTexture = fontBoldTexture;
        _fontBoldDimensions = fontBoldDimensions;
        _fontColoredTexture = fontColoredTexture;
        _fontColoredDimensions = fontColoredDimensions;
        _isRussianAlternateBmFont = isRussianAlternateBmFont;
    }

    /// <summary>
    /// Native line height in game pixels before final secondary scale (0.75) is applied.
    /// </summary>
    public float LineHeight { get; }

    internal string LanguageCode { get; }
    internal bool IsBitmapFont => _isBitmapFont;
    internal string? FontAsset { get; }
    internal float BaseZoom { get; }
    internal float EffectiveZoom { get; }
    internal int PageCount => _pageDimensions.Length;
    internal IReadOnlyList<string> ResolvedPageAssets { get; }

    internal static DialogueNativeFont? CreateRuntimeLatin(
        string languageCode,
        Texture2D? fontBoldTexture,
        Texture2D? fontColoredTexture,
        float zoom = DefaultLatinZoom)
    {
        if (!IsValidTexture(fontBoldTexture) || !IsValidTexture(fontColoredTexture))
            return null;
        if (!IsValidZoom(zoom))
            return null;

        var boldDims = new TextureDimensions(fontBoldTexture!.Width, fontBoldTexture.Height);
        var coloredDims = new TextureDimensions(fontColoredTexture!.Width, fontColoredTexture.Height);
        float lineHeight = (SpriteSheetCharHeight + VerticalSpaceBetweenCharacters) * zoom;

        return new DialogueNativeFont(
            languageCode: languageCode,
            isBitmapFont: false,
            fontAsset: "LooseSprites/font_bold",
            baseZoom: zoom,
            effectiveZoom: zoom,
            lineHeight: lineHeight,
            characterMap: null,
            pageTextures: Array.Empty<Texture2D?>(),
            pageDimensions: Array.Empty<TextureDimensions>(),
            resolvedPageAssets: Array.Empty<string>(),
            fontBoldTexture: fontBoldTexture,
            fontBoldDimensions: boldDims,
            fontColoredTexture: fontColoredTexture,
            fontColoredDimensions: coloredDims,
            isRussianAlternateBmFont: false);
    }

    internal static DialogueNativeFont? CreateRuntimeBitmap(
        string languageCode,
        string fontAsset,
        FontFile? fontFile,
        IReadOnlyList<Texture2D?>? pageTextures,
        IReadOnlyList<string>? resolvedPageAssets,
        Texture2D? fontBoldTexture,
        float baseZoom,
        float effectiveZoom,
        bool isRussianAlternateBmFont = false)
    {
        if (fontFile?.Common == null || fontFile.Common.LineHeight <= 0)
            return null;
        if (fontFile.Pages == null || fontFile.Pages.Count == 0)
            return null;
        if (pageTextures == null || pageTextures.Count != fontFile.Pages.Count)
            return null;
        if (!IsValidTexture(fontBoldTexture))
            return null;
        if (!IsValidZoom(baseZoom) || !IsValidZoom(effectiveZoom))
            return null;

        var texArray = new Texture2D?[pageTextures.Count];
        var dimArray = new TextureDimensions[pageTextures.Count];
        for (int i = 0; i < pageTextures.Count; i++)
        {
            Texture2D? pageTex = pageTextures[i];
            if (!IsValidTexture(pageTex))
                return null;

            texArray[i] = pageTex;
            dimArray[i] = new TextureDimensions(pageTex!.Width, pageTex.Height);
        }

        Dictionary<char, FontChar> charMap = BuildCharacterMap(fontFile);
        if (charMap.Count == 0)
            return null;

        float lineHeight = (fontFile.Common.LineHeight + VerticalSpaceBetweenCharacters) * effectiveZoom;
        var boldDims = new TextureDimensions(fontBoldTexture!.Width, fontBoldTexture.Height);
        IReadOnlyList<string> pageAssets = resolvedPageAssets != null
            ? new List<string>(resolvedPageAssets)
            : Array.Empty<string>();

        return new DialogueNativeFont(
            languageCode: languageCode,
            isBitmapFont: true,
            fontAsset: fontAsset,
            baseZoom: baseZoom,
            effectiveZoom: effectiveZoom,
            lineHeight: lineHeight,
            characterMap: charMap,
            pageTextures: texArray,
            pageDimensions: dimArray,
            resolvedPageAssets: pageAssets,
            fontBoldTexture: fontBoldTexture,
            fontBoldDimensions: boldDims,
            fontColoredTexture: null,
            fontColoredDimensions: default,
            isRussianAlternateBmFont: isRussianAlternateBmFont);
    }

    internal static DialogueNativeFont? CreateLatinFromMetrics(
        string languageCode = "en",
        float zoom = DefaultLatinZoom,
        int sheetWidth = DefaultSpriteSheetWidth,
        int sheetHeight = DefaultSpriteSheetHeight,
        int coloredWidth = DefaultSpriteSheetWidth,
        int coloredHeight = DefaultSpriteSheetHeight)
    {
        if (!IsValidZoom(zoom))
            return null;

        var boldDims = new TextureDimensions(sheetWidth, sheetHeight);
        var coloredDims = new TextureDimensions(coloredWidth, coloredHeight);
        if (!boldDims.IsValid || !coloredDims.IsValid)
            return null;

        float lineHeight = (SpriteSheetCharHeight + VerticalSpaceBetweenCharacters) * zoom;
        return new DialogueNativeFont(
            languageCode: languageCode,
            isBitmapFont: false,
            fontAsset: "LooseSprites/font_bold",
            baseZoom: zoom,
            effectiveZoom: zoom,
            lineHeight: lineHeight,
            characterMap: null,
            pageTextures: Array.Empty<Texture2D?>(),
            pageDimensions: Array.Empty<TextureDimensions>(),
            resolvedPageAssets: Array.Empty<string>(),
            fontBoldTexture: null,
            fontBoldDimensions: boldDims,
            fontColoredTexture: null,
            fontColoredDimensions: coloredDims,
            isRussianAlternateBmFont: false);
    }

    internal static DialogueNativeFont? CreateBitmapFromXml(
        string xmlContent,
        float baseZoom,
        float effectiveZoom,
        IReadOnlyList<TextureDimensions> pageDimensions,
        int iconSheetWidth = DefaultSpriteSheetWidth,
        int iconSheetHeight = DefaultSpriteSheetHeight,
        string languageCode = "",
        string fontAsset = "",
        IReadOnlyList<string>? resolvedPageAssets = null,
        bool isRussianAlternateBmFont = false)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            return null;

        FontFile? fontFile;
        try
        {
            fontFile = FontLoader.Parse(xmlContent);
        }
        catch
        {
            return null;
        }

        return CreateBitmapFromFontFile(
            fontFile,
            baseZoom,
            effectiveZoom,
            pageDimensions,
            iconSheetWidth,
            iconSheetHeight,
            languageCode,
            fontAsset,
            resolvedPageAssets,
            isRussianAlternateBmFont);
    }

    internal static bool TryInspectBitmapXml(
        string xmlContent,
        out string face,
        out int size,
        out int lineHeight,
        out int scaleW,
        out int scaleH,
        out IReadOnlyList<string> pageFiles,
        out int charCount)
    {
        face = string.Empty;
        size = 0;
        lineHeight = 0;
        scaleW = 0;
        scaleH = 0;
        pageFiles = Array.Empty<string>();
        charCount = 0;

        if (string.IsNullOrWhiteSpace(xmlContent))
            return false;

        try
        {
            FontFile? fontFile = FontLoader.Parse(xmlContent);
            if (fontFile?.Common == null || fontFile.Common.LineHeight <= 0 || fontFile.Pages == null || fontFile.Pages.Count == 0)
                return false;

            face = fontFile.Info?.Face ?? string.Empty;
            size = fontFile.Info?.Size ?? 0;
            lineHeight = fontFile.Common.LineHeight;
            scaleW = fontFile.Common.ScaleW > 0 ? fontFile.Common.ScaleW : 1024;
            scaleH = fontFile.Common.ScaleH > 0 ? fontFile.Common.ScaleH : 1024;

            var pages = new List<string>(fontFile.Pages.Count);
            foreach (FontPage page in fontFile.Pages)
            {
                if (page == null || string.IsNullOrWhiteSpace(page.File))
                    return false;
                pages.Add(page.File);
            }

            pageFiles = pages;
            charCount = fontFile.Chars?.Count ?? 0;
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static DialogueNativeFont? CreateBitmapFromFontFile(
        FontFile? fontFile,
        float baseZoom,
        float effectiveZoom,
        IReadOnlyList<TextureDimensions> pageDimensions,
        int iconSheetWidth = DefaultSpriteSheetWidth,
        int iconSheetHeight = DefaultSpriteSheetHeight,
        string languageCode = "",
        string fontAsset = "",
        IReadOnlyList<string>? resolvedPageAssets = null,
        bool isRussianAlternateBmFont = false)
    {
        if (fontFile?.Common == null || fontFile.Common.LineHeight <= 0)
            return null;
        if (fontFile.Pages == null || fontFile.Pages.Count == 0)
            return null;
        if (pageDimensions == null || pageDimensions.Count != fontFile.Pages.Count)
            return null;
        if (!IsValidZoom(baseZoom) || !IsValidZoom(effectiveZoom))
            return null;

        var boldDims = new TextureDimensions(iconSheetWidth, iconSheetHeight);
        if (!boldDims.IsValid)
            return null;

        var dimArray = new TextureDimensions[pageDimensions.Count];
        for (int i = 0; i < pageDimensions.Count; i++)
        {
            if (!pageDimensions[i].IsValid)
                return null;
            dimArray[i] = pageDimensions[i];
        }

        Dictionary<char, FontChar> charMap = BuildCharacterMap(fontFile);
        if (charMap.Count == 0)
            return null;

        float lineHeight = (fontFile.Common.LineHeight + VerticalSpaceBetweenCharacters) * effectiveZoom;
        IReadOnlyList<string> pageAssets = resolvedPageAssets != null
            ? new List<string>(resolvedPageAssets)
            : Array.Empty<string>();

        return new DialogueNativeFont(
            languageCode: languageCode,
            isBitmapFont: true,
            fontAsset: fontAsset,
            baseZoom: baseZoom,
            effectiveZoom: effectiveZoom,
            lineHeight: lineHeight,
            characterMap: charMap,
            pageTextures: new Texture2D?[pageDimensions.Count],
            pageDimensions: dimArray,
            resolvedPageAssets: pageAssets,
            fontBoldTexture: null,
            fontBoldDimensions: boldDims,
            fontColoredTexture: null,
            fontColoredDimensions: default,
            isRussianAlternateBmFont: isRussianAlternateBmFont);
    }

    /// <summary>
    /// Checks whether every character in <paramref name="text"/> is supported by this native dialogue font
    /// and within its backing texture/page bounds.
    /// </summary>
    public bool Supports(string text)
    {
        if (text == null)
            return false;

        if (!_isBitmapFont)
        {
            if (!_fontBoldDimensions.IsValid || !_fontColoredDimensions.IsValid)
                return false;
        }
        else
        {
            if (_characterMap == null || _pageDimensions.Length == 0 || !_fontBoldDimensions.IsValid)
                return false;
        }

        for (int i = 0; i < text.Length; i++)
        {
            char raw = text[i];
            if (raw == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                    i++;
                continue;
            }
            if (raw == '\n' || raw == '^')
                continue;

            char c = NormalizeChar(raw);
            if (IsIconCharacter(c))
            {
                if (!TryGetSpriteSheetGlyphRect(c, _fontBoldDimensions, out _))
                    return false;
                continue;
            }

            if (!_isBitmapFont)
            {
                if (!TryGetSpriteSheetGlyphRect(c, _fontBoldDimensions, out Rectangle srcRect))
                    return false;
                if (srcRect.Right > _fontColoredDimensions.Width || srcRect.Bottom > _fontColoredDimensions.Height)
                    return false;
            }
            else
            {
                if (!TryGetBitmapGlyph(c, out _, out _, out _))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Measures the native game pixel dimensions of <paramref name="text"/> before final secondary scale (0.75).
    /// Honors <c>\n</c> and <c>^</c> as linebreaks and uses identical horizontal/vertical advances to <see cref="Draw"/>.
    /// </summary>
    public Vector2 Measure(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Vector2.Zero;

        return ExecuteLayout(
            batch: null,
            text: text,
            position: Vector2.Zero,
            color: Color.White,
            scale: 1f,
            depth: 0f);
    }

    /// <summary>
    /// Draws <paramref name="text"/> using native glyph pages or sprite sheets without mutating global <see cref="SpriteText"/> state.
    /// </summary>
    public void Draw(SpriteBatch batch, string text, Vector2 position, Color color, float scale, float depth)
    {
        if (batch == null || string.IsNullOrEmpty(text) || scale <= 0f)
            return;

        ExecuteLayout(batch, text, position, color, scale, depth);
    }

    private Vector2 ExecuteLayout(
        SpriteBatch? batch,
        string text,
        Vector2 position,
        Color color,
        float scale,
        float depth)
    {
        float lineX = 0f;
        float lineY = 0f;
        float maxWidth = 0f;
        int lineCharIndex = 0;
        bool prevWasSpriteSheet = false;

        for (int i = 0; i < text.Length; i++)
        {
            char raw = text[i];
            if (raw == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                    i++;

                maxWidth = Math.Max(maxWidth, lineX);
                lineX = 0f;
                lineY += LineHeight;
                lineCharIndex = 0;
                prevWasSpriteSheet = false;
                continue;
            }

            if (raw == '\n' || raw == '^')
            {
                maxWidth = Math.Max(maxWidth, lineX);
                lineX = 0f;
                lineY += LineHeight;
                lineCharIndex = 0;
                prevWasSpriteSheet = false;
                continue;
            }

            char c = NormalizeChar(raw);
            if (IsIconCharacter(c))
            {
                float iconZoom = _isBitmapFont ? IconPixelZoom : EffectiveZoom;
                if (TryGetSpriteSheetGlyphRect(c, _fontBoldDimensions, out Rectangle iconRect))
                {
                    int widthOffset = GetWidthOffsetForChar(c);
                    if (lineCharIndex > 0 && (!_isBitmapFont || prevWasSpriteSheet))
                    {
                        lineX += widthOffset * iconZoom;
                    }

                    if (batch != null && IsValidTexture(_fontBoldTexture) && IsRectWithinTexture(iconRect, _fontBoldTexture!))
                    {
                        Vector2 drawPos = position + new Vector2(lineX, lineY - 1f * iconZoom) * scale;
                        Color iconColor = Color.White * (color.A / 255f);
                        batch.Draw(
                            _fontBoldTexture!,
                            drawPos,
                            iconRect,
                            iconColor,
                            0f,
                            Vector2.Zero,
                            iconZoom * scale,
                            SpriteEffects.None,
                            depth);
                    }

                    lineX += (SpriteSheetCharWidth + widthOffset) * iconZoom;
                    lineCharIndex++;
                    prevWasSpriteSheet = true;
                }
                continue;
            }

            if (!_isBitmapFont)
            {
                if (TryGetSpriteSheetGlyphRect(c, _fontBoldDimensions, out Rectangle srcRect)
                    && srcRect.Right <= _fontColoredDimensions.Width
                    && srcRect.Bottom <= _fontColoredDimensions.Height)
                {
                    int widthOffset = GetWidthOffsetForChar(c);
                    if (lineCharIndex > 0)
                    {
                        lineX += widthOffset * EffectiveZoom;
                    }

                    if (batch != null && IsValidTexture(_fontColoredTexture) && IsRectWithinTexture(srcRect, _fontColoredTexture!))
                    {
                        float yOffset = GetLatinLetterYOffset(c);
                        Vector2 drawPos = position + new Vector2(lineX, lineY + yOffset * EffectiveZoom) * scale;
                        batch.Draw(
                            _fontColoredTexture!,
                            drawPos,
                            srcRect,
                            color,
                            0f,
                            Vector2.Zero,
                            EffectiveZoom * scale,
                            SpriteEffects.None,
                            depth);
                    }

                    lineX += (SpriteSheetCharWidth + widthOffset) * EffectiveZoom;
                    lineCharIndex++;
                    prevWasSpriteSheet = true;
                }
                continue;
            }

            if (TryGetBitmapGlyph(c, out FontChar fc, out Rectangle bmRect, out int pageIndex))
            {
                if (batch != null && bmRect.Width > 0 && bmRect.Height > 0)
                {
                    Texture2D? pageTexture = _pageTextures[pageIndex];
                    if (IsValidTexture(pageTexture) && IsRectWithinTexture(bmRect, pageTexture!))
                    {
                        Vector2 drawPos = position + new Vector2(
                            lineX + fc.XOffset * EffectiveZoom,
                            lineY + fc.YOffset * EffectiveZoom) * scale;
                        float drawScale = EffectiveZoom * scale;

                        if (_isRussianAlternateBmFont)
                        {
                            Vector2 shadowOffset = new Vector2(-1f, 1f) * drawScale;
                            Color shadowColor = color * RussianShadowAlpha;
                            batch.Draw(pageTexture!, drawPos + shadowOffset, bmRect, shadowColor, 0f, Vector2.Zero, drawScale, SpriteEffects.None, depth);
                            batch.Draw(pageTexture!, drawPos + new Vector2(0f, shadowOffset.Y), bmRect, shadowColor, 0f, Vector2.Zero, drawScale, SpriteEffects.None, depth);
                            batch.Draw(pageTexture!, drawPos + new Vector2(shadowOffset.X, 0f), bmRect, shadowColor, 0f, Vector2.Zero, drawScale, SpriteEffects.None, depth);
                        }

                        batch.Draw(pageTexture!, drawPos, bmRect, color, 0f, Vector2.Zero, drawScale, SpriteEffects.None, depth);
                    }
                }

                lineX += fc.XAdvance * EffectiveZoom;
                lineCharIndex++;
                prevWasSpriteSheet = false;
            }
        }

        maxWidth = Math.Max(maxWidth, lineX);
        float totalHeight = lineY + LineHeight;
        return new Vector2(maxWidth, totalHeight);
    }

    private bool TryGetBitmapGlyph(char c, out FontChar fontChar, out Rectangle srcRect, out int pageIndex)
    {
        fontChar = null!;
        srcRect = default;
        pageIndex = -1;

        if (_characterMap == null || !_characterMap.TryGetValue(c, out FontChar? fc) || fc == null)
            return false;

        if (fc.Page < 0 || fc.Page >= _pageDimensions.Length)
            return false;

        TextureDimensions pageDim = _pageDimensions[fc.Page];
        if (!pageDim.IsValid)
            return false;

        if (fc.X < 0 || fc.Y < 0 || fc.Width < 0 || fc.Height < 0)
            return false;

        if (fc.X + fc.Width > pageDim.Width || fc.Y + fc.Height > pageDim.Height)
            return false;

        fontChar = fc;
        srcRect = new Rectangle(fc.X, fc.Y, fc.Width, fc.Height);
        pageIndex = fc.Page;
        return true;
    }

    internal static char NormalizeChar(char c)
    {
        return c == '♡' ? '<' : c;
    }

    internal static bool IsIconCharacter(char c)
    {
        return c is '<' or '=' or '>' or '@' or '$' or '`' or '+';
    }

    internal static int GetWidthOffsetForChar(char c)
    {
        return c switch
        {
            ',' or '.' => -2,
            '!' or 'j' or 'l' or '¡' => -1,
            'i' or 'ì' or 'í' or 'î' or 'ï' or 'ı' => -1,
            '^' => -8,
            '$' => 1,
            'ş' => -1,
            _ => 0
        };
    }

    private static float GetLatinLetterYOffset(char c)
    {
        bool upper = char.IsUpper(c) || c == 'ß';
        float yOffset = -1f + (upper ? -3f : 0f);
        if (c == 'Ç')
        {
            yOffset += 2f;
        }
        return yOffset;
    }

    internal static bool TryGetSpriteSheetGlyphRect(char c, TextureDimensions sheetDimensions, out Rectangle srcRect)
    {
        srcRect = default;
        if (!sheetDimensions.IsValid || sheetDimensions.Width < SpriteSheetCharWidth || sheetDimensions.Height < SpriteSheetCharHeight)
            return false;

        if (!IsSupportedSpriteSheetChar(c))
            return false;

        Rectangle candidate;
        Texture2D? currentSpriteTexture = SpriteText.spriteTexture;
        if (s_vanillaGetSourceRectForChar != null
            && currentSpriteTexture != null
            && !currentSpriteTexture.IsDisposed
            && currentSpriteTexture.Width == sheetDimensions.Width)
        {
            candidate = s_vanillaGetSourceRectForChar(c, false);
        }
        else
        {
            candidate = GetSourceRectForCharPure(c, sheetDimensions.Width);
        }

        if (candidate.X < 0
            || candidate.Y < 0
            || candidate.Width <= 0
            || candidate.Height <= 0
            || candidate.Right > sheetDimensions.Width
            || candidate.Bottom > sheetDimensions.Height)
        {
            return false;
        }

        srcRect = candidate;
        return true;
    }

    private static bool IsSupportedSpriteSheetChar(char c)
    {
        if ((c >= 32 && c <= 126) || (c >= 160 && c <= 255))
            return true;

        int offset = c - 32;
        if (offset >= 1008 && offset < 1072)
            return true;

        return TryGetSpecialSpriteSheetIndex(c, out _);
    }

    private static Rectangle GetSourceRectForCharPure(char c, int sheetWidth)
    {
        if (!TryGetSpecialSpriteSheetIndex(c, out int i))
        {
            i = c - 32;
            if (i >= 1008 && i < 1040)
            {
                i -= 528;
            }
            else if (i >= 1040 && i < 1072)
            {
                i -= 512;
            }
        }

        return new Rectangle(
            i * SpriteSheetCharWidth % sheetWidth,
            i * SpriteSheetCharWidth / sheetWidth * SpriteSheetCharHeight,
            SpriteSheetCharWidth,
            SpriteSheetCharHeight);
    }

    private static bool TryGetSpecialSpriteSheetIndex(char c, out int index)
    {
        switch (c)
        {
            case 'Œ': index = 96; return true;
            case 'œ': index = 97; return true;
            case 'İ': index = 98; return true;
            case 'ı': index = 99; return true;
            case 'Ş': index = 100; return true;
            case 'ş': index = 101; return true;
            case 'Ğ': index = 102; return true;
            case 'ğ': index = 103; return true;
            case '’': index = 104; return true;
            case 'Ő': index = 105; return true;
            case 'ő': index = 106; return true;
            case 'Ű': index = 107; return true;
            case 'ű': index = 108; return true;
            case '–': index = 464; return true;
            case '—': index = 465; return true;
            case '№': index = 466; return true;
            case 'Ё': index = 512; return true;
            case 'Ґ': index = 513; return true;
            case 'Є': index = 514; return true;
            case 'І': index = 515; return true;
            case 'Ї': index = 516; return true;
            case 'Ў': index = 517; return true;
            case 'ё': index = 560; return true;
            case 'ґ': index = 561; return true;
            case 'є': index = 562; return true;
            case 'і': index = 563; return true;
            case 'ї': index = 564; return true;
            case 'ў': index = 565; return true;
            case 'Ś': index = 574; return true;
            case 'ś': index = 575; return true;
            case 'Ą': index = 576; return true;
            case 'ą': index = 578; return true;
            case 'Ć': index = 579; return true;
            case 'ć': index = 580; return true;
            case 'Ę': index = 581; return true;
            case 'ę': index = 582; return true;
            case 'Ł': index = 583; return true;
            case 'ł': index = 584; return true;
            case 'Ń': index = 585; return true;
            case 'ń': index = 586; return true;
            case 'Ź': index = 587; return true;
            case 'ź': index = 588; return true;
            case 'Ż': index = 589; return true;
            case 'ż': index = 590; return true;
            default:
                index = -1;
                return false;
        }
    }

    private static Dictionary<char, FontChar> BuildCharacterMap(FontFile fontFile)
    {
        var map = new Dictionary<char, FontChar>();
        if (fontFile.Chars == null)
            return map;

        foreach (FontChar fc in fontFile.Chars)
        {
            if (fc == null || fc.ID < 0 || fc.ID > char.MaxValue)
                continue;

            char ch = (char)fc.ID;
            map[ch] = fc;
        }

        return map;
    }

    private static bool IsValidZoom(float zoom)
    {
        return zoom > 0f && !float.IsNaN(zoom) && !float.IsInfinity(zoom);
    }

    private static bool IsValidTexture(Texture2D? texture)
    {
        return texture != null && !texture.IsDisposed && texture.Width > 0 && texture.Height > 0;
    }

    private static bool IsRectWithinTexture(Rectangle rect, Texture2D texture)
    {
        return rect.X >= 0
            && rect.Y >= 0
            && rect.Right <= texture.Width
            && rect.Bottom <= texture.Height;
    }

    private static Func<char, bool, Rectangle>? CreateVanillaSourceRectDelegate()
    {
        try
        {
            MethodInfo? method = typeof(SpriteText).GetMethod(
                "getSourceRectForChar",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(char), typeof(bool) },
                modifiers: null);

            if (method != null && method.ReturnType == typeof(Rectangle))
            {
                return (Func<char, bool, Rectangle>)Delegate.CreateDelegate(
                    typeof(Func<char, bool, Rectangle>),
                    method);
            }
        }
        catch
        {
            // Ignore reflection errors and rely on pure glyph rectangle mapping.
        }

        return null;
    }
}
