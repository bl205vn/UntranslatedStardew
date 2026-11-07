using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using StardewValley.BellsAndWhistles;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static void TestNativeDialogueFont()
    {
        Console.WriteLine("\n-- Testing DialogueNativeFont & DialogueNativeFontProvider --");

        var initialSpriteTexture = SpriteText.spriteTexture;
        var initialColoredTexture = SpriteText.coloredTexture;
        var initialFontFile = typeof(SpriteText).GetField("FontFile")?.GetValue(null);
        var initialCharMap = typeof(SpriteText).GetField("characterMap")?.GetValue(null);

        // 1. Pure Native Latin font metrics, supports, width offsets, heart alias, linebreaks, bounds
        DialogueNativeFont? latin = DialogueNativeFont.CreateLatinFromMetrics("en", zoom: 3f, sheetWidth: 128, sheetHeight: 608, coloredWidth: 128, coloredHeight: 608);
        Assert(latin != null && !latin.IsBitmapFont, "NativeLatin: Created headlessly from metrics");
        Assert(latin != null && Math.Abs(latin.LineHeight - 54f) < 0.001f, "NativeLatin: LineHeight is (16 + 2) * 3 = 54", $"Got {latin?.LineHeight}");

        if (latin != null)
        {
            Assert(latin.Supports("Hello, World! 100$ < = > @ ` + ♡"), "NativeLatin: Supports ASCII, icons, and heart alias ♡");
            Assert(latin.Supports("äöüß éèêœŒ ĞğİıŞş ŐőŰű Привет Ёё ĄąĆćĘęŁłŃńŚśŹźŻż"), "NativeLatin: Supports extended European, Cyrillic, and Polish glyphs");
            Assert(!latin.Supports("Xin chào tiếng Việt đ ă ơ ư ệ"), "NativeLatin: Rejects unmapped Vietnamese diacritics in vanilla font_bold");
            Assert(!latin.Supports("日本語 한국어 中文"), "NativeLatin: Rejects CJK characters");

            Vector2 emptyMeasure = latin.Measure("");
            Assert(emptyMeasure == Vector2.Zero, "NativeLatin: Empty string measures to Vector2.Zero");

            Vector2 singleA = latin.Measure("A");
            Assert(Math.Abs(singleA.X - 24f) < 0.001f && Math.Abs(singleA.Y - 54f) < 0.001f,
                "NativeLatin: Single 'A' measures 24x54 in native pixels", $"Got {singleA}");

            // Width offsets: '.' has offset -2, '$' has offset +1
            Vector2 singleDot = latin.Measure(".");
            Assert(Math.Abs(singleDot.X - 18f) < 0.001f, "NativeLatin: '.' width includes -2 offset ((8-2)*3 = 18)", $"Got {singleDot.X}");

            Vector2 twoDots = latin.Measure("..");
            Assert(Math.Abs(twoDots.X - 30f) < 0.001f, "NativeLatin: '..' width uses pairwise width offsets (18 + (-6 + 18) = 30)", $"Got {twoDots.X}");

            Vector2 dollar = latin.Measure("$");
            Assert(Math.Abs(dollar.X - 27f) < 0.001f, "NativeLatin: '$' width includes +1 offset ((8+1)*3 = 27)", $"Got {dollar.X}");

            // Heart alias ♡ normalized to '<'
            Vector2 heartAlias = latin.Measure("A♡B");
            Vector2 heartIcon = latin.Measure("A<B");
            Assert(heartAlias == heartIcon && Math.Abs(heartAlias.X - 72f) < 0.001f,
                "NativeLatin: Heart alias '♡' normalizes to '<' with identical advance", $"Got {heartAlias} vs {heartIcon}");

            // Multi-line \n, \r\n, and ^
            Vector2 multiline = latin.Measure("AB\nCD^EF\r\nG");
            Assert(Math.Abs(multiline.X - 48f) < 0.001f && Math.Abs(multiline.Y - 216f) < 0.001f,
                "NativeLatin: Honors \\n, ^, and \\r\\n as linebreaks (4 lines = 216px height)", $"Got {multiline}");
        }

        // Bounds check on truncated Latin texture sheet
        DialogueNativeFont? tinyLatin = DialogueNativeFont.CreateLatinFromMetrics("en", zoom: 3f, sheetWidth: 128, sheetHeight: 32, coloredWidth: 128, coloredHeight: 32);
        Assert(tinyLatin != null && tinyLatin.Supports(" !\"#") && !tinyLatin.Supports("A"),
            "NativeLatin Bounds: Rejects glyph whose source rectangle exceeds texture height");

        // 2. Multi-page BMFont lookup, bounds check, icon support despite BMFont missing icon, and native advances
        string multiPageXml = @"<?xml version=""1.0""?>
<font>
  <info face=""TestMultiPage"" size=""32"" />
  <common lineHeight=""32"" base=""26"" scaleW=""256"" scaleH=""256"" pages=""3"" />
  <pages>
    <page id=""0"" file=""TestMulti_0.png"" />
    <page id=""1"" file=""TestMulti_1.png"" />
    <page id=""2"" file=""TestMulti_2.png"" />
  </pages>
  <chars count=""6"">
    <char id=""32"" x=""0"" y=""0"" width=""0"" height=""0"" xoffset=""0"" yoffset=""0"" xadvance=""10"" page=""0"" chnl=""15"" />
    <char id=""26085"" x=""10"" y=""10"" width=""24"" height=""24"" xoffset=""1"" yoffset=""2"" xadvance=""26"" page=""0"" chnl=""15"" />
    <char id=""26412"" x=""40"" y=""20"" width=""24"" height=""24"" xoffset=""1"" yoffset=""2"" xadvance=""28"" page=""1"" chnl=""15"" />
    <char id=""35486"" x=""80"" y=""50"" width=""24"" height=""24"" xoffset=""0"" yoffset=""2"" xadvance=""30"" page=""2"" chnl=""15"" />
    <char id=""28450"" x=""240"" y=""10"" width=""30"" height=""24"" xoffset=""0"" yoffset=""0"" xadvance=""30"" page=""1"" chnl=""15"" />
    <char id=""23383"" x=""10"" y=""10"" width=""20"" height=""20"" xoffset=""0"" yoffset=""0"" xadvance=""20"" page=""5"" chnl=""15"" />
  </chars>
</font>";

        var pageDims = new[]
        {
            new DialogueNativeFont.TextureDimensions(256, 256),
            new DialogueNativeFont.TextureDimensions(256, 256),
            new DialogueNativeFont.TextureDimensions(256, 256)
        };

        DialogueNativeFont? bmMulti = DialogueNativeFont.CreateBitmapFromXml(
            multiPageXml,
            baseZoom: 1.5f,
            effectiveZoom: 1.5f,
            pageDimensions: pageDims,
            languageCode: "ja",
            fontAsset: "Fonts/TestMulti");

        Assert(bmMulti != null && bmMulti.IsBitmapFont && bmMulti.PageCount == 3, "NativeBMFont: Created 3-page BMFont headlessly");
        if (bmMulti != null)
        {
            // LineHeight = (32 + 2) * 1.5 = 51
            Assert(Math.Abs(bmMulti.LineHeight - 51f) < 0.001f, "NativeBMFont: LineHeight is (32 + 2) * 1.5 = 51", $"Got {bmMulti.LineHeight}");
            Assert(bmMulti.Supports("日本語"), "NativeBMFont: Supports glyphs across pages 0, 1, and 2");
            Assert(!bmMulti.Supports("漢"), "NativeBMFont Bounds: Rejects glyph exceeding page texture width (240 + 30 > 256)");
            Assert(!bmMulti.Supports("字"), "NativeBMFont Bounds: Rejects glyph referencing out-of-range page index 5");
            Assert(!bmMulti.Supports("A"), "NativeBMFont: Rejects unmapped ASCII letter not in BMFont char map");

            // Icons (< = > @ $ ` + and ♡) are NOT in multiPageXml, yet must be supported via shared sprite sheet
            Assert(bmMulti.Supports("日本語 < = > @ $ ` + ♡"),
                "NativeBMFont Icons: Supports all shared icons and ♡ even when missing from .fnt char map");

            // Measure advance: '日' (26 * 1.5 = 39) + '本' (28 * 1.5 = 42) + '<' (icon 8 * 3 = 24) = 105
            Vector2 measuredIconMix = bmMulti.Measure("日本♡");
            Vector2 measuredExplicitLt = bmMulti.Measure("日本<");
            Assert(measuredIconMix == measuredExplicitLt && Math.Abs(measuredIconMix.X - 105f) < 0.001f && Math.Abs(measuredIconMix.Y - 51f) < 0.001f,
                "NativeBMFont Advance: Combines BMFont XAdvance*zoom (39+42) with shared icon advance (24) = 105",
                $"Got {measuredIconMix}");

            // Multi-line with ^ and \n
            Vector2 bmMultiline = bmMulti.Measure("日本^語\n<");
            Assert(Math.Abs(bmMultiline.X - 81f) < 0.001f && Math.Abs(bmMultiline.Y - 153f) < 0.001f,
                "NativeBMFont Newline: Honors ^ and \\n (max width = 39+42 = 81, height = 3 * 51 = 153)",
                $"Got {bmMultiline}");
        }

        // 3. Asset-page resolution & path boundary validation
        Assert(DialogueNativeFontProvider.TryBuildPageAssetCandidates("Fonts/Japanese", "Japanese_0.png", out var jaCandidates)
            && jaCandidates.Count >= 2
            && jaCandidates[0] == "Fonts/Japanese_0.png"
            && jaCandidates[1] == "Fonts/Japanese_0",
            "PageResolution: Handles .png and unsuffixed fallback in Fonts/");

        Assert(DialogueNativeFontProvider.TryBuildPageAssetCandidates(@"Fonts\Chinese_round\Chinese.fnt", "Chinese_0", out var zhRoundCandidates)
            && zhRoundCandidates[0] == "Fonts/Chinese_round/Chinese_0"
            && zhRoundCandidates[1] == "Fonts/Chinese_round/Chinese_0.png",
            "PageResolution: Normalizes backslashes, strips .fnt on descriptor, and provides .png variant");

        Assert(!DialogueNativeFontProvider.TryBuildPageAssetCandidates("C:/Fonts/Evil", "Evil_0.png", out _),
            "PageResolution Security: Rejects drive-rooted font asset");
        Assert(!DialogueNativeFontProvider.TryBuildPageAssetCandidates("/Fonts/Evil", "Evil_0.png", out _),
            "PageResolution Security: Rejects slash-rooted font asset");
        Assert(!DialogueNativeFontProvider.TryBuildPageAssetCandidates("Fonts/Safe", "../Evil_0.png", out _),
            "PageResolution Security: Rejects parent traversal in page file");
        Assert(!DialogueNativeFontProvider.TryBuildPageAssetCandidates("Fonts/../Evil", "Evil_0.png", out _),
            "PageResolution Security: Rejects parent traversal in font asset");

        // 4. Provider locale policies, options caching (ru, zh), custom registry (vi, Latin, missing native decl), once-only diagnostics
        bool useAltRussian = false;
        bool useZhSmooth = false;
        float zhDialogueScale = 1.0f;
        var logs = new List<string>();
        var registry = new LanguageSourceRegistry();

        var xmlMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Fonts/Russian"] = BuildSimpleFntXml("BasicSmaller4Ext", size: 15, lineHeight: 15, pageCount: 1, "Russian", ('Б', 6, 0)),
            ["Fonts/Japanese"] = BuildSimpleFntXml("SetoFont-SP", size: 24, lineHeight: 24, pageCount: 2, "Japanese", ('あ', 20, 0), ('い', 20, 1)),
            ["Fonts/Korean"] = BuildSimpleFntXml("Sandoll MiSaeng", size: 48, lineHeight: 49, pageCount: 12, "Korean", ('한', 28, 0), ('글', 28, 11)),
            ["Fonts/Chinese"] = BuildSimpleFntXml("Noto Sans CJK SC Bold", size: 32, lineHeight: 33, pageCount: 4, "Chinese", ('中', 32, 0), ('文', 32, 3)),
            ["Fonts/Chinese_round/Chinese"] = BuildSimpleFntXml("SmoothZh", size: 42, lineHeight: 54, pageCount: 1, "Chinese", ('中', 42, 0)),
            ["Fonts/NewFont"] = BuildSimpleFntXml("StardewValley_K", size: 48, lineHeight: 48, pageCount: 1, "NewFont", ('ă', 22, 0), ('đ', 24, 0), ('ệ', 22, 0))
        };

        using var provider = new DialogueNativeFontProvider(
            registry: registry,
            log: msg => logs.Add(msg),
            xmlLoader: asset => xmlMap.TryGetValue(asset, out string? xml) ? xml : null,
            metricTextureLoader: asset =>
            {
                if (string.Equals(asset, DialogueNativeFontProvider.FontBoldAsset, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(asset, DialogueNativeFontProvider.FontColoredAsset, StringComparison.OrdinalIgnoreCase))
                {
                    return new DialogueNativeFont.TextureDimensions(128, 608);
                }
                if (asset.StartsWith("Fonts/", StringComparison.OrdinalIgnoreCase) && !asset.Contains("Thai", StringComparison.OrdinalIgnoreCase))
                {
                    return new DialogueNativeFont.TextureDimensions(512, 512);
                }
                return null;
            },
            useAlternateRussianFont: () => useAltRussian,
            useChineseSmoothFont: () => useZhSmooth,
            chineseDialogueFontScale: () => zhDialogueScale);

        // Built-in Latin locales
        string[] latinLocales = { "en", "es", "de", "fr", "pt", "it", "tr", "hu" };
        foreach (string loc in latinLocales)
        {
            DialogueNativeFont? f = provider.Get(loc);
            Assert(f != null && !f.IsBitmapFont && Math.Abs(f.EffectiveZoom - 3f) < 0.001f,
                $"Provider Policy: Built-in '{loc}' uses native Latin font_bold at zoom 3");
        }
        Assert(ReferenceEquals(provider.Get("en"), provider.Get("en-US")), "Provider Cache: Canonicalizes 'en-US' to cached 'en' instance");

        // Russian option policy
        useAltRussian = false;
        DialogueNativeFont? ruDefault = provider.Get("ru-RU");
        Assert(ruDefault != null && !ruDefault.IsBitmapFont && Math.Abs(ruDefault.EffectiveZoom - 3f) < 0.001f,
            "Provider Policy (ru): Default uses Latin/Cyrillic sprite sheet at zoom 3");

        useAltRussian = true;
        DialogueNativeFont? ruAlt = provider.Get("ru");
        Assert(ruAlt != null && ruAlt.IsBitmapFont && ruAlt.FontAsset == "Fonts/Russian" && Math.Abs(ruAlt.EffectiveZoom - 3f) < 0.001f && Math.Abs(ruAlt.LineHeight - 51f) < 0.001f,
            "Provider Policy (ru): useAlternateFont=true switches to Fonts/Russian BMFont at zoom 3");

        // Japanese & Korean
        DialogueNativeFont? jaFont = provider.Get("ja-JP");
        Assert(jaFont != null && jaFont.IsBitmapFont && jaFont.FontAsset == "Fonts/Japanese" && Math.Abs(jaFont.EffectiveZoom - 1.75f) < 0.001f && jaFont.PageCount == 2 && jaFont.Supports("あい♡"),
            "Provider Policy (ja): Uses Fonts/Japanese BMFont at zoom 1.75 with 2 pages");

        DialogueNativeFont? koFont = provider.Get("ko-KR");
        Assert(koFont != null && koFont.IsBitmapFont && koFont.FontAsset == "Fonts/Korean" && Math.Abs(koFont.EffectiveZoom - 1.5f) < 0.001f && koFont.PageCount == 12 && koFont.Supports("한글♡"),
            "Provider Policy (ko): Uses Fonts/Korean BMFont at zoom 1.5 with 12 pages");

        // Chinese normal vs smooth + dialogueFontScale adjustment
        useZhSmooth = false;
        zhDialogueScale = 1.0f;
        DialogueNativeFont? zhNormal = provider.Get("zh-CN");
        Assert(zhNormal != null && zhNormal.FontAsset == "Fonts/Chinese" && Math.Abs(zhNormal.EffectiveZoom - 1.5f) < 0.001f && zhNormal.PageCount == 4,
            "Provider Policy (zh): Normal uses Fonts/Chinese at zoom 1.5 with 4 pages");

        zhDialogueScale = 1.4f;
        DialogueNativeFont? zhNormalScaled = provider.Get("zh");
        Assert(zhNormalScaled != null && Math.Abs(zhNormalScaled.EffectiveZoom - 1.7f) < 0.001f,
            "Provider Policy (zh): Normal applies SpriteText dialogueFontScale formula 1.5 + (1.4 - 1)/2 = 1.7",
            $"Got {zhNormalScaled?.EffectiveZoom}");

        useZhSmooth = true;
        zhDialogueScale = 1.0f;
        DialogueNativeFont? zhSmooth = provider.Get("zh");
        Assert(zhSmooth != null && zhSmooth.FontAsset == "Fonts/Chinese_round/Chinese" && Math.Abs(zhSmooth.EffectiveZoom - 1.0f) < 0.001f && zhSmooth.PageCount == 1,
            "Provider Policy (zh): Smooth uses Fonts/Chinese_round/Chinese at zoom 1.0");

        zhDialogueScale = 1.4f;
        DialogueNativeFont? zhSmoothScaled = provider.Get("zh");
        Assert(zhSmoothScaled != null && Math.Abs(zhSmoothScaled.EffectiveZoom - 1.1f) < 0.001f,
            "Provider Policy (zh): Smooth applies SpriteText dialogueFontScale formula 1.0 + (1.4 - 1)/4 = 1.1",
            $"Got {zhSmoothScaled?.EffectiveZoom}");

        // Thai fail-closed when assets are absent
        int logsBeforeTh = logs.Count;
        Assert(provider.Get("th") == null && provider.Get("th-TH") == null, "Provider Policy (th): Fails closed when Fonts/Thai assets are absent");
        Assert(logs.Count == logsBeforeTh + 1, "Provider Diagnostic (th): Logs missing Thai asset diagnostic only once");

        // Unknown locale returns null with once diagnostic
        int logsBeforeUnknown = logs.Count;
        Assert(provider.Get("tlh") == null && provider.Get("tlh") == null, "Provider Unknown Locale: Returns null for unregistered 'tlh'");
        Assert(logs.Count == logsBeforeUnknown + 1, "Provider Unknown Locale: Logs once diagnostic across repeated Get('tlh') calls");

        // Custom language without native declaration (only UI SpriteFont1 / SmallFont) must return null and NOT silently use SpriteFont1
        registry.Register(new LanguageSourceDescriptor
        {
            LanguageCode = "uk",
            SourceModId = "Test.UkrainianUIOnly",
            SmallFont = "Fonts/SmallFont_uk",
            DialogueFont = "Fonts/SpriteFont1_uk"
        });
        int logsBeforeUk = logs.Count;
        Assert(provider.Get("uk") == null && provider.Get("uk") == null,
            "Provider Custom Policy: Rejects custom language lacking UseGameLatinFont/BitmapFontAsset (no silent SpriteFont1 fallback)");
        Assert(logs.Count == logsBeforeUk + 1,
            "Provider Custom Policy: Logs once diagnostic for custom locale missing native dialogue declaration");

        // Custom language with active BMFont declaration (Vietnamese NewFont)
        registry.Register(new LanguageSourceDescriptor
        {
            LanguageCode = "vi",
            SourceModId = "Kou.ViethoaSV",
            BitmapFontAsset = "Fonts/NewFont",
            BitmapFontZoom = 1f,
            UseGameLatinFont = false,
            DialogueFont = "Fonts/SpriteFont1_vi1"
        });
        DialogueNativeFont? viFont = provider.Get("vi-VN");
        Assert(viFont != null && viFont.IsBitmapFont && viFont.FontAsset == "Fonts/NewFont" && Math.Abs(viFont.EffectiveZoom - 1f) < 0.001f && viFont.Supports("ăđệ♡"),
            "Provider Custom Policy (vi): Uses declared BitmapFontAsset 'Fonts/NewFont' at zoom 1 instead of UI SpriteFont1");

        // Custom language with UseGameLatinFont = true
        registry.Register(new LanguageSourceDescriptor
        {
            LanguageCode = "eo",
            SourceModId = "Test.EsperantoLatin",
            UseGameLatinFont = true
        });
        DialogueNativeFont? eoFont = provider.Get("eo");
        Assert(eoFont != null && !eoFont.IsBitmapFont && Math.Abs(eoFont.EffectiveZoom - 3f) < 0.001f,
            "Provider Custom Policy (eo): UseGameLatinFont=true resolves to native Latin font_bold at zoom 3");

        // 5. Headless verification against actual installed .fnt files when present
        VerifyInstalledFntMetadataHeadlessly();

        Assert(ReferenceEquals(SpriteText.spriteTexture, initialSpriteTexture)
            && ReferenceEquals(SpriteText.coloredTexture, initialColoredTexture)
            && ReferenceEquals(typeof(SpriteText).GetField("FontFile")?.GetValue(null), initialFontFile)
            && ReferenceEquals(typeof(SpriteText).GetField("characterMap")?.GetValue(null), initialCharMap),
            "Global State Invariant: SpriteText static fields were not mutated");
    }

    private static string BuildSimpleFntXml(
        string face,
        int size,
        int lineHeight,
        int pageCount,
        string pagePrefix,
        params (char Ch, int Advance, int Page)[] chars)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0""?>");
        sb.AppendLine("<font>");
        sb.AppendLine($@"  <info face=""{face}"" size=""{size}"" />");
        sb.AppendLine($@"  <common lineHeight=""{lineHeight}"" base=""{size}"" scaleW=""512"" scaleH=""512"" pages=""{pageCount}"" />");
        sb.AppendLine("  <pages>");
        for (int i = 0; i < pageCount; i++)
        {
            sb.AppendLine($@"    <page id=""{i}"" file=""{pagePrefix}_{i}.png"" />");
        }
        sb.AppendLine("  </pages>");
        sb.AppendLine($@"  <chars count=""{chars.Length + 1}"">");
        sb.AppendLine(@"    <char id=""32"" x=""0"" y=""0"" width=""0"" height=""0"" xoffset=""0"" yoffset=""0"" xadvance=""8"" page=""0"" chnl=""15"" />");
        foreach (var (ch, adv, page) in chars)
        {
            sb.AppendLine($@"    <char id=""{(int)ch}"" x=""4"" y=""4"" width=""16"" height=""16"" xoffset=""0"" yoffset=""0"" xadvance=""{adv}"" page=""{page}"" chnl=""15"" />");
        }
        sb.AppendLine("  </chars>");
        sb.AppendLine("</font>");
        return sb.ToString();
    }

    private static void VerifyInstalledFntMetadataHeadlessly()
    {
        string? gamePath = LocateGamePath();
        if (string.IsNullOrEmpty(gamePath))
            return;

        // Check installed vanilla .fnt descriptors in Content/Fonts
        (string RelativePath, string Locale, float BaseZoom, int ExpectedLineHeight, int ExpectedPages, string SampleText)[] vanillaFnts =
        {
            (Path.Combine("Content", "Fonts", "Chinese.fnt"), "zh", 1.5f, 33, 4, "中文 < = > @ $ ` + ♡"),
            (Path.Combine("Content", "Fonts", "Chinese_round", "Chinese.fnt"), "zh", 1.0f, 54, 1, "中文 ♡"),
            (Path.Combine("Content", "Fonts", "Japanese.fnt"), "ja", 1.75f, 24, 2, "日本語 ♡"),
            (Path.Combine("Content", "Fonts", "Korean.fnt"), "ko", 1.5f, 49, 12, "한국어 ♡"),
            (Path.Combine("Content", "Fonts", "Russian.fnt"), "ru", 3.0f, 15, 1, "Привет ♡")
        };

        foreach (var entry in vanillaFnts)
        {
            string fullPath = Path.Combine(gamePath, entry.RelativePath);
            if (!File.Exists(fullPath))
                continue;

            string xml = File.ReadAllText(fullPath);
            bool inspected = DialogueNativeFont.TryInspectBitmapXml(
                xml,
                out _,
                out _,
                out int parsedLineHeight,
                out int scaleW,
                out int scaleH,
                out IReadOnlyList<string> pageFiles,
                out int charCount);

            Assert(inspected
                && parsedLineHeight == entry.ExpectedLineHeight
                && pageFiles.Count == entry.ExpectedPages
                && charCount > 0,
                $"Installed {entry.RelativePath}: Parsed lineHeight={parsedLineHeight}, pages={pageFiles.Count}, chars={charCount}");

            var dims = new DialogueNativeFont.TextureDimensions[pageFiles.Count];
            for (int i = 0; i < dims.Length; i++)
                dims[i] = new DialogueNativeFont.TextureDimensions(scaleW, scaleH);

            DialogueNativeFont? font = DialogueNativeFont.CreateBitmapFromXml(
                xml,
                baseZoom: entry.BaseZoom,
                effectiveZoom: entry.BaseZoom,
                pageDimensions: dims,
                languageCode: entry.Locale,
                fontAsset: entry.RelativePath);

            float expectedTotalLineHeight = (entry.ExpectedLineHeight + 2) * entry.BaseZoom;
            Assert(font != null
                && Math.Abs(font.LineHeight - expectedTotalLineHeight) < 0.001f
                && font.PageCount == entry.ExpectedPages
                && font.Supports(entry.SampleText),
                $"Installed {entry.RelativePath}: Supports '{entry.SampleText}' across {entry.ExpectedPages} page(s) with native LineHeight={expectedTotalLineHeight}");
        }

        // Check installed Vietnamese pack NewFont.fnt if present
        string viFntPath = Path.Combine(gamePath, "Mods", "[CP] Viet Hoa", "assets", "Content", "Fonts", "Font-Vi", "NewFont.fnt");
        if (File.Exists(viFntPath))
        {
            string xml = File.ReadAllText(viFntPath);
            bool inspected = DialogueNativeFont.TryInspectBitmapXml(
                xml,
                out _,
                out _,
                out int parsedLineHeight,
                out int scaleW,
                out int scaleH,
                out IReadOnlyList<string> pageFiles,
                out _);

            Assert(inspected && parsedLineHeight == 48 && pageFiles.Count == 1,
                "Installed NewFont.fnt: Metadata has lineHeight=48 and 1 page");

            var dims = new DialogueNativeFont.TextureDimensions[pageFiles.Count];
            for (int i = 0; i < dims.Length; i++)
                dims[i] = new DialogueNativeFont.TextureDimensions(scaleW, scaleH);

            DialogueNativeFont? realViFont = DialogueNativeFont.CreateBitmapFromXml(
                xml,
                baseZoom: 1f,
                effectiveZoom: 1f,
                pageDimensions: dims,
                languageCode: "vi",
                fontAsset: "Fonts/NewFont");

            Assert(realViFont != null
                && Math.Abs(realViFont.LineHeight - 50f) < 0.001f
                && realViFont.Supports("Xin chào! < = > @ $ ` + ♡"),
                "Installed NewFont.fnt: Parsed headlessly with lineHeight=48 (total 50), supports Vietnamese + icons");
        }
    }
}
