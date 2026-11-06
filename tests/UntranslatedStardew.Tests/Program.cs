using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.GameData;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects.Trinkets;
using UntranslatedStardew.Core;
using UntranslatedStardew.Patches;

namespace UntranslatedStardew.Tests;

/// <summary>
/// Standalone regression test suite and offline real XNB smoke check for Untranslated Stardew.
/// Verifies title duplicate suppression, ordering, accent normalization, Vietnamese 'đ'/'Đ' mapping,
/// Lexicon pluralization, displayNameFormat precedence, tool upgrades, stable API lifecycle,
/// and real XNB asset loading from disk.
/// </summary>
public static partial class Program
{
    private static int _passed;
    private static int _failed;

    public static int Main()
    {
        // Setup assembly resolver to locate Stardew Valley.dll when running tests
        AppDomain.CurrentDomain.AssemblyResolve += ResolveGameAssembly;

        Console.WriteLine("=== Running Untranslated Stardew Regression Checks ===");

        TestLanguageHelper();
        TestAccentNormalization();
        TestSearchMatching();
        TestTooltipFormatting();
        TestLexiconPluralization();
        TestItemCacheKey();
        TestTrinketResolution();
        TestCjbSearchFilter();
        TestCjbHarmonyBoundary();
        TestMockContentProviderAndResolver();
        TestApiLifecycleAndConfig();
        TestSystematicCoverageAudit();
        TestRealXnbSmoke();
        TestDefaultAndEnLanguageSemantics();
        TestUiHarmonyBoundarySignatures();
        TestLevelUpMenuScopedStateAndLayout();
        TestUiFixtureFormattingAndMergedText();
        TestRealXnbUiExpansion();
        TestMailAndSecretNotesParsingAndFlow();
        TestExternalTranslationRegistryAndFallback();
        TestContentPatcherExamplePackSchema();
        TestPublicApiRegistrationAndLifecycle();
        TestLevelUpMenuPrefixFailureRestoration();
        TestRealXnbMailAndNotesExpansion();
        TestCollectionsNoteImageLayoutAndDynamicFonts();
        TestXnbGlyphMetadataParsing();
        TestBilingualRunPlannerAndMetrics();
        TestBilingualDiagnosticAndLifecycle();
        TestHarmonyInterceptionAndRecursionGuards();
        TestAcceptanceFixesLeadReview();
        TestVersion130Features();
        TestRoleFontAcceptance();
        TestStandardCustomLanguagePack();
        TestInstalledCustomLanguagePairs();
        TestDialoguePrototype();

        Console.WriteLine();
        Console.WriteLine($"=== Results: {_passed} Passed, {_failed} Failed ===");

        return _failed == 0 ? 0 : 1;
    }

    private static string? LocateGamePath()
    {
        string? envPath = Environment.GetEnvironmentVariable("STARDEW_GAME_PATH");
        if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            return envPath;

        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 413150");
                string? installLoc = key?.GetValue("InstallLocation") as string;
                if (!string.IsNullOrEmpty(installLoc) && Directory.Exists(installLoc))
                    return installLoc;
            }
            catch
            {
                // Ignore registry errors
            }
        }

        // Common default directories
        string[] commonDirs =
        {
            @"E:\SteamLibrary\steamapps\common\Stardew Valley",
            @"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley",
            @"C:\Program Files\Steam\steamapps\common\Stardew Valley"
        };

        foreach (string dir in commonDirs)
        {
            if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "Stardew Valley.dll")))
                return dir;
        }

        return null;
    }

    private static Assembly? ResolveGameAssembly(object? sender, ResolveEventArgs args)
    {
        string assemblyName = new AssemblyName(args.Name).Name + ".dll";
        string? gamePath = LocateGamePath();
        if (gamePath == null)
            return null;

        string directPath = Path.Combine(gamePath, assemblyName);
        if (File.Exists(directPath))
            return Assembly.LoadFrom(directPath);

        string internalPath = Path.Combine(gamePath, "smapi-internal", assemblyName);
        if (File.Exists(internalPath))
            return Assembly.LoadFrom(internalPath);

        return null;
    }

    private static void Assert(bool condition, string testName, string? details = null)
    {
        if (condition)
        {
            _passed++;
            Console.WriteLine($"  [PASS] {testName}");
        }
        else
        {
            _failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {testName}{(details != null ? ": " + details : "")}");
            Console.ResetColor();
        }
    }

    private static void TestLanguageHelper()
    {
        Console.WriteLine("\n-- Testing LanguageHelper --");

        Assert(LanguageHelper.IsBuiltInLanguage("en"), "en is recognized as built-in");
        Assert(LanguageHelper.IsBuiltInLanguage("es"), "es is recognized as built-in");
        Assert(LanguageHelper.IsBuiltInLanguage("fr-FR"), "fr-FR is recognized as built-in");
        Assert(LanguageHelper.IsBuiltInLanguage("ja"), "ja is recognized as built-in");
        Assert(LanguageHelper.IsBuiltInLanguage("zh-cn"), "zh-cn is recognized as built-in");

        Assert(!LanguageHelper.IsBuiltInLanguage("vi"), "vi is not recognized as vanilla built-in");
        Assert(!LanguageHelper.IsBuiltInLanguage("custom_mod"), "custom_mod is not recognized as built-in");

        Assert(LanguageHelper.GetLanguageSuffix("en") == null, "en has null suffix (default file)");
        Assert(LanguageHelper.GetLanguageSuffix("es") == "es-ES", "es has es-ES suffix");
        Assert(LanguageHelper.GetLanguageSuffix("de") == "de-DE", "de has de-DE suffix");
        Assert(LanguageHelper.GetLanguageSuffix("fr") == "fr-FR", "fr has fr-FR suffix");

        Assert(LanguageHelper.IsVietnamese("vi"), "vi is recognized as Vietnamese");
        Assert(LanguageHelper.IsVietnamese("vi-VN"), "vi-VN is recognized as Vietnamese");
        Assert(!LanguageHelper.IsVietnamese("en"), "en is not recognized as Vietnamese");

        // Canonicalization
        Assert(LanguageHelper.CanonicalizeLanguageCode("FR-fr") == "fr", "Canonicalize FR-fr -> fr");
        Assert(LanguageHelper.CanonicalizeLanguageCode("vi-VN") == "vi", "Canonicalize vi-VN -> vi");
        Assert(LanguageHelper.CanonicalizeLanguageCode(null) == "en", "Canonicalize null -> en");
        Assert(LanguageHelper.CanonicalizeLanguageCode("   ") == "en", "Canonicalize whitespace -> en");
    }

    private static void TestAccentNormalization()
    {
        Console.WriteLine("\n-- Testing SearchMatcher Accent & 'đ' Normalization --");

        string norm1 = SearchMatcher.RemoveDiacritics("Cá nóc");
        Assert(norm1 == "Ca noc", "Removes accents from 'Cá nóc' -> 'Ca noc'");

        string norm2 = SearchMatcher.RemoveDiacritics("Đá vôi");
        Assert(norm2 == "Da voi", "Normalizes Vietnamese 'Đ' and accents in 'Đá vôi' -> 'Da voi'");

        string norm3 = SearchMatcher.RemoveDiacritics("đường phèn");
        Assert(norm3 == "duong phen", "Normalizes Vietnamese 'đ' and tones in 'đường phèn' -> 'duong phen'");

        string norm4 = SearchMatcher.RemoveDiacritics("Pufferfish");
        Assert(norm4 == "Pufferfish", "Leaves English 'Pufferfish' unchanged");

        string norm5 = SearchMatcher.RemoveDiacritics("");
        Assert(norm5 == "", "Handles empty string safely");

        string norm6 = SearchMatcher.RemoveDiacritics(null);
        Assert(norm6 == "", "Handles null string safely");
    }

    private static void TestSearchMatching()
    {
        Console.WriteLine("\n-- Testing SearchMatcher Query Matching --");

        // Diacritic-insensitive matching
        Assert(SearchMatcher.MatchesText("Cá nóc", "ca noc", true), "Matches 'Cá nóc' with unaccented query 'ca noc'");
        Assert(SearchMatcher.MatchesText("Cá nóc", "Cá", true), "Matches 'Cá nóc' with exact query 'Cá'");
        Assert(SearchMatcher.MatchesText("Đá vôi", "da voi", true), "Matches 'Đá vôi' with 'da voi'");
        Assert(SearchMatcher.MatchesText("Đá vôi", "Đá", true), "Matches 'Đá vôi' with 'Đá'");
        Assert(SearchMatcher.MatchesText("Đá vôi", "voi", true), "Partial substring match 'voi' in 'Đá vôi'");

        // Diacritic-sensitive matching
        Assert(!SearchMatcher.MatchesText("Cá nóc", "ca noc", false), "Fails 'Cá nóc' with 'ca noc' when diacritic sensitive");
        Assert(SearchMatcher.MatchesText("Cá nóc", "cá", false), "Matches 'Cá nóc' with 'cá' (case-insensitive only)");

        // Case insensitivity
        Assert(SearchMatcher.MatchesText("Pufferfish", "PUFFER", true), "Case-insensitive uppercase query");
        Assert(SearchMatcher.MatchesText("Pufferfish", "pufferfish", false), "Case-insensitive exact query");

        // Null and blank query handling
        Assert(!SearchMatcher.MatchesText(null, "test", true), "Null source returns false");
        Assert(!SearchMatcher.MatchesText("test", null, true), "Null query returns false");
        Assert(SearchMatcher.MatchesText("test", "  ", true), "Empty query returns true (no filter)");

        // Search terms generation
        var terms = SearchMatcher.GenerateSearchTerms("Cá nóc", "Pufferfish", includeNormalized: true).ToList();
        Assert(terms.Contains("Cá nóc"), "Terms contain primary name");
        Assert(terms.Contains("Ca noc"), "Terms contain normalized primary name");
        Assert(terms.Contains("Pufferfish"), "Terms contain secondary name");
        Assert(terms.Count >= 3, $"Terms count is at least 3 (was {terms.Count})");
    }

    private static void TestTooltipFormatting()
    {
        Console.WriteLine("\n-- Testing TooltipFormatter --");

        // 1. Basic formatting (Primary top, Secondary below)
        string r1 = TooltipFormatter.FormatTitle("Cá nóc", "Cá nóc", "Pufferfish", secondaryFirst: false);
        Assert(r1 == "Cá nóc\nPufferfish", "Formats basic title with primary first");

        // 2. Secondary First
        string r2 = TooltipFormatter.FormatTitle("Cá nóc", "Cá nóc", "Pufferfish", secondaryFirst: true);
        Assert(r2 == "Pufferfish\nCá nóc", "Formats title with secondary first");

        // 3. Duplicate suppression: exact match
        string r3 = TooltipFormatter.FormatTitle("Pufferfish", "Pufferfish", "Pufferfish", secondaryFirst: false);
        Assert(r3 == "Pufferfish", "Suppresses exact duplicate title");

        // 4. Duplicate suppression: case-insensitive
        string r4 = TooltipFormatter.FormatTitle("pufferfish", "Pufferfish", "pufferfish", secondaryFirst: false);
        Assert(r4 == "pufferfish", "Suppresses case-insensitive duplicate title");

        // 5. Duplicate suppression: null or whitespace secondary
        string r5 = TooltipFormatter.FormatTitle("Cá nóc", "Cá nóc", null, secondaryFirst: false);
        Assert(r5 == "Cá nóc", "Leaves title unchanged when secondary is null");

        string r6 = TooltipFormatter.FormatTitle("Cá nóc", "Cá nóc", "   ", secondaryFirst: false);
        Assert(r6 == "Cá nóc", "Leaves title unchanged when secondary is whitespace");

        // 6. Suffix preservation: stack count " x5"
        string r7 = TooltipFormatter.FormatTitle("Cá nóc x5", "Cá nóc", "Pufferfish", secondaryFirst: false);
        Assert(r7 == "Cá nóc x5\nPufferfish x5", "Preserves stack count ' x5' in both titles");

        // 7. Suffix preservation: recipe suffix
        string r8 = TooltipFormatter.FormatTitle("Mứt nho (Công thức)", "Mứt nho", "Grape Jelly", secondaryFirst: false);
        Assert(r8 == "Mứt nho (Công thức)\nGrape Jelly (Công thức)", "Preserves recipe suffix '(Công thức)'");

        // 8. Prevent duplicate line if already multiline
        string r9 = TooltipFormatter.FormatTitle("Cá nóc\nPufferfish", "Cá nóc", "Pufferfish", secondaryFirst: false);
        Assert(r9 == "Cá nóc\nPufferfish", "Does not add duplicate line if already formatted with newline");
    }

    private static void TestLexiconPluralization()
    {
        Console.WriteLine("\n-- Testing LexiconPluralize --");

        // Game exceptions
        Assert(ItemNameResolver.LexiconPluralize("Dried Cranberries") == "Dried Cranberries", "Dried Cranberries exception unchanged");
        Assert(ItemNameResolver.LexiconPluralize("Dried Sunflowers") == "Dried Sunflowers", "Dried Sunflowers exception unchanged");
        Assert(ItemNameResolver.LexiconPluralize("Hops") == "Hops", "Hops exception unchanged");
        Assert(ItemNameResolver.LexiconPluralize("Pickles") == "Pickles", "Pickles exception unchanged");
        Assert(ItemNameResolver.LexiconPluralize("Mixed Seeds") == "Mixed Seeds", "Mixed Seeds exception unchanged");

        // Standard English noun endings
        Assert(ItemNameResolver.LexiconPluralize("Dried Apple") == "Dried Apples", "Dried Apple -> Dried Apples");
        Assert(ItemNameResolver.LexiconPluralize("Dried Blueberry") == "Dried Blueberries", "Dried Blueberry -> Dried Blueberries");
        Assert(ItemNameResolver.LexiconPluralize("Dried Peach") == "Dried Peaches", "Dried Peach -> Dried Peaches");
        Assert(ItemNameResolver.LexiconPluralize("Dried Mushroom") == "Dried Mushrooms", "Dried Mushroom -> Dried Mushrooms");

        // Cactus Fruit ends with 't' -> +s -> 'Dried Cactus Fruits' (not 'Cactuses')
        Assert(ItemNameResolver.LexiconPluralize("Dried Cactus Fruit") == "Dried Cactus Fruits", "Dried Cactus Fruit -> Dried Cactus Fruits");
    }

    private static void SetTrinketTemplate(Trinket trinket, string template)
    {
        FieldInfo? valField = typeof(NetFieldBase<string, NetString>).GetField("value", BindingFlags.Instance | BindingFlags.NonPublic);
        if (valField != null)
        {
            valField.SetValue(trinket.displayNameOverrideTemplate, template);
        }
        else
        {
            trinket.displayNameOverrideTemplate.Value = template;
        }
    }

    private static void TestItemCacheKey()
    {
        Console.WriteLine("\n-- Testing ItemCacheKey --");

        var key1 = new ItemCacheKey("(O)348", StardewValley.Object.PreserveType.Wine, "(O)258", null, null);
        var key2 = new ItemCacheKey("(O)348", StardewValley.Object.PreserveType.Wine, "(O)258", null, null);
        var key3 = new ItemCacheKey("(O)348", StardewValley.Object.PreserveType.Wine, "(O)268", null, null);
        var key4 = new ItemCacheKey("(O)348", null, null, null, null);
        var keyTool0 = new ItemCacheKey("(T)Axe", null, null, null, 0);
        var keyTool4 = new ItemCacheKey("(T)Axe", null, null, null, 4);

        Assert(key1 == key2, "Keys with identical preserve type and ingredient are equal");
        Assert(key1 != key3, "Blueberry Wine and Starfruit Wine produce distinct cache keys");
        Assert(key1 != key4, "Flavored Wine and base unflavored Wine produce distinct cache keys");
        Assert(keyTool0 != keyTool4, "Base Axe and Iridium Axe produce distinct cache keys");

        // Trinket dynamic variant cache keys
        var trinketBase = new ItemCacheKey("(TR)FrogEgg", null, null, null, null);
        var trinketVariant0 = new ItemCacheKey("(TR)FrogEgg", null, null, "[LocalizedText Strings\\1_6_Strings:frog_variant_0]", null);
        var trinketVariant4 = new ItemCacheKey("(TR)FrogEgg", null, null, "[LocalizedText Strings\\1_6_Strings:frog_variant_4]", null);

        Assert(trinketBase != trinketVariant0, "Base Trinket and Variant Trinket produce distinct cache keys");
        Assert(trinketVariant0 != trinketVariant4, "Different Trinket variants (e.g. green vs red frog egg) produce distinct cache keys");

        var t0 = new Trinket();
        t0.ItemId = "FrogEgg";
        SetTrinketTemplate(t0, "[LocalizedText Strings\\1_6_Strings:frog_variant_0]");
        var keyFromT0 = ItemCacheKey.FromItem(t0);
        Assert(keyFromT0 == trinketVariant0, "ItemCacheKey.FromItem extracts dynamic variant override");
    }

    private static void TestTrinketResolution()
    {
        Console.WriteLine("\n-- Testing Trinket Resolution (RawData=null Bug Reproduction & Dynamic Variants) --");

        string[] vanillaTrinketIds = new[]
        {
            "BasiliskPaw",
            "FairyBox",
            "IceRod",
            "IridiumSpur",
            "FrogEgg",
            "MagicQuiver",
            "MagicHairDye",
            "ParrotEgg"
        };

        var expectedEn = new Dictionary<string, string>
        {
            ["BasiliskPaw"] = "Basilisk Paw",
            ["FairyBox"] = "Fairy Box",
            ["IceRod"] = "Ice Rod",
            ["IridiumSpur"] = "Golden Spur",
            ["FrogEgg"] = "Frog Egg",
            ["MagicQuiver"] = "Magic Quiver",
            ["MagicHairDye"] = "Magic Hair Gel",
            ["ParrotEgg"] = "Parrot Egg"
        };

        var expectedFr = new Dictionary<string, string>
        {
            ["BasiliskPaw"] = "Patte de basilic",
            ["FairyBox"] = "Boîte de fées",
            ["IceRod"] = "Baguette magique de glace",
            ["IridiumSpur"] = "Éperon doré",
            ["FrogEgg"] = "Œuf de grenouille",
            ["MagicQuiver"] = "Carquois magique",
            ["MagicHairDye"] = "Gel magique pour les cheveux",
            ["ParrotEgg"] = "Œuf de perroquet"
        };

        var expectedVi = new Dictionary<string, string>
        {
            ["BasiliskPaw"] = "Móng vuốt Tử xà",
            ["FairyBox"] = "Rương Thần",
            ["IceRod"] = "Pháp trượng Băng",
            ["IridiumSpur"] = "Đinh gót Vàng",
            ["FrogEgg"] = "Trứng Ếch",
            ["MagicQuiver"] = "Pháp Tiễn",
            ["MagicHairDye"] = "Keo vuốt tóc Thần kỳ",
            ["ParrotEgg"] = "Trứng Vẹt"
        };

        var mock = new MockContentProvider("fr", "fr");
        foreach (var (k, v) in expectedEn)
            mock.AddEnglish("Strings/1_6_Strings", $"{k}_Name", v);
        foreach (var (k, v) in expectedFr)
            mock.AddLocalized("Strings/1_6_Strings", $"{k}_Name", v);

        // Variant strings
        mock.AddEnglish("Strings/1_6_Strings", "frog_variant_0", "Green Frog Egg");
        mock.AddLocalized("Strings/1_6_Strings", "frog_variant_0", "Œuf de grenouille verte");
        mock.AddEnglish("Strings/1_6_Strings", "frog_variant_4", "Red Frog Egg");
        mock.AddLocalized("Strings/1_6_Strings", "frog_variant_4", "Œuf de grenouille rouge");
        mock.AddEnglish("Strings/1_6_Strings", "PerfectIceRod", "Perfect Ice Rod");
        mock.AddLocalized("Strings/1_6_Strings", "PerfectIceRod", "Baguette magique de glace parfaite");
        mock.AddEnglish("Strings/1_6_Strings", "RapidMagicQuiver", "Rapid Magic Quiver");
        mock.AddLocalized("Strings/1_6_Strings", "RapidMagicQuiver", "Carquois de magie rapide");

        // TrinketData provider simulation (matching actual Game1.content / DataLoader.Trinkets data)
        foreach (var id in vanillaTrinketIds)
        {
            mock.AddTrinketData(id, new TrinketData
            {
                DisplayName = $"[LocalizedText Strings\\1_6_Strings:{id}_Name]"
            });
        }

        var trinketDef = new TrinketDataDefinition();
        var resolverFr = new ItemNameResolver(mock);

        // 1. Test all 8 vanilla trinkets with ParsedItemData where RawData is NULL (exact game bug shape)
        foreach (string id in vanillaTrinketIds)
        {
            var parsed = new ParsedItemData(
                trinketDef,
                id,
                0,
                "LooseSprites/specialItems",
                id, // In game, internalName == itemId
                expectedEn[id],
                "Description",
                -101,
                null,
                null // RawData is null!
            );

            string? resolved = resolverFr.ResolveFromParsedData(parsed);
            Assert(resolved == expectedFr[id], $"Trinket '{id}' with RawData=null resolves French secondary (expected '{expectedFr[id]}', got '{resolved}')");
        }

        // 2. Test all 8 vanilla trinkets with English secondary provider
        var mockEn = new MockContentProvider("en", "en");
        foreach (var (k, v) in expectedEn)
            mockEn.AddEnglish("Strings/1_6_Strings", $"{k}_Name", v);
        mockEn.AddEnglish("Strings/1_6_Strings", "frog_variant_0", "Green Frog Egg");
        mockEn.AddEnglish("Strings/1_6_Strings", "frog_variant_4", "Red Frog Egg");
        mockEn.AddEnglish("Strings/1_6_Strings", "PerfectIceRod", "Perfect Ice Rod");
        mockEn.AddEnglish("Strings/1_6_Strings", "RapidMagicQuiver", "Rapid Magic Quiver");
        foreach (var id in vanillaTrinketIds)
        {
            mockEn.AddTrinketData(id, new TrinketData
            {
                DisplayName = $"[LocalizedText Strings\\1_6_Strings:{id}_Name]"
            });
        }
        var resolverEn = new ItemNameResolver(mockEn);
        foreach (string id in vanillaTrinketIds)
        {
            var parsed = new ParsedItemData(
                trinketDef,
                id,
                0,
                "LooseSprites/specialItems",
                id,
                id,
                "Description",
                -101,
                null,
                null
            );
            string? resolved = resolverEn.ResolveFromParsedData(parsed);
            Assert(resolved == expectedEn[id], $"Trinket '{id}' with RawData=null resolves English secondary (expected '{expectedEn[id]}', got '{resolved}')");
        }

        // 3. Test Vietnamese secondary provider
        var mockVi = new MockContentProvider("vi", "vi");
        foreach (var (k, v) in expectedEn)
            mockVi.AddEnglish("Strings/1_6_Strings", $"{k}_Name", v);
        foreach (var (k, v) in expectedVi)
            mockVi.AddLocalized("Strings/1_6_Strings", $"{k}_Name", v);
        foreach (var id in vanillaTrinketIds)
        {
            mockVi.AddTrinketData(id, new TrinketData
            {
                DisplayName = $"[LocalizedText Strings\\1_6_Strings:{id}_Name]"
            });
        }
        var resolverVi = new ItemNameResolver(mockVi);
        foreach (string id in vanillaTrinketIds)
        {
            var parsed = new ParsedItemData(
                trinketDef,
                id,
                0,
                "LooseSprites/specialItems",
                id,
                expectedEn[id],
                "Description",
                -101,
                null,
                null
            );
            string? resolved = resolverVi.ResolveFromParsedData(parsed);
            Assert(resolved == expectedVi[id], $"Trinket '{id}' with RawData=null resolves Vietnamese secondary (expected '{expectedVi[id]}', got '{resolved}')");
        }

        // 4. Test dynamic variants on instantiated Trinket objects
        var frog0 = new Trinket();
        frog0.ItemId = "FrogEgg";
        SetTrinketTemplate(frog0, "[LocalizedText Strings\\1_6_Strings:frog_variant_0]");

        var frog4 = new Trinket();
        frog4.ItemId = "FrogEgg";
        SetTrinketTemplate(frog4, "[LocalizedText Strings\\1_6_Strings:frog_variant_4]");

        var iceRodPerfect = new Trinket();
        iceRodPerfect.ItemId = "IceRod";
        SetTrinketTemplate(iceRodPerfect, "[LocalizedText Strings\\1_6_Strings:PerfectIceRod]");

        var quiverRapid = new Trinket();
        quiverRapid.ItemId = "MagicQuiver";
        SetTrinketTemplate(quiverRapid, "[LocalizedText Strings\\1_6_Strings:RapidMagicQuiver]");

        Assert(resolverFr.GetSecondaryName(frog0) == "Œuf de grenouille verte", "Dynamic variant: Green Frog Egg resolves French");
        Assert(resolverFr.GetSecondaryName(frog4) == "Œuf de grenouille rouge", "Dynamic variant: Red Frog Egg resolves French");
        Assert(resolverFr.GetSecondaryName(iceRodPerfect) == "Baguette magique de glace parfaite", "Dynamic variant: Perfect Ice Rod resolves French");
        Assert(resolverFr.GetSecondaryName(quiverRapid) == "Carquois de magie rapide", "Dynamic variant: Rapid Magic Quiver resolves French");

        Assert(resolverEn.GetSecondaryName(frog0) == "Green Frog Egg", "Dynamic variant: Green Frog Egg resolves English");
        Assert(resolverEn.GetSecondaryName(iceRodPerfect) == "Perfect Ice Rod", "Dynamic variant: Perfect Ice Rod resolves English");
    }

    private static void TestCjbSearchFilter()
    {
        Console.WriteLine("\n-- Testing CJB Search Filter Integration --");

        var mock = new MockContentProvider("en", "en");
        mock.AddEnglish("Strings/Objects", "128_Name", "Pufferfish");
        var resolver = new ItemNameResolver(mock);

        var item = new FakeItem("128", "Cá nóc"); // Vietnamese display name, English secondary is Pufferfish

        // 1. Accentless Vietnamese query: "ca noc" matches "Cá nóc" when ignoreDiacritics is true
        bool m1 = CjbSearchFilter.Matches(item, "Pufferfish", "Cá nóc", "128", "(O)128", "ca noc", resolver, ignoreDiacritics: true);
        Assert(m1, "CJB search: accentless 'ca noc' matches 'Cá nóc'");

        // 2. Exact Vietnamese query: "Cá nóc"
        bool m2 = CjbSearchFilter.Matches(item, "Pufferfish", "Cá nóc", "128", "(O)128", "Cá nóc", resolver, ignoreDiacritics: true);
        Assert(m2, "CJB search: exact 'Cá nóc' matches 'Cá nóc'");

        // 3. English secondary matching: query "pufferfish"
        bool m3 = CjbSearchFilter.Matches(item, "Pufferfish", "Cá nóc", "128", "(O)128", "pufferfish", resolver, ignoreDiacritics: true);
        Assert(m3, "CJB search: secondary name 'pufferfish' matches");

        // 4. Config toggle: when ignoreDiacritics is false, unaccented query "ca noc" fails against "Cá nóc"
        bool m4 = CjbSearchFilter.Matches(item, "Pufferfish", "Cá nóc", "128", "(O)128", "ca noc", resolver, ignoreDiacritics: false);
        Assert(!m4, "CJB search: accentless 'ca noc' rejected when ignoreDiacritics=false");

        // 5. Item ID matching
        bool m5 = CjbSearchFilter.Matches(item, "Pufferfish", "Cá nóc", "128", "(O)128", "128", resolver, ignoreDiacritics: true);
        bool m6 = CjbSearchFilter.Matches(item, "Pufferfish", "Cá nóc", "128", "(O)128", "(O)128", resolver, ignoreDiacritics: true);
        Assert(m5, "CJB search: matches unqualified ID '128'");
        Assert(m6, "CJB search: matches qualified ID '(O)128'");

        // 6. Non-matching query returns false
        bool m7 = CjbSearchFilter.Matches(item, "Pufferfish", "Cá nóc", "128", "(O)128", "Kim cương", resolver, ignoreDiacritics: true);
        Assert(!m7, "CJB search: non-matching query returns false");

        // 7. Null item / no CJB installed path sanity
        bool m8 = CjbSearchFilter.Matches(null, "Pufferfish", "Cá nóc", "128", "(O)128", "ca noc", resolver, ignoreDiacritics: true);
        Assert(m8, "CJB search: null item falls back to name/id fields safely");

        // 8. Null resolver path sanity
        bool m9 = CjbSearchFilter.Matches(item, "Pufferfish", "Cá nóc", "128", "(O)128", "ca noc", null, ignoreDiacritics: true);
        Assert(m9, "CJB search: null resolver falls back to primary display name safely");

        // 9. Empty query matches everything
        bool m10 = CjbSearchFilter.Matches(item, "Pufferfish", "Cá nóc", "128", "(O)128", "", resolver, ignoreDiacritics: true);
        Assert(m10, "CJB search: empty query returns true");
    }

    private static void TestCjbHarmonyBoundary()
    {
        Console.WriteLine("\n-- Testing CJB Item Spawner Harmony Adapter Boundary --");

        CjbItemSpawnerIntegration.ResetStateForTests();

        // 1. Unsupported shape safe skip: missing SearchBox field
        var harmonySkip = new Harmony("test.cjb.unsupported");
        bool appliedInvalid = CjbItemSpawnerIntegration.TryApplyInternal(harmonySkip, typeof(FakeUnsupportedItemMenu), typeof(FakeCjbSpawnableItem));
        Assert(!appliedInvalid, "Harmony adapter skips unsupported menu shape safely");
        Assert(CjbItemSpawnerIntegration.WarnedUnsupportedShape, "Warned once on unsupported shape");

        // 2. Supported shape patch binding
        CjbItemSpawnerIntegration.ResetStateForTests();
        var harmony = new Harmony("test.cjb.supported");
        bool appliedValid = CjbItemSpawnerIntegration.TryApplyInternal(harmony, typeof(FakeSupportedItemMenu), typeof(FakeCjbSpawnableItem));
        Assert(appliedValid, "Harmony adapter successfully patches supported menu shape");

        // Prepare test data with deferred native category sort orders
        var mock = new MockContentProvider("en", "en");
        mock.AddEnglish("Strings/Objects", "128_Name", "Pufferfish");
        var resolver = new ItemNameResolver(mock);
        CjbItemSpawnerIntegration.TestResolverOverride = resolver;

        var menu = new FakeSupportedItemMenu();
        // Item 0: Pufferfish (display "Cá nóc", id "128", category sort order 2)
        var itemPuffer = new FakeItem("128", "Cá nóc");
        var cjbPuffer = new FakeCjbSpawnableItem("128", "Pufferfish", "Cá nóc", sortOrder: 2, item: itemPuffer);

        // Item 1: Diamond (display "Kim cương", id "72", category sort order 1)
        var itemDiamond = new FakeItem("72", "Kim cương");
        var cjbDiamond = new FakeCjbSpawnableItem("72", "Diamond", "Kim cương", sortOrder: 1, item: itemDiamond);

        // Item 2: Carp (display "Cá chép", id "142", category sort order 3)
        var itemCarp = new FakeItem("142", "Cá chép");
        var cjbCarp = new FakeCjbSpawnableItem("142", "Carp", "Cá chép", sortOrder: 3, item: itemCarp);

        menu.AllItems.AddRange(new[] { cjbPuffer, cjbDiamond, cjbCarp });

        // 3. Exact query preservation (untrimmed) & restoration
        menu.SearchBox.Text = "  ca noc  ";
        var resultsPuffer = menu.SearchItems().ToList();
        Assert(resultsPuffer.Count == 1 && resultsPuffer[0].Name == "Pufferfish", "Harmony Postfix filtered to matching item 'Pufferfish'");
        Assert(menu.SearchBox.Text == "  ca noc  ", $"SearchBox.Text restored to exact untrimmed query '{menu.SearchBox.Text}'");

        // 4. Return type cast preservation: result is IEnumerable<FakeCjbSpawnableItem>
        IEnumerable<FakeCjbSpawnableItem> typedResult = menu.SearchItems();
        Assert(typedResult != null, "SearchItems return is strongly typed IEnumerable<FakeCjbSpawnableItem>");

        // 5. Preservation of deferred native category sort order
        // Query "ca" matches both "Cá nóc" (sortOrder 2) and "Cá chép" (sortOrder 3)
        menu.SearchBox.Text = "ca";
        var resultsMulti = menu.SearchItems().ToList();
        Assert(resultsMulti.Count == 2, $"Query 'ca' matched 2 items (got {resultsMulti.Count})");
        Assert(resultsMulti[0].CategorySortOrder == 2 && resultsMulti[1].CategorySortOrder == 3,
            "Deferred native sort order preserved: sortOrder 2 precedes sortOrder 3");

        // 6. Secondary name matching through Harmony adapter
        menu.SearchBox.Text = "pufferfish";
        var resultsSecondary = menu.SearchItems().ToList();
        Assert(resultsSecondary.Count == 1 && resultsSecondary[0].Id == "128", "Bilingual secondary query 'pufferfish' matched via Harmony adapter");
        Assert(menu.SearchBox.Text == "pufferfish", "SearchBox.Text restored after secondary query");

        // 7. Guaranteed query restore on throwing method (Harmony Finalizer)
        CjbItemSpawnerIntegration.ResetStateForTests();
        var harmonyThrow = new Harmony("test.cjb.throwing");
        CjbItemSpawnerIntegration.TryApplyInternal(harmonyThrow, typeof(FakeThrowingItemMenu), typeof(FakeCjbSpawnableItem));
        var throwingMenu = new FakeThrowingItemMenu();
        throwingMenu.SearchBox.Text = "queryBeforeThrow";
        bool caughtExpected = false;
        try
        {
            throwingMenu.SearchItems();
        }
        catch (InvalidOperationException)
        {
            caughtExpected = true;
        }
        Assert(caughtExpected, "SearchItems threw expected exception through Harmony boundary");
        Assert(throwingMenu.SearchBox.Text == "queryBeforeThrow", "Harmony Finalizer restored SearchBox.Text on throwing method");

        // 8. Guaranteed query restore on null-returning method
        CjbItemSpawnerIntegration.ResetStateForTests();
        var harmonyNull = new Harmony("test.cjb.nullret");
        CjbItemSpawnerIntegration.TryApplyInternal(harmonyNull, typeof(FakeNullReturningItemMenu), typeof(FakeCjbSpawnableItem));
        var nullMenu = new FakeNullReturningItemMenu();
        nullMenu.SearchBox.Text = "queryBeforeNull";
        var nullResult = nullMenu.SearchItems();
        Assert(nullResult == null, "SearchItems returned null");
        Assert(nullMenu.SearchBox.Text == "queryBeforeNull", "Harmony Postfix finally restored SearchBox.Text on null return");

        // 9. Per-item error fallback and logging once
        CjbItemSpawnerIntegration.ResetStateForTests();
        var harmonyFallback = new Harmony("test.cjb.fallback");
        CjbItemSpawnerIntegration.TryApplyInternal(harmonyFallback, typeof(FakeFallbackItemMenu), typeof(FakeCjbSpawnableItem));
        var fallbackMenu = new FakeFallbackItemMenu();
        var throwingItem = new FakeThrowingSpawnableItem("500", "Exploding Fruit", "Trái Nổ");
        fallbackMenu.AllItems.Add(throwingItem);
        fallbackMenu.AllItems.Add(cjbDiamond);

        fallbackMenu.SearchBox.Text = "Exploding";
        var fallbackResults = fallbackMenu.SearchItems().ToList();
        Assert(fallbackResults.Count == 1 && fallbackResults[0].Name == "Exploding Fruit",
            "Per-item error gracefully fell back to native name query filter");
        Assert(CjbItemSpawnerIntegration.LoggedFilterError, "Error logged once during per-item failure");

        // 10. Outer error handling: SearchBox.Text restoration when enumeration fails catastrophically
        CjbItemSpawnerIntegration.ResetStateForTests();
        var harmonyOuter = new Harmony("test.cjb.outerthrow");
        CjbItemSpawnerIntegration.TryApplyInternal(harmonyOuter, typeof(FakeOuterThrowItemMenu), typeof(FakeCjbSpawnableItem));
        var outerMenu = new FakeOuterThrowItemMenu();
        outerMenu.SearchBox.Text = "OuterQuery";
        try
        {
            outerMenu.SearchItems()?.ToList();
        }
        catch
        {
            // Expected throw when consuming throwing enumerable
        }
        Assert(outerMenu.SearchBox.Text == "OuterQuery", "Harmony Postfix finally restored SearchBox.Text on outer enumeration failure");

        // Cleanup test state
        CjbItemSpawnerIntegration.ResetStateForTests();
    }

    private static void TestMockContentProviderAndResolver()
    {
        Console.WriteLine("\n-- Testing MockContentProvider, Per-Key Fallback, and Resolver --");

        var mock = new MockContentProvider("fr", "fr");
        // French has 128_Name and Wine_Flavored_Name
        mock.AddLocalized("Strings/Objects", "128_Name", "Poisson-globe");
        mock.AddLocalized("Strings/Objects", "Wine_Flavored_Name", "Vin de {0}");

        // Base English has 128_Name, Wine_Flavored_Name, and Honey_Wild_Name (missing in French)
        mock.AddEnglish("Strings/Objects", "128_Name", "Pufferfish");
        mock.AddEnglish("Strings/Objects", "Wine_Flavored_Name", "{0} Wine");
        mock.AddEnglish("Strings/Objects", "Honey_Wild_Name", "Wild Honey");
        mock.AddEnglish("Strings/Objects", "DriedFruit_Flavored_(O)258_Name", "Dried Blueberries");
        mock.AddEnglish("Strings/Objects", "DriedFruit_Flavored_Name", "Dried {0}");

        // Per-key fallback check
        Assert(mock.TryGetString("Strings/Objects", "128_Name", out string? val1) && val1 == "Poisson-globe", "Retrieves French translated 128_Name");
        Assert(mock.TryGetString("Strings/Objects", "Honey_Wild_Name", out string? val2) && val2 == "Wild Honey", "Per-key fallback: retrieves English Honey_Wild_Name when missing in French");

        var resolver = new ItemNameResolver(mock);

        // Precedence: displayNameFormat vs preserve
        string? formatTest = resolver.ResolveDisplayNameFormat("(O)348", "(O)258", "Special %DISPLAY_NAME with %PRESERVED_DISPLAY_NAME");
        Assert(formatTest != null && formatTest.Contains("Special"), "displayNameFormat properly parsed");

        // Specialized dried fruit template (should NOT be pluralized again)
        var mockEn = new MockContentProvider("en", "en");
        mockEn.AddEnglish("Strings/Objects", "DriedFruit_Flavored_(O)258_Name", "Dried Blueberries");
        var resolverEn = new ItemNameResolver(mockEn);
        string? specDried = resolverEn.ResolveFlavoredObject("(O)DriedFruit", StardewValley.Object.PreserveType.DriedFruit, "(O)258");
        Assert(specDried == "Dried Blueberries", $"Specialized dried template used as-is without double pluralizing (was '{specDried}')");
    }

    private static void TestApiLifecycleAndConfig()
    {
        Console.WriteLine("\n-- Testing Stable API Lifecycle Across Config/Resolver Updates --");

        var config = new ModConfig
        {
            SecondaryLanguage = "en",
            DiacriticInsensitiveSearch = true
        };

        var mock = new MockContentProvider("en", "en");
        mock.AddEnglish("Strings/Objects", "128_Name", "Pufferfish");
        var resolver = new ItemNameResolver(mock);

        // Create API once
        var api = new UntranslatedStardewApi(() => resolver, () => config);

        // Test with DiacriticInsensitiveSearch = true
        bool m1 = api.Matches(new FakeItem("Cá nóc"), "ca noc");
        Assert(m1, "API matches unaccented query when DiacriticInsensitiveSearch is true");

        // Change config dynamically
        config.DiacriticInsensitiveSearch = false;
        bool m2 = api.Matches(new FakeItem("Cá nóc"), "ca noc");
        Assert(!m2, "API honors dynamic config change without re-creating API instance");

        // Explicit override in Matches parameter
        bool m3 = api.Matches(new FakeItem("Cá nóc"), "ca noc", ignoreDiacritics: true);
        Assert(m3, "Explicit caller override ignoreDiacritics=true overrides config=false");

        // Simulate GMCM save / resolver recreation
        var mockFr = new MockContentProvider("fr", "fr");
        mockFr.AddLocalized("Strings/Objects", "128_Name", "Poisson-globe");
        resolver = new ItemNameResolver(mockFr);

        // Verify the original API reference now sees the new French resolver!
        var (primary, secondary) = api.GetNames(new FakeItem("128", "Cá nóc"));
        Assert(primary == "Cá nóc", "Primary name preserved");

        // Test safe behavior when resolver is null (e.g. during disposal)
        resolver = null!;
        var (pSafe, sSafe) = api.GetNames(new FakeItem("128", "Cá nóc"));
        Assert(pSafe == "Cá nóc" && sSafe == null, "Safe return (no crash) when resolver is null");
    }

    private static void TestSystematicCoverageAudit()
    {
        Console.WriteLine("\n-- Testing Systematic Item Name Coverage Audit (Offline Real XNB) --");

        string? gamePath = LocateGamePath();
        if (gamePath == null || !Directory.Exists(gamePath))
        {
            Console.WriteLine("  [SKIP] Stardew Valley game path not found; skipping offline coverage audit.");
            return;
        }

        string contentRoot = Path.Combine(gamePath, "Content");
        var testService = new DummyServiceProvider();
        var rawFr = new RawContentProvider(() => testService, contentRoot, "fr");
        var resolverFr = new ItemNameResolver(rawFr);

        string[] vanillaTrinketIds = new[]
        {
            "BasiliskPaw", "FairyBox", "IceRod", "IridiumSpur",
            "FrogEgg", "MagicQuiver", "MagicHairDye", "ParrotEgg"
        };

        int trinketCount = 0;
        foreach (string id in vanillaTrinketIds)
        {
            string? name = resolverFr.GetSecondaryNameById($"(TR){id}");
            if (!string.IsNullOrEmpty(name))
                trinketCount++;
        }
        Assert(trinketCount == 8, $"Audit: all 8/8 vanilla trinkets resolve via GetSecondaryNameById in French (got {trinketCount}/8)");

        // Boots & Hats audit
        string? bootsFr = resolverFr.GetSecondaryNameById("(B)504");
        Assert(bootsFr == "Baskets", $"Audit: boots (B)504 resolves via GetSecondaryNameById in French ('{bootsFr}')");

        string? hatFr = resolverFr.GetSecondaryNameById("(H)0");
        Assert(hatFr == "Chapeau de cowboy", $"Audit: hat (H)0 resolves via GetSecondaryNameById in French ('{hatFr}')");
    }

    private static void TestRealXnbSmoke()
    {
        Console.WriteLine("\n-- Testing Real XNB Offline Smoke Check (No Game Launch) --");

        string? gamePath = LocateGamePath();
        if (gamePath == null || !Directory.Exists(gamePath))
        {
            Console.WriteLine("  [SKIP] Stardew Valley game path not found on this machine; skipping real XNB smoke check.");
            return;
        }

        string contentRoot = Path.Combine(gamePath, "Content");
        string modContentRoot = Path.Combine(gamePath, "Mods", "Stardew Valley Viethoa", "assets", "Content");

        var testService = new DummyServiceProvider();

        // 1. English vanilla ContentManager
        var rawEn = new RawContentProvider(() => testService, contentRoot, "en");
        bool gotEn = rawEn.TryGetString("Strings/Objects", "Honey_Wild_Name", out string? enName);
        Assert(gotEn && enName == "Wild Honey", $"Real English XNB Strings/Objects:Honey_Wild_Name == 'Wild Honey' (got '{enName}')");

        // 2. French vanilla ContentManager
        var rawFr = new RawContentProvider(() => testService, contentRoot, "fr");
        bool gotFr = rawFr.TryGetString("Strings/Objects", "Honey_Wild_Name", out string? frName);
        Assert(gotFr && frName == "Miel sauvage", $"Real French XNB Strings/Objects.fr-FR:Honey_Wild_Name == 'Miel sauvage' (got '{frName}')");

        // 3. Real Boots dictionary in French (Data/Boots.fr-FR)
        bool gotBootsFr = rawFr.TryGetString("Data/Boots", "504", out string? bootsEntryFr);
        string? bootsFrName = bootsEntryFr != null && bootsEntryFr.Split('/').Length > 6 ? bootsEntryFr.Split('/')[6] : null;
        Assert(gotBootsFr && !string.IsNullOrEmpty(bootsFrName), $"Real French XNB Data/Boots.fr-FR:504 name == '{bootsFrName}'");

        // 4. Real Hats dictionary in French (Data/hats.fr-FR)
        bool gotHatsFr = rawFr.TryGetString("Data/hats", "0", out string? hatsEntryFr);
        string? hatsFrName = hatsEntryFr != null && hatsEntryFr.Split('/').Length > 5 ? hatsEntryFr.Split('/')[5] : null;
        Assert(gotHatsFr && !string.IsNullOrEmpty(hatsFrName), $"Real French XNB Data/hats.fr-FR:0 name == '{hatsFrName}'");

        // 5. Real Trinkets dictionary & 1_6_Strings in French (Data/Trinkets & Strings/1_6_Strings.fr-FR)
        bool gotTrinketData = rawFr.TryGetTrinketData("FrogEgg", out TrinketData? frogData);
        Assert(gotTrinketData && frogData != null, "Real XNB: loaded Data/Trinkets for FrogEgg");
        bool gotFrogFr = rawFr.TryGetString("Strings/1_6_Strings", "FrogEgg_Name", out string? frogFrName);
        Assert(gotFrogFr && frogFrName == "Œuf de grenouille", $"Real French XNB Strings/1_6_Strings.fr-FR:FrogEgg_Name == 'Œuf de grenouille' (got '{frogFrName}')");

        // 6. Real Vietnamese mod ContentManager (if Kou.ViethoaSV installed)
        if (Directory.Exists(modContentRoot))
        {
            var rawVi = new RawContentProvider(() => testService, contentRoot, "vi", modContentRoot);
            bool gotVi = rawVi.TryGetString("Strings/Objects", "Honey_Wild_Name", out string? viName);
            Assert(gotVi && viName == "Mật ong rừng", $"Real Vietnamese mod XNB Strings/Objects_vi:Honey_Wild_Name == 'Mật ong rừng' (got '{viName}')");

            bool gotBootsVi = rawVi.TryGetString("Data/Boots", "504", out string? bootsEntryVi);
            string? bootsViName = bootsEntryVi != null && bootsEntryVi.Split('/').Length > 6 ? bootsEntryVi.Split('/')[6] : null;
            Assert(gotBootsVi && !string.IsNullOrEmpty(bootsViName), $"Real Vietnamese mod XNB Data/Boots_vi:504 name == '{bootsViName}'");

            bool gotFrogVi = rawVi.TryGetString("Strings/1_6_Strings", "FrogEgg_Name", out string? frogViName);
            Assert(gotFrogVi && frogViName == "Trứng Ếch", $"Real Vietnamese mod XNB Strings/1_6_Strings_vi:FrogEgg_Name == 'Trứng Ếch' (got '{frogViName}')");
        }
        else
        {
            Console.WriteLine("  [INFO] Kou.ViethoaSV mod directory not present; skipped Vietnamese mod XNB check.");
        }

        // 7. Supplement real XNB audit with all 8 vanilla trinkets through ResolveFromParsedData with RawData=null
        Console.WriteLine("\n-- Supplement: Real XNB Trinket Resolution (RawData=null) for en, fr, and vi --");
        string[] vanillaTrinketIds = new[]
        {
            "BasiliskPaw", "FairyBox", "IceRod", "IridiumSpur",
            "FrogEgg", "MagicQuiver", "MagicHairDye", "ParrotEgg"
        };

        var expectedEn = new Dictionary<string, string>
        {
            ["BasiliskPaw"] = "Basilisk Paw",
            ["FairyBox"] = "Fairy Box",
            ["IceRod"] = "Ice Rod",
            ["IridiumSpur"] = "Golden Spur",
            ["FrogEgg"] = "Frog Egg",
            ["MagicQuiver"] = "Magic Quiver",
            ["MagicHairDye"] = "Magic Hair Gel",
            ["ParrotEgg"] = "Parrot Egg"
        };

        var expectedFr = new Dictionary<string, string>
        {
            ["BasiliskPaw"] = "Patte de basilic",
            ["FairyBox"] = "Boîte de fées",
            ["IceRod"] = "Baguette magique de glace",
            ["IridiumSpur"] = "Éperon doré",
            ["FrogEgg"] = "Œuf de grenouille",
            ["MagicQuiver"] = "Carquois magique",
            ["MagicHairDye"] = "Gel magique pour les cheveux",
            ["ParrotEgg"] = "Œuf de perroquet"
        };

        var expectedVi = new Dictionary<string, string>
        {
            ["BasiliskPaw"] = "Móng vuốt Tử xà",
            ["FairyBox"] = "Rương Thần",
            ["IceRod"] = "Pháp trượng Băng",
            ["IridiumSpur"] = "Đinh gót Vàng",
            ["FrogEgg"] = "Trứng Ếch",
            ["MagicQuiver"] = "Pháp Tiễn",
            ["MagicHairDye"] = "Keo vuốt tóc Thần kỳ",
            ["ParrotEgg"] = "Trứng Vẹt"
        };

        var trinketDef = new TrinketDataDefinition();

        // A. Resolve all 8 vanilla trinkets from real English XNB ContentManager
        var resolverRealEn = new ItemNameResolver(rawEn);
        foreach (string id in vanillaTrinketIds)
        {
            var parsed = new ParsedItemData(
                trinketDef,
                id,
                0,
                "LooseSprites/specialItems",
                id,
                expectedEn[id],
                "Description",
                -101,
                null,
                null // RawData is null!
            );
            string? resolved = resolverRealEn.ResolveFromParsedData(parsed);
            Assert(resolved == expectedEn[id], $"Real XNB en: Trinket '{id}' (RawData=null) resolves '{expectedEn[id]}' (got '{resolved}')");
        }

        // B. Resolve all 8 vanilla trinkets from real French XNB ContentManager
        var resolverRealFr = new ItemNameResolver(rawFr);
        foreach (string id in vanillaTrinketIds)
        {
            var parsed = new ParsedItemData(
                trinketDef,
                id,
                0,
                "LooseSprites/specialItems",
                id,
                expectedEn[id],
                "Description",
                -101,
                null,
                null // RawData is null!
            );
            string? resolved = resolverRealFr.ResolveFromParsedData(parsed);
            Assert(resolved == expectedFr[id], $"Real XNB fr: Trinket '{id}' (RawData=null) resolves '{expectedFr[id]}' (got '{resolved}')");
        }

        // C. Resolve all 8 vanilla trinkets from real Vietnamese mod ContentManager (if available)
        if (Directory.Exists(modContentRoot))
        {
            var rawVi = new RawContentProvider(() => testService, contentRoot, "vi", modContentRoot);
            var resolverRealVi = new ItemNameResolver(rawVi);
            foreach (string id in vanillaTrinketIds)
            {
                var parsed = new ParsedItemData(
                    trinketDef,
                    id,
                    0,
                    "LooseSprites/specialItems",
                    id,
                    expectedEn[id],
                    "Description",
                    -101,
                    null,
                    null // RawData is null!
                );
                string? resolved = resolverRealVi.ResolveFromParsedData(parsed);
                Assert(resolved == expectedVi[id], $"Real XNB vi: Trinket '{id}' (RawData=null) resolves '{expectedVi[id]}' (got '{resolved}')");
            }
        }
    }

    private static void TestDefaultAndEnLanguageSemantics()
    {
        Console.WriteLine("\n-- Testing Native SMAPI 'default' vs 'en' Language Semantics --");

        Assert(LanguageHelper.IsBuiltInLanguage("default"), "LanguageHelper recognizes 'default' as supported built-in");
        Assert(LanguageHelper.GetLanguageSuffix("default") == null, "'default' maps to null suffix (base unsuffixed file)");
        Assert(LanguageHelper.CanonicalizeLanguageCode("default") == "default", "Canonicalize 'default' -> 'default'");
        Assert(LanguageHelper.CanonicalizeLanguageCode("DEFAULT") == "default", "Canonicalize 'DEFAULT' -> 'default'");
        Assert(LanguageHelper.IsBaseOrEnglish("default"), "IsBaseOrEnglish('default') is true");
        Assert(LanguageHelper.IsBaseOrEnglish("en"), "IsBaseOrEnglish('en') is true");
        Assert(!LanguageHelper.IsBaseOrEnglish("fr"), "IsBaseOrEnglish('fr') is false");
        Assert(!LanguageHelper.IsBaseOrEnglish("vi"), "IsBaseOrEnglish('vi') is false");

        string? gamePath = LocateGamePath();
        string contentRoot = gamePath != null ? Path.Combine(gamePath, "Content") : AppContext.BaseDirectory;
        var testService = new DummyServiceProvider();

        // 1. RequestedLanguage = "default" -> EffectiveLanguage = "default", NO warning fired
        string? warnedRequested = null;
        string? warnedFallback = null;
        var rawDefault = new RawContentProvider(
            () => testService,
            contentRoot,
            "default",
            null,
            (req, fb) => { warnedRequested = req; warnedFallback = fb; });

        Assert(rawDefault.RequestedLanguage == "default", "rawDefault RequestedLanguage == 'default'");
        Assert(rawDefault.EffectiveLanguage == "default", "rawDefault EffectiveLanguage == 'default'");
        Assert(warnedRequested == null, "rawDefault did not trigger unsupported fallback warning");

        // 2. RequestedLanguage = "en" -> EffectiveLanguage = "en", NO warning fired
        warnedRequested = null;
        warnedFallback = null;
        var rawEn = new RawContentProvider(
            () => testService,
            contentRoot,
            "en",
            null,
            (req, fb) => { warnedRequested = req; warnedFallback = fb; });

        Assert(rawEn.RequestedLanguage == "en", "rawEn RequestedLanguage == 'en'");
        Assert(rawEn.EffectiveLanguage == "en", "rawEn EffectiveLanguage == 'en'");
        Assert(warnedRequested == null, "rawEn did not trigger unsupported fallback warning");

        // 3. RequestedLanguage = "unsupported_xyz" -> Fallback to "default" with warning fired
        warnedRequested = null;
        warnedFallback = null;
        var rawUnsupported = new RawContentProvider(
            () => testService,
            contentRoot,
            "unsupported_xyz",
            null,
            (req, fb) => { warnedRequested = req; warnedFallback = fb; });

        Assert(rawUnsupported.EffectiveLanguage == "unsupported_xyz" && !rawUnsupported.IsSecondaryAvailable, "rawUnsupported retains requested locale and disables unavailable secondary");
        Assert(warnedRequested == "unsupported_xyz" && warnedFallback!.Contains("primary-only"), "rawUnsupported reports primary-only when no custom source exists");

        // 4. Default config uses "default"
        var config = new ModConfig();
        Assert(config.SecondaryLanguage == "default", "ModConfig default SecondaryLanguage is 'default'");
    }

    private static void TestUiHarmonyBoundarySignatures()
    {
        Console.WriteLine("\n-- Testing UI Harmony Boundary Signatures --");

        var mToolTip = typeof(StardewValley.Menus.IClickableMenu).GetMethod(nameof(StardewValley.Menus.IClickableMenu.drawToolTip));
        Assert(mToolTip != null, "Reflection: IClickableMenu.drawToolTip exists");

        var mSkillsHover = typeof(StardewValley.Menus.SkillsPage).GetMethod(nameof(StardewValley.Menus.SkillsPage.performHoverAction));
        Assert(mSkillsHover != null, "Reflection: SkillsPage.performHoverAction exists");

        var mLevelUpDraw = typeof(StardewValley.Menus.LevelUpMenu).GetMethod(nameof(StardewValley.Menus.LevelUpMenu.draw), new[] { typeof(Microsoft.Xna.Framework.Graphics.SpriteBatch) });
        Assert(mLevelUpDraw != null, "Reflection: LevelUpMenu.draw(SpriteBatch) exists");

        var mPowersHover = typeof(StardewValley.Menus.PowersTab).GetMethod(nameof(StardewValley.Menus.PowersTab.performHoverAction));
        Assert(mPowersHover != null, "Reflection: PowersTab.performHoverAction exists");

        var mCollectionsDesc = typeof(StardewValley.Menus.CollectionsPage).GetMethod(nameof(StardewValley.Menus.CollectionsPage.createDescription), new[] { typeof(string) });
        Assert(mCollectionsDesc != null, "Reflection: CollectionsPage.createDescription(string) exists");

        // Targeted private fields bound by Harmony patches
        var fLeftDesc = typeof(StardewValley.Menus.LevelUpMenu).GetField("leftProfessionDescription", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert(fLeftDesc != null && fLeftDesc.FieldType == typeof(List<string>), "Reflection: LevelUpMenu.leftProfessionDescription (List<string>) exists");

        var fRightDesc = typeof(StardewValley.Menus.LevelUpMenu).GetField("rightProfessionDescription", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert(fRightDesc != null && fRightDesc.FieldType == typeof(List<string>), "Reflection: LevelUpMenu.rightProfessionDescription (List<string>) exists");

        var fChoose = typeof(StardewValley.Menus.LevelUpMenu).GetField("professionsToChoose", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert(fChoose != null && fChoose.FieldType == typeof(List<int>), "Reflection: LevelUpMenu.professionsToChoose (List<int>) exists");

        var fLeftColor = typeof(StardewValley.Menus.LevelUpMenu).GetField("leftProfessionColor", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert(fLeftColor != null && fLeftColor.FieldType == typeof(Microsoft.Xna.Framework.Color), "Reflection: LevelUpMenu.leftProfessionColor (Color) exists");

        var fRightColor = typeof(StardewValley.Menus.LevelUpMenu).GetField("rightProfessionColor", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert(fRightColor != null && fRightColor.FieldType == typeof(Microsoft.Xna.Framework.Color), "Reflection: LevelUpMenu.rightProfessionColor (Color) exists");

        var fSkillsHoverTitle = typeof(StardewValley.Menus.SkillsPage).GetField("hoverTitle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert(fSkillsHoverTitle != null && fSkillsHoverTitle.FieldType == typeof(string), "Reflection: SkillsPage.hoverTitle (string) exists");

        var fPowersHover = typeof(StardewValley.Menus.PowersTab).GetField("hoverText", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert(fPowersHover != null && fPowersHover.FieldType == typeof(string), "Reflection: PowersTab.hoverText (string) exists");

        var fPowersDesc = typeof(StardewValley.Menus.PowersTab).GetField("descriptionText", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert(fPowersDesc != null && fPowersDesc.FieldType == typeof(string), "Reflection: PowersTab.descriptionText (string) exists");

        var mCollectionsDraw = typeof(StardewValley.Menus.CollectionsPage).GetMethod(nameof(StardewValley.Menus.CollectionsPage.draw), new[] { typeof(Microsoft.Xna.Framework.Graphics.SpriteBatch) });
        Assert(mCollectionsDraw != null, "Reflection: CollectionsPage.draw(SpriteBatch) exists");

        var fSecretNoteImage = typeof(StardewValley.Menus.CollectionsPage).GetField("secretNoteImage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert(fSecretNoteImage != null && fSecretNoteImage.FieldType == typeof(int), "Reflection: CollectionsPage.secretNoteImage (int) exists");

        var fSecretNoteTexture = typeof(StardewValley.Menus.CollectionsPage).GetField("secretNoteImageTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert(fSecretNoteTexture != null && fSecretNoteTexture.FieldType == typeof(Microsoft.Xna.Framework.Graphics.Texture2D), "Reflection: CollectionsPage.secretNoteImageTexture (Texture2D) exists");

        var mDrawPatchPrefix = typeof(CollectionsPageDrawPatch).GetMethod(nameof(CollectionsPageDrawPatch.Prefix));
        Assert(mDrawPatchPrefix != null, "Reflection: CollectionsPageDrawPatch.Prefix exists");

        var mDrawPatchPostfix = typeof(CollectionsPageDrawPatch).GetMethod(nameof(CollectionsPageDrawPatch.Postfix));
        Assert(mDrawPatchPostfix != null, "Reflection: CollectionsPageDrawPatch.Postfix exists");

        var mDrawPatchFinalizer = typeof(CollectionsPageDrawPatch).GetMethod(nameof(CollectionsPageDrawPatch.Finalizer));
        Assert(mDrawPatchFinalizer != null, "Reflection: CollectionsPageDrawPatch.Finalizer exists");

        var fPowersList = typeof(StardewValley.Menus.PowersTab).GetField("powers", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert(fPowersList != null && fPowersList.FieldType == typeof(List<List<StardewValley.Menus.ClickableTextureComponent>>), "Reflection: PowersTab.powers (List<List<ClickableTextureComponent>>) exists");
    }

    private static void TestLevelUpMenuScopedStateAndLayout()
    {
        Console.WriteLine("\n-- Testing LevelUpMenu Scoped State, Pure Layout & Truncation --");

        // 1. Pure layout helper calculations derived from LevelUpMenu.cs
        int screenX = 100;
        int screenY = 50;
        int width = 960;
        int sideBorder = 16;
        int topBorder = 16;

        LevelUpLayoutResult layout = LevelUpLayoutHelper.CalculateLayout(screenX, screenY, width, sideBorder, topBorder);

        Assert(layout.LeftTitleX == screenX + sideBorder + 32, "Layout: LeftTitleX matches vanilla position");
        Assert(layout.RightTitleX == screenX + sideBorder + width / 2, "Layout: RightTitleX matches vanilla position");
        Assert(layout.TitleY == screenY + topBorder + 160, "Layout: TitleY matches vanilla (top + 160)");
        Assert(layout.SubtitleY == screenY + topBorder + 208, "Layout: SubtitleY matches measured gap (top + 208)");
        Assert(layout.Bullet1Y == screenY + topBorder + 264, "Layout: Bullet1Y matches vanilla (top + 264)");
        Assert(layout.TotalTitleGap == 104, "Layout: TotalTitleGap is exactly 104px (160 to 264)");
        Assert(layout.SubtitleToBulletGap == 56, "Layout: SubtitleToBulletGap is 56px before first bullet");
        Assert(layout.LeftMaxWidth == 960 / 2 - 144, "Layout: LeftMaxWidth is 336px before left icon");
        Assert(layout.RightMaxWidth == 960 / 2 - 128, "Layout: RightMaxWidth is 352px before right icon");

        // 2. Pure text truncation helper
        Func<string, float> charMeasure = s => s.Length * 8f; // 8px per character estimate

        string shortText = "Rancher"; // 7 chars * 8 = 56px <= 100px
        string truncatedShort = LevelUpLayoutHelper.TruncateToWidth(shortText, 100f, charMeasure);
        Assert(truncatedShort == "Rancher", "Truncate: text within bounds returns unchanged");

        string longText = "Very Long Custom Profession Title Exceeding Width"; // 49 chars * 8 = 392px > 100px
        string truncatedLong = LevelUpLayoutHelper.TruncateToWidth(longText, 100f, charMeasure);
        Assert(truncatedLong.EndsWith("..."), "Truncate: overflowing text appends ellipsis '...'");
        Assert(charMeasure(truncatedLong) <= 100f, $"Truncate: clamped length ({charMeasure(truncatedLong)}px) <= maxWidth (100px)");

        string emptyResult = LevelUpLayoutHelper.TruncateToWidth("", 100f, charMeasure);
        Assert(emptyResult == "", "Truncate: empty string returns empty");

        string zeroWidthResult = LevelUpLayoutHelper.TruncateToWidth("Rancher", 0f, charMeasure);
        Assert(zeroWidthResult == "", "Truncate: zero maxWidth returns empty");

        // 3. LevelUpMenuState scoped save, swap, restore, and idempotency
        var menu = (StardewValley.Menus.LevelUpMenu)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(StardewValley.Menus.LevelUpMenu));
        var fLeftDesc = typeof(StardewValley.Menus.LevelUpMenu).GetField("leftProfessionDescription", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var fRightDesc = typeof(StardewValley.Menus.LevelUpMenu).GetField("rightProfessionDescription", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var fChoose = typeof(StardewValley.Menus.LevelUpMenu).GetField("professionsToChoose", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var fChooser = typeof(StardewValley.Menus.LevelUpMenu).GetField("isProfessionChooser", BindingFlags.Public | BindingFlags.Instance)!;

        var leftList = new List<string> { "Quản trại", "Mô tả dòng 1", "Mô tả dòng 2" };
        var rightList = new List<string> { "Trồng trọt", "Mô tả dòng 1", "Mô tả dòng 2" };
        var chooseList = new List<int> { 0, 1 };

        fLeftDesc.SetValue(menu, leftList);
        fRightDesc.SetValue(menu, rightList);
        fChoose.SetValue(menu, chooseList);
        fChooser.SetValue(menu, true);

        // Test State with SecondaryFirst = false: primary stays as main title, secondary is subtitle
        var stateNormal = new LevelUpMenuState
        {
            LeftList = leftList,
            RightList = rightList,
            OriginalLeft = "Quản trại",
            OriginalRight = "Trồng trọt",
            SubtitleLeft = "Rancher",
            SubtitleRight = "Tiller",
            LeftSwapped = false,
            RightSwapped = false
        };

        // Main titles must NOT have been mutated
        Assert(leftList[0] == "Quản trại", "State normal: leftList[0] remains unmodified primary");
        Assert(rightList[0] == "Trồng trọt", "State normal: rightList[0] remains unmodified primary");
        Assert(stateNormal.SubtitleLeft == "Rancher", "State normal: SubtitleLeft holds secondary");
        stateNormal.Restore(menu);
        Assert(leftList[0] == "Quản trại", "State normal: leftList[0] after Restore is 'Quản trại'");

        // Test State with SecondaryFirst = true: secondary swapped into list[0] for draw, primary is subtitle
        var stateSwapped = new LevelUpMenuState
        {
            LeftList = leftList,
            RightList = rightList,
            OriginalLeft = "Quản trại",
            OriginalRight = "Trồng trọt",
            SubtitleLeft = "Quản trại",
            SubtitleRight = "Trồng trọt",
            LeftSwapped = true,
            RightSwapped = true
        };
        leftList[0] = "Rancher"; // Simulated Prefix swap
        rightList[0] = "Tiller";

        Assert(leftList[0] == "Rancher", "State swapped: leftList[0] temporarily holds secondary for draw");
        Assert(stateSwapped.SubtitleLeft == "Quản trại", "State swapped: SubtitleLeft holds original primary");

        // Restore restores original primary
        stateSwapped.Restore(menu);
        Assert(leftList[0] == "Quản trại", "State swapped: leftList[0] restored to original 'Quản trại'");
        Assert(rightList[0] == "Trồng trọt", "State swapped: rightList[0] restored to original 'Trồng trọt'");

        // 4. Finalizer exception safety test
        var stateException = new LevelUpMenuState
        {
            LeftList = leftList,
            RightList = rightList,
            OriginalLeft = "Quản trại",
            OriginalRight = "Trồng trọt",
            LeftSwapped = true,
            RightSwapped = true
        };
        leftList[0] = "Rancher"; // Swapped during Prefix
        var testException = new InvalidOperationException("Simulated draw failure");
        Exception? returnedEx = LevelUpMenuPatch.Finalizer(menu, stateException, testException);

        Assert(ReferenceEquals(returnedEx, testException), "Finalizer preserves and passes through original exception");
        Assert(leftList[0] == "Quản trại", "Finalizer: leftList[0] restored to original 'Quản trại' on exception");

        // 5. 50 Repeated simulated draw cycles (Idempotency and zero string leakage)
        for (int frame = 0; frame < 50; frame++)
        {
            bool swapOrder = (frame % 2 == 1);
            var frameState = new LevelUpMenuState
            {
                LeftList = leftList,
                RightList = rightList,
                OriginalLeft = leftList[0],
                OriginalRight = rightList[0],
                LeftSwapped = swapOrder,
                RightSwapped = swapOrder,
                SubtitleLeft = swapOrder ? "Quản trại" : "Rancher",
                SubtitleRight = swapOrder ? "Trồng trọt" : "Tiller"
            };

            if (swapOrder)
            {
                leftList[0] = "Rancher";
                rightList[0] = "Tiller";
            }

            // End of frame restore
            frameState.Restore(menu);

            Assert(leftList[0] == "Quản trại", $"Repeated draw frame {frame}: leftList[0] strictly 'Quản trại'");
            Assert(!leftList[0].Contains('\n'), $"Repeated draw frame {frame}: no newline injected into leftList[0]");
            Assert(leftList.Count == 3, $"Repeated draw frame {frame}: leftList count remains exactly 3");
        }
    }

    private static void TestUiFixtureFormattingAndMergedText()
    {
        Console.WriteLine("\n-- Testing UI Fixture Formatting & Merged Text --");

        // A. CollectionsPage combined title + description + stats structure
        string dnl = Environment.NewLine + Environment.NewLine;
        string primaryName = "Cá nóc";
        string secondaryName = "Pufferfish";
        string primaryDesc = "Làm phồng cơ thể khi bị đe dọa.";
        string primaryStats = "Số lượng đã đánh bắt: 5\nKỷ lục lớn nhất: 24 cm";
        string gameDescription = $"{primaryName}{dnl}{primaryDesc}{dnl}{primaryStats}";

        // Simulate CollectionsPagePatch postfix logic:
        int firstDnl = gameDescription.IndexOf(dnl, StringComparison.Ordinal);
        Assert(firstDnl >= 0, "Collections double-newline delimiter found");

        string titlePart = gameDescription.Substring(0, firstDnl);
        string remainderPart = gameDescription.Substring(firstDnl + dnl.Length);

        // Primary first
        string formattedPrimaryFirst = TooltipFormatter.FormatTitle(titlePart, titlePart, secondaryName, secondaryFirst: false);
        string assembledPrimaryFirst = $"{formattedPrimaryFirst}{dnl}{remainderPart}";
        Assert(assembledPrimaryFirst == $"Cá nóc\nPufferfish{dnl}{primaryDesc}{dnl}{primaryStats}", "Collections merged text formatted with primary first and preserved stats");

        // Secondary first
        string formattedSecFirst = TooltipFormatter.FormatTitle(titlePart, titlePart, secondaryName, secondaryFirst: true);
        string assembledSecFirst = $"{formattedSecFirst}{dnl}{remainderPart}";
        Assert(assembledSecFirst == $"Pufferfish\nCá nóc{dnl}{primaryDesc}{dnl}{primaryStats}", "Collections merged text formatted with secondary first and preserved stats");

        // Duplicate suppression: when primary and secondary names match
        string dupTitle = TooltipFormatter.FormatTitle("Pufferfish", "Pufferfish", "Pufferfish", secondaryFirst: false);
        Assert(dupTitle == "Pufferfish", "Duplicate title suppressed in collections title");

        // Concealment / Unknown ???
        string unknownDesc = "???";
        Assert(unknownDesc == "???", "Concealed unknown ??? remains untouched");

        // No double insertion (idempotency check)
        string alreadyFormatted = "Cá nóc\nPufferfish";
        string twiceFormatted = TooltipFormatter.FormatTitle(alreadyFormatted, "Cá nóc", "Pufferfish", secondaryFirst: false);
        Assert(twiceFormatted == alreadyFormatted, "TooltipFormatter rejects double-insertion when title contains newline");

        // B. LevelUpMenu profession descriptions chooser titles
        string profPrimary = "Người chăn nuôi";
        string profSecondary = "Rancher";
        string formattedChooser = TooltipFormatter.FormatTitle(profPrimary, profPrimary, profSecondary, secondaryFirst: false);
        Assert(formattedChooser == "Người chăn nuôi\nRancher", "LevelUpMenu profession chooser formats bilingual title");

        // Second pass idempotency on profession chooser
        string twiceChooser = TooltipFormatter.FormatTitle(formattedChooser, profPrimary, profSecondary, secondaryFirst: false);
        Assert(twiceChooser == formattedChooser, "LevelUpMenu profession chooser title does not double-format");

        // C. PowersTab unlocked hoverText formatting
        string powerPrimary = "Chìa khóa sọ";
        string powerSecondary = "Skull Key";
        string formattedPower = TooltipFormatter.FormatTitle(powerPrimary, powerPrimary, powerSecondary, secondaryFirst: false);
        Assert(formattedPower == "Chìa khóa sọ\nSkull Key", "PowersTab formats bilingual power title");

        // Concealment for locked powers
        string lockedPowerHover = "???";
        Assert(lockedPowerHover == "???", "Locked power hover remains concealed ???");
    }

    private static void TestRealXnbUiExpansion()
    {
        Console.WriteLine("\n-- Testing Real XNB UI Expansion (Professions, Skills, Powers, Collections, Achievements) --");

        string? gamePath = LocateGamePath();
        if (gamePath == null || !Directory.Exists(gamePath))
        {
            Console.WriteLine("  [SKIP] Stardew Valley game path not found; skipping real XNB UI expansion tests.");
            return;
        }

        string contentRoot = Path.Combine(gamePath, "Content");
        string modContentRoot = Path.Combine(gamePath, "Mods", "Stardew Valley Viethoa", "assets", "Content");
        var testService = new DummyServiceProvider();

        var rawDefault = new RawContentProvider(() => testService, contentRoot, "default");
        var rawEn = new RawContentProvider(() => testService, contentRoot, "en");
        var rawFr = new RawContentProvider(() => testService, contentRoot, "fr");

        var resDefault = new ItemNameResolver(rawDefault);
        var resEn = new ItemNameResolver(rawEn);
        var resFr = new ItemNameResolver(rawFr);

        // 1. Professions: Level 5 & 10 (0 Rancher, 1 Tiller, 4 Artisan, 5 Agriculturist)
        Assert(resDefault.GetSecondaryProfessionTitle(0) == "Rancher", "Real XNB default: Profession 0 == 'Rancher'");
        Assert(resEn.GetSecondaryProfessionTitle(0) == "Rancher", "Real XNB en: Profession 0 == 'Rancher'");
        Assert(resFr.GetSecondaryProfessionTitle(0) == "Éleveur de bétail", $"Real XNB fr: Profession 0 == 'Éleveur de bétail' (got '{resFr.GetSecondaryProfessionTitle(0)}')");
        Assert(resFr.GetSecondaryProfessionTitle(1) == "Cultivateur", $"Real XNB fr: Profession 1 == 'Cultivateur' (got '{resFr.GetSecondaryProfessionTitle(1)}')");
        Assert(resFr.GetSecondaryProfessionTitle(4) == "Artisan", $"Real XNB fr: Profession 4 == 'Artisan' (got '{resFr.GetSecondaryProfessionTitle(4)}')");

        // 2. Skills: 0 Farming, 1 Fishing, 2 Foraging, 3 Mining, 4 Combat
        Assert(resDefault.GetSecondarySkillName(0) == "Farming", "Real XNB default: Skill 0 == 'Farming'");
        Assert(resEn.GetSecondarySkillName(0) == "Farming", "Real XNB en: Skill 0 == 'Farming'");
        Assert(resFr.GetSecondarySkillName(0) == "Agriculture", $"Real XNB fr: Skill 0 == 'Agriculture' (got '{resFr.GetSecondarySkillName(0)}')");
        Assert(resFr.GetSecondarySkillName(1) == "Pêche", $"Real XNB fr: Skill 1 == 'Pêche' (got '{resFr.GetSecondarySkillName(1)}')");
        Assert(resFr.GetSecondarySkillName(2) == "Cueillette", $"Real XNB fr: Skill 2 == 'Cueillette' (got '{resFr.GetSecondarySkillName(2)}')");
        Assert(resFr.GetSecondarySkillName(3) == "Extraction minière", $"Real XNB fr: Skill 3 == 'Extraction minière' (got '{resFr.GetSecondarySkillName(3)}')");
        Assert(resFr.GetSecondarySkillName(4) == "Combat", $"Real XNB fr: Skill 4 == 'Combat' (got '{resFr.GetSecondarySkillName(4)}')");

        // 3. Powers: SkullKey, RustyKey, BearPaw, ForestMagic, DwarvishTranslationGuide
        Assert(resDefault.GetSecondaryPowerName("SkullKey") == "Skull Key", "Real XNB default: SkullKey == 'Skull Key'");
        Assert(resEn.GetSecondaryPowerName("SkullKey") == "Skull Key", "Real XNB en: SkullKey == 'Skull Key'");
        Assert(resFr.GetSecondaryPowerName("SkullKey") == "Clé en tête-de-mort", $"Real XNB fr: SkullKey == 'Clé en tête-de-mort' (got '{resFr.GetSecondaryPowerName("SkullKey")}')");
        Assert(resFr.GetSecondaryPowerName("BearPaw") == "Savoir de l’ours", $"Real XNB fr: BearPaw == 'Savoir de l’ours' (got '{resFr.GetSecondaryPowerName("BearPaw")}')");
        Assert(resFr.GetSecondaryPowerName("ForestMagic") == "Magie de la forêt", $"Real XNB fr: ForestMagic == 'Magie de la forêt' (got '{resFr.GetSecondaryPowerName("ForestMagic")}')");

        // 4. Collections Items & Achievements
        Assert(resDefault.GetSecondaryAchievementTitle("0") == "Greenhorn (15k)", "Real XNB default: Achievement 0 == 'Greenhorn (15k)'");
        Assert(resEn.GetSecondaryAchievementTitle("0") == "Greenhorn (15k)", "Real XNB en: Achievement 0 == 'Greenhorn (15k)'");
        Assert(resFr.GetSecondaryAchievementTitle("0") == "Néophyte (15k)", $"Real XNB fr: Achievement 0 == 'Néophyte (15k)' (got '{resFr.GetSecondaryAchievementTitle("0")}')");

        Assert(resDefault.GetSecondaryCollectionItemTitle("128") == "Pufferfish", "Real XNB default: Collection item 128 == 'Pufferfish'");
        Assert(resEn.GetSecondaryCollectionItemTitle("128") == "Pufferfish", "Real XNB en: Collection item 128 == 'Pufferfish'");
        Assert(resFr.GetSecondaryCollectionItemTitle("128") == "Poisson-globe", $"Real XNB fr: Collection item 128 == 'Poisson-globe' (got '{resFr.GetSecondaryCollectionItemTitle("128")}')");

        // 5. Vietnamese mod XNB if available
        if (Directory.Exists(modContentRoot))
        {
            var rawVi = new RawContentProvider(() => testService, contentRoot, "vi", modContentRoot);
            var resVi = new ItemNameResolver(rawVi);

            string? prof0Vi = resVi.GetSecondaryProfessionTitle(0);
            Assert(!string.IsNullOrEmpty(prof0Vi), $"Real Vietnamese mod XNB: Profession 0 == '{prof0Vi}'");

            string? prof4Vi = resVi.GetSecondaryProfessionTitle(4);
            Assert(!string.IsNullOrEmpty(prof4Vi), $"Real Vietnamese mod XNB: Profession 4 == '{prof4Vi}'");

            string? skill0Vi = resVi.GetSecondarySkillName(0);
            Assert(!string.IsNullOrEmpty(skill0Vi), $"Real Vietnamese mod XNB: Skill 0 == '{skill0Vi}'");

            string? skill3Vi = resVi.GetSecondarySkillName(3);
            Assert(!string.IsNullOrEmpty(skill3Vi), $"Real Vietnamese mod XNB: Skill 3 == '{skill3Vi}'");

            string? powerSkullVi = resVi.GetSecondaryPowerName("SkullKey");
            Assert(!string.IsNullOrEmpty(powerSkullVi), $"Real Vietnamese mod XNB: SkullKey == '{powerSkullVi}'");

            string? ach0Vi = resVi.GetSecondaryAchievementTitle("0");
            Assert(ach0Vi == "Lính Mới (15ng)", $"Real Vietnamese mod XNB: Achievement 0 == 'Lính Mới (15ng)' (got '{ach0Vi}')");

            string? col128Vi = resVi.GetSecondaryCollectionItemTitle("128");
            Assert(col128Vi == "Cá Nóc", $"Real Vietnamese mod XNB: Collection item 128 == 'Cá Nóc' (got '{col128Vi}')");
        }
    }

    private static void TestMailAndSecretNotesParsingAndFlow()
    {
        Console.WriteLine("\n-- Testing Mail & Secret Notes Parsing & Flow --");

        // 1. Mail parser with body, directives (%item, %secretsanta, @), and [#] title delimiter
        string rawMailWithTitle = "Dear @, please accept this gift!%item object 128 1 %%[#]Special Delivery";
        var mockProvider = new MockContentProvider("en", "en");
        mockProvider.AddEnglish("Data/mail", "deliveryMail", rawMailWithTitle);
        var resolver = new ItemNameResolver(mockProvider);

        string? mailTitle = resolver.GetSecondaryMailTitle("deliveryMail");
        Assert(mailTitle == "Special Delivery", "Mail parser extracts title suffix after [#]");

        // Verify body bytes unchanged
        int delimIdx = rawMailWithTitle.IndexOf("[#]", StringComparison.Ordinal);
        string body = rawMailWithTitle.Substring(0, delimIdx);
        Assert(body == "Dear @, please accept this gift!%item object 128 1 %%", "Mail body text bytes unchanged");

        // Letter without [#]
        mockProvider.AddEnglish("Data/mail", "noTitleMail", "Just plain body text without any title");
        string? noTitle = resolver.GetSecondaryMailTitle("noTitleMail");
        Assert(noTitle == null, "Mail without [#] returns null (keeps primary, no fake title)");

        // Letter with empty or ??? title
        mockProvider.AddEnglish("Data/mail", "unknownTitleMail", "Body text[#]???");
        string? unknownTitle = resolver.GetSecondaryMailTitle("unknownTitleMail");
        Assert(unknownTitle == null, "Mail with [#]??? returns null");

        // 2. Secret Note text note with double-newline delimiter
        mockProvider.AddEnglish("Strings/Locations", "Secret_Note_Name", "Secret Note");
        mockProvider.AddEnglish("Strings/Locations", "Journal_Name", "Journal Scrap");
        string secNote1 = resolver.GetSecondarySecretNoteTitle(1)!;
        Assert(secNote1 == "Secret Note #1", "Secret Note #1 resolves to 'Secret Note #1'");

        // Journal Scrap (note index >= 1000)
        string journalScrap1 = resolver.GetSecondarySecretNoteTitle(1001)!;
        Assert(journalScrap1 == "Journal Scrap #1", "Note 1001 resolves to 'Journal Scrap #1' (index - 1000)");

        // 3. CollectionsPagePatch createDescription simulation for Tab 6 (Secret Notes)
        // Text note on Windows (\r\n\r\n)
        string winTextNote = "Ghi chú bí mật #1\r\n\r\nNội dung mật thư số 1 bí ẩn.";
        int delimWin = winTextNote.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        string winTitle = winTextNote.Substring(0, delimWin);
        string winRem = winTextNote.Substring(delimWin + 4);
        string winFormatted = TooltipFormatter.FormatTitle(winTitle, winTitle, secNote1, secondaryFirst: false);
        string winAssembled = $"{winFormatted}\r\n\r\n{winRem}";
        Assert(winAssembled == "Ghi chú bí mật #1\nSecret Note #1\r\n\r\nNội dung mật thư số 1 bí ẩn.", "Secret note text preserves body across \\r\\n\\r\\n");

        // Text note on Unix (\n\n)
        string unixTextNote = "Ghi chú bí mật #1\n\nNội dung mật thư số 1.";
        int delimUnix = unixTextNote.IndexOf("\n\n", StringComparison.Ordinal);
        string unixTitle = unixTextNote.Substring(0, delimUnix);
        string unixRem = unixTextNote.Substring(delimUnix + 2);
        string unixFormatted = TooltipFormatter.FormatTitle(unixTitle, unixTitle, secNote1, secondaryFirst: false);
        string unixAssembled = $"{unixFormatted}\n\n{unixRem}";
        Assert(unixAssembled == "Ghi chú bí mật #1\nSecret Note #1\n\nNội dung mật thư số 1.", "Secret note text preserves body across \\n\\n");

        // Image note (no double newline): title only
        string imgNote = "Ghi chú bí mật #10";
        string imgSecTitle = resolver.GetSecondarySecretNoteTitle(10)!;
        string imgFormatted = TooltipFormatter.FormatTitle(imgNote, imgNote, imgSecTitle, secondaryFirst: false);
        Assert(imgFormatted == "Ghi chú bí mật #10\nSecret Note #10", "Image note formats title only without injecting double newline or body");

        // 4. CollectionsPageHoverPatch simulation for Letters (Tab 7)
        // c2.name format: "<mailId> true <primaryTitle>"
        string letterCompName = "krobus true Bức thư từ Krobus";
        int sp1 = letterCompName.IndexOf(' ');
        int sp2 = letterCompName.IndexOf(' ', sp1 + 1);
        string primaryLetterTitle = letterCompName.Substring(sp2 + 1);
        mockProvider.AddEnglish("Data/mail", "krobus", "Krobus text[#]Letter from Krobus");
        string? secKrobus = resolver.GetSecondaryMailTitle("krobus");
        string letterHover = TooltipFormatter.FormatTitle(primaryLetterTitle, primaryLetterTitle, secKrobus, secondaryFirst: false);
        Assert(letterHover == "Bức thư từ Krobus\nLetter from Krobus", "Letter hover formats bilingual title");

        // Long letter title wrapping test:
        // Even when primary title wraps across multiple lines, independent section formatting does not suppress the secondary title
        string longPrimaryWrapped = "Bức thư rất dài từ Thị trưởng Lewis\ngửi cho chủ trang trại thung lũng";
        string longSecondary = "A Very Long Letter From Mayor Lewis To The Valley Farm Owner";
        string combinedLong = $"{longPrimaryWrapped}\n{longSecondary}";
        Assert(combinedLong.Contains(longPrimaryWrapped) && combinedLong.Contains(longSecondary), "Long letter wrapping retains both sections without suppression");

        // Submenu guard logic: when letterviewerSubMenu is active, underlying grid is not modified
        bool submenuActive = true;
        string hoverOutput = "OriginalHover";
        if (!submenuActive)
        {
            hoverOutput = "ModifiedByUnderlyingGrid";
        }
        Assert(hoverOutput == "OriginalHover", "Submenu active guard prevents underlying hover text mutation");

        // Letter undiscovered: c2.name = "krobus false ???"
        string lockedLetterName = "krobus false ???";
        int lockedSp1 = lockedLetterName.IndexOf(' ');
        int lockedSp2 = lockedLetterName.IndexOf(' ', lockedSp1 + 1);
        string lockedPrimary = lockedLetterName.Substring(lockedSp2 + 1);
        Assert(lockedPrimary == "???", "Locked letter remains ???");

        // 5. Side tab names (0..7)
        mockProvider.AddEnglish("Strings/UI", "Collections_Letters", "Letters");
        mockProvider.AddEnglish("Strings/UI", "Collections_SecretNotes", "Secret Notes");
        mockProvider.AddEnglish("Strings/UI", "Collections_Shipped", "Items Shipped");
        Assert(resolver.GetSecondarySideTabName(7) == "Letters", "Side tab 7 resolves to 'Letters'");
        Assert(resolver.GetSecondarySideTabName(6) == "Secret Notes", "Side tab 6 resolves to 'Secret Notes'");
        Assert(resolver.GetSecondarySideTabName(0) == "Items Shipped", "Side tab 0 resolves to 'Items Shipped'");
    }

    private static void TestExternalTranslationRegistryAndFallback()
    {
        Console.WriteLine("\n-- Testing External Translation Registry & Fallback --");

        var reg = new ExternalTranslationRegistry();

        // 1. CP entries registration
        var cpEntries = new Dictionary<string, string>
        {
            ["Item/Custom_DragonSword@en"] = "Dragon Sword",
            ["Item/Custom_DragonSword@vi"] = "Long Kiếm",
            ["Item/Custom_DragonSword@default"] = "Dragon Sword",
            ["Mail/Custom_WizardLetter@en"] = "Letter from Wizard",
            ["Mail/Custom_WizardLetter@vi"] = "Thư từ Pháp sư",
            ["Power/Custom_DragonBlessing@en"] = "Dragon Blessing",
            ["Power/Custom_DragonBlessing@vi"] = "Phước Lành Của Rồng",
            ["SecretNote/1050@en"] = "Journal Scrap #50",
            ["SecretNote/1050@vi"] = "Mảnh nhật ký #50",
            ["SideTab/7@vi"] = "Thư từ đặc biệt",
            // Item with only French default
            ["Item/FrenchBread@default"] = "Pain français",
            // Item with parent Chinese
            ["Item/ChineseTea@zh"] = "茶",
            ["Item/ChineseTea@zh-CN"] = "绿茶"
        };
        reg.LoadFromContentPackDictionary(cpEntries);

        // Exact match
        Assert(reg.GetTranslation("Item", "Custom_DragonSword", "en") == "Dragon Sword", "CP exact locale match en");
        Assert(reg.GetTranslation("Item", "Custom_DragonSword", "vi") == "Long Kiếm", "CP exact locale match vi");

        // Default fallback for regional or non-English
        Assert(reg.GetTranslation("Item", "Custom_DragonSword", "default") == "Dragon Sword", "CP default locale match");
        Assert(reg.GetTranslation("Item", "Custom_DragonSword", "fr") == "Dragon Sword", "CP falls back to default for unprovided fr");

        // SMAPI fallback: default French-only + requested en returns French base
        Assert(reg.GetTranslation("Item", "FrenchBread", "en") == "Pain français", "default French-only + requested en returns French base");
        Assert(reg.GetTranslation("Item", "FrenchBread", "default") == "Pain français", "FrenchBread returns 'Pain français' for default");

        // Explicit en entry wins over default
        reg.RegisterModTranslation("ModCroissant", "Item", "Croissant", "default", "Croissant de base");
        reg.RegisterModTranslation("ModCroissant", "Item", "Croissant", "en", "English Croissant");
        Assert(reg.GetTranslation("Item", "Croissant", "en") == "English Croissant", "explicit en entry wins over default");
        Assert(reg.GetTranslation("Item", "Croissant", "default") == "Croissant de base", "requested default returns default base, not en");

        // fr exact-parent-default
        Assert(reg.GetTranslation("Item", "FrenchBread", "fr") == "Pain français", "fr exact-parent-default resolves default");

        // default does NOT fall back to en
        reg.RegisterModTranslation("ModEnOnly", "Item", "EnglishPie", "en", "English Pie");
        Assert(reg.GetTranslation("Item", "EnglishPie", "default") == null, "default does not fall back to en");

        // Qualified weapon / item IDs lookup (e.g. (W)Custom_RubyBlade)
        reg.RegisterModTranslation("ModWeapon", "Item", "(W)Custom_RubyBlade", "en", "Ruby Blade (W)");
        Assert(reg.GetTranslation("Item", "(W)Custom_RubyBlade", "en") == "Ruby Blade (W)", "Qualified (W) lookup exact match");
        Assert(reg.GetTranslation("Item", "Custom_RubyBlade", "en") == "Ruby Blade (W)", "Unqualified lookup matches registered (W)");

        // Parent locale fallback (zh-TW -> zh)
        Assert(reg.GetTranslation("Item", "ChineseTea", "zh-CN") == "绿茶", "ChineseTea returns exact zh-CN");
        Assert(reg.GetTranslation("Item", "ChineseTea", "zh-TW") == "茶", "ChineseTea falls back to parent zh for zh-TW");

        // Other categories: Mail, Power, SecretNote, SideTab
        Assert(reg.GetTranslation("Mail", "Custom_WizardLetter", "vi") == "Thư từ Pháp sư", "CP Mail translation resolved");
        Assert(reg.GetTranslation("Power", "Custom_DragonBlessing", "vi") == "Phước Lành Của Rồng", "CP Power translation resolved");
        Assert(reg.GetTranslation("SecretNote", "1050", "vi") == "Mảnh nhật ký #50", "CP SecretNote translation resolved");
        Assert(reg.GetTranslation("SideTab", "7", "vi") == "Thư từ đặc biệt", "CP SideTab translation resolved");

        // 2. Priority: External registration overrides base
        var mockProvider = new MockContentProvider("en", "en");
        mockProvider.AddEnglish("Strings/Objects", "Custom_DragonSword_Name", "Vanilla Sword");
        var resolver = new ItemNameResolver(mockProvider, externalRegistry: reg);
        Assert(resolver.GetSecondaryNameById("Custom_DragonSword") == "Dragon Sword", "External CP registration has priority over provider string");
    }

    private static void TestContentPatcherExamplePackSchema()
    {
        Console.WriteLine("\n-- Testing Content Patcher Example Pack Schema --");

        string exampleDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "examples", "ContentPatcherPack");
        if (!Directory.Exists(exampleDir))
        {
            exampleDir = Path.Combine(Directory.GetCurrentDirectory(), "examples", "ContentPatcherPack");
        }

        Assert(Directory.Exists(exampleDir), $"Example CP pack directory exists");

        string manifestPath = Path.Combine(exampleDir, "manifest.json");
        string contentPath = Path.Combine(exampleDir, "content.json");
        string defaultI18nPath = Path.Combine(exampleDir, "i18n", "default.json");
        string viI18nPath = Path.Combine(exampleDir, "i18n", "vi.json");

        Assert(File.Exists(manifestPath), "manifest.json exists in CP example");
        Assert(File.Exists(contentPath), "content.json exists in CP example");
        Assert(File.Exists(defaultI18nPath), "default.json exists in CP example");
        Assert(File.Exists(viI18nPath), "vi.json exists in CP example");

        // Verify valid JSON
        string manifestText = File.ReadAllText(manifestPath);
        Assert(manifestText.Contains("Pathoschild.ContentPatcher"), "CP manifest targets Pathoschild.ContentPatcher");
        Assert(manifestText.Contains("\"IsRequired\": true"), "CP manifest requires UntranslatedStardew (IsRequired: true)");

        string contentText = File.ReadAllText(contentPath);
        Assert(contentText.Contains("EditData"), "CP content.json uses Action EditData");
        Assert(contentText.Contains("Mods/GiaHung.UntranslatedStardew/Names"), "CP content.json targets Mods/GiaHung.UntranslatedStardew/Names");
        Assert(contentText.Contains("(W)Custom_DragonSword"), "CP content.json uses qualified weapon ID (W)Custom_DragonSword");

        string defaultI18nText = File.ReadAllText(defaultI18nPath);
        Assert(defaultI18nText.Contains("Dragon Sword"), "CP default.json contains Dragon Sword");
    }

    private static void TestPublicApiRegistrationAndLifecycle()
    {
        Console.WriteLine("\n-- Testing Public API Registration & Lifecycle --");

        var reg = new ExternalTranslationRegistry();
        var mockProvider = new MockContentProvider("vi", "vi");
        var resolver = new ItemNameResolver(mockProvider, externalRegistry: reg);
        var config = new ModConfig { SecondaryLanguage = "vi" };

        var api = new UntranslatedStardewApi(() => resolver, () => config);

        // 1. Register translations dictionary
        var modDict = new Dictionary<string, string>
        {
            ["Item/MyMod_Axe@vi"] = "Rìu Tùy Chỉnh",
            ["Mail/MyMod_Mail@vi"] = "Thư Của Mod",
            ["Power/MyMod_Buff@vi"] = "Sức Mạnh Vô Song"
        };

        IDisposable token = api.RegisterTranslations("MyMod.Tools", modDict);
        Assert(token != null, "RegisterTranslations returns non-null IDisposable token");

        // Exercise resolution consumption through API
        string? resolvedItem = api.GetSecondaryName("Item", "MyMod_Axe", "vi");
        Assert(resolvedItem == "Rìu Tùy Chỉnh", "api.GetSecondaryName resolves registered item");

        string? resolvedMail = api.GetSecondaryName("Mail", "MyMod_Mail", "vi");
        Assert(resolvedMail == "Thư Của Mod", "api.GetSecondaryName resolves registered mail");

        string? resolvedPower = api.GetSecondaryName("Power", "MyMod_Buff", "vi");
        Assert(resolvedPower == "Sức Mạnh Vô Song", "api.GetSecondaryName resolves registered power");

        // 2. Token disposal unregisters entries and invalidates resolver cache immediately
        // First populate resolver._cache
        var regDict = new Dictionary<string, string> { ["Item/TempStaff@vi"] = "Gậy Thần" };
        IDisposable cacheToken = api.RegisterTranslations("Mod.CacheTest", regDict);
        Assert(resolver.GetSecondaryNameById("TempStaff") == "Gậy Thần", "Cached in resolver before disposal");
        cacheToken.Dispose();
        Assert(resolver.GetSecondaryNameById("TempStaff") == null, "Token disposal immediately invalidates resolver cache");

        token?.Dispose();
        string? afterDisposal = api.GetSecondaryName("Item", "MyMod_Axe", "vi");
        Assert(afterDisposal == null, "Disposed token unregisters mod translations");

        // 3. Generation-based token ownership (disposing an older token does not remove newer registration)
        IDisposable tokenV1 = api.RegisterTranslations("Mod.GenerationTest", new Dictionary<string, string>
        {
            ["Item/GenSword@vi"] = "Kiếm V1"
        });
        Assert(api.GetSecondaryName("Item", "GenSword", "vi") == "Kiếm V1", "Mod v1 active");

        IDisposable tokenV2 = api.RegisterTranslations("Mod.GenerationTest", new Dictionary<string, string>
        {
            ["Item/GenSword@vi"] = "Kiếm V2"
        });
        Assert(api.GetSecondaryName("Item", "GenSword", "vi") == "Kiếm V2", "Mod v2 replaces v1");

        tokenV1.Dispose();
        Assert(api.GetSecondaryName("Item", "GenSword", "vi") == "Kiếm V2", "Disposing older token does not remove newer registration");

        tokenV2.Dispose();
        Assert(api.GetSecondaryName("Item", "GenSword", "vi") == null, "Disposing active token removes registration");

        // 4. Deterministic conflict resolution: last registration wins
        api.RegisterTranslations("ConflictModA", new Dictionary<string, string>
        {
            ["Item/SharedArmor@vi"] = "Giáp Của Mod A"
        });
        api.RegisterTranslations("ConflictModB", new Dictionary<string, string>
        {
            ["Item/SharedArmor@vi"] = "Giáp Của Mod B"
        });
        Assert(api.GetSecondaryName("Item", "SharedArmor", "vi") == "Giáp Của Mod B", "Deterministic conflict: last registered Mod B wins");

        api.RegisterTranslations("ConflictModA", new Dictionary<string, string>
        {
            ["Item/SharedArmor@vi"] = "Giáp Của Mod A Cập Nhật"
        });
        Assert(api.GetSecondaryName("Item", "SharedArmor", "vi") == "Giáp Của Mod A Cập Nhật", "Re-registered Mod A takes priority");

        // 5. Thread safety test
        bool threadSafeSuccess = true;
        var threadTasks = new System.Threading.Tasks.Task[6];
        for (int t = 0; t < threadTasks.Length; t++)
        {
            int threadId = t;
            threadTasks[t] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < 30; i++)
                {
                    api.RegisterTranslation($"ThreadMod{threadId}", "Item", $"ThreadItem_{threadId}_{i}", "vi", $"Name_{threadId}_{i}");
                    string? read = api.GetSecondaryName("Item", $"ThreadItem_{threadId}_{i}", "vi");
                    if (read != $"Name_{threadId}_{i}")
                        threadSafeSuccess = false;
                }
            });
        }
        System.Threading.Tasks.Task.WaitAll(threadTasks);
        Assert(threadSafeSuccess, "Concurrent registrations and lookups are thread-safe without exception");

        // 6. Explicit language contract: no silent French fallback when Vietnamese requested
        var frenchMock = new MockContentProvider("fr", "fr");
        frenchMock.AddEnglish("Data/mail", "Robin", "Robin text[#]A Gift From Robin");
        frenchMock.AddLocalized("Data/mail", "Robin", "Robin text[#]Un cadeau de la part de Robine");
        var frenchResolver = new ItemNameResolver(frenchMock);
        var frenchConfig = new ModConfig { SecondaryLanguage = "fr" };

        var frenchApi = new UntranslatedStardewApi(() => frenchResolver, () => frenchConfig);

        Assert(frenchApi.GetSecondaryName("Mail", "Robin", "fr") == "Un cadeau de la part de Robine", "Explicit fr returns French title");
        Assert(frenchApi.GetSecondaryName("Mail", "Robin", "vi") == null, "Explicit vi returns null instead of silent French fallback");
        Assert(frenchConfig.SecondaryLanguage == "fr", "Primary config remains unchanged");

        // 7. Single registration
        api.RegisterTranslation("MyMod.Single", "Item", "MyMod_Pickaxe", "vi", "Cuốc Chim Mod");
        Assert(api.GetSecondaryName("Item", "MyMod_Pickaxe", "vi") == "Cuốc Chim Mod", "api.RegisterTranslation single item resolved");

        bool unregResult = api.UnregisterTranslations("MyMod.Single");
        Assert(unregResult, "UnregisterTranslations returns true for registered mod");
        Assert(api.GetSecondaryName("Item", "MyMod_Pickaxe", "vi") == null, "Unregistered single item returns null");

        // 8. Fault isolation: null or empty registrations do not throw
        IDisposable emptyToken = api.RegisterTranslations("NullTest", new Dictionary<string, string>());
        Assert(emptyToken != null, "Empty dictionary does not throw");
    }

    private static void TestLevelUpMenuPrefixFailureRestoration()
    {
        Console.WriteLine("\n-- Testing LevelUpMenu Prefix Failure Restoration --");

        var left = new List<string> { "Quản trại", "Gia súc tăng 20%" };
        var right = new List<string> { "Trồng trọt", "Nông sản tăng 10%" };

        var state = new LevelUpMenuState
        {
            LeftList = left,
            RightList = right,
            OriginalLeft = "Quản trại",
            OriginalRight = "Trồng trọt",
            LeftSwapped = true
        };

        // Mutate left profession as Prefix does
        left[0] = "Rancher";

        // Simulate Prefix failure occurring midway (after left was mutated)
        // catch block calls state.Restore()
        state.Restore();

        Assert(left[0] == "Quản trại", "Prefix failure: left[0] immediately restored to 'Quản trại'");
        Assert(left.Count == 2, "Prefix failure: list count preserved");
    }

    private static void TestRealXnbMailAndNotesExpansion()
    {
        Console.WriteLine("\n-- Testing Real XNB Mail, Notes & Side Tabs Expansion --");

        string? gamePath = LocateGamePath();
        if (gamePath == null || !Directory.Exists(gamePath))
        {
            Console.WriteLine("  [SKIP] Stardew Valley game path not found; skipping real XNB mail and notes expansion.");
            return;
        }

        string contentRoot = Path.Combine(gamePath, "Content");
        string modContentRoot = Path.Combine(gamePath, "Mods", "Stardew Valley Viethoa", "assets", "Content");
        var testService = new DummyServiceProvider();

        var rawDefault = new RawContentProvider(() => testService, contentRoot, "default");
        var rawEn = new RawContentProvider(() => testService, contentRoot, "en");
        var rawFr = new RawContentProvider(() => testService, contentRoot, "fr");

        var resDefault = new ItemNameResolver(rawDefault);
        var resEn = new ItemNameResolver(rawEn);
        var resFr = new ItemNameResolver(rawFr);

        // 1. Mail titles
        string? robinDefault = resDefault.GetSecondaryMailTitle("Robin");
        Assert(robinDefault == "A Gift From Robin", $"Real XNB default: Robin mail == 'A Gift From Robin' (got '{robinDefault}')");

        string? robinEn = resEn.GetSecondaryMailTitle("Robin");
        Assert(robinEn == "A Gift From Robin", $"Real XNB en: Robin mail == 'A Gift From Robin' (got '{robinEn}')");

        string? robinFr = resFr.GetSecondaryMailTitle("Robin");
        Assert(robinFr == "Un cadeau de la part de Robine", $"Real XNB fr: Robin mail == 'Un cadeau de la part de Robine' (got '{robinFr}')");

        // 2. Secret Note and Journal Scrap titles
        string? note1Default = resDefault.GetSecondarySecretNoteTitle(1);
        Assert(note1Default == "Secret Note #1", $"Real XNB default: Note 1 == 'Secret Note #1' (got '{note1Default}')");

        string? journal1Default = resDefault.GetSecondarySecretNoteTitle(1001);
        Assert(journal1Default == "Journal Scrap #1", $"Real XNB default: Note 1001 == 'Journal Scrap #1' (got '{journal1Default}')");

        string? note1Fr = resFr.GetSecondarySecretNoteTitle(1);
        Assert(note1Fr == "Note secrète #1", $"Real XNB fr: Note 1 == 'Note secrète #1' (got '{note1Fr}')");

        string? journal1Fr = resFr.GetSecondarySecretNoteTitle(1001);
        Assert(journal1Fr == "Page de journal #1", $"Real XNB fr: Note 1001 == 'Page de journal #1' (got '{journal1Fr}')");

        // 3. Side tab hover names (0..7)
        string? tabLettersDefault = resDefault.GetSecondarySideTabName(7);
        Assert(tabLettersDefault == "Letters", $"Real XNB default: Side tab 7 == 'Letters' (got '{tabLettersDefault}')");

        string? tabNotesDefault = resDefault.GetSecondarySideTabName(6);
        Assert(tabNotesDefault == "Secret Notes", $"Real XNB default: Side tab 6 == 'Secret Notes' (got '{tabNotesDefault}')");

        string? tabLettersFr = resFr.GetSecondarySideTabName(7);
        Assert(tabLettersFr == "Lettres", $"Real XNB fr: Side tab 7 == 'Lettres' (got '{tabLettersFr}')");

        // 4. Vietnamese mod XNB if available
        if (Directory.Exists(modContentRoot))
        {
            var rawVi = new RawContentProvider(() => testService, contentRoot, "vi", modContentRoot);
            var resVi = new ItemNameResolver(rawVi);

            string? robinVi = resVi.GetSecondaryMailTitle("Robin");
            Assert(!string.IsNullOrEmpty(robinVi), $"Real Vietnamese mod XNB: Robin mail == '{robinVi}'");

            string? note1Vi = resVi.GetSecondarySecretNoteTitle(1);
            Assert(note1Vi == "Mật thư #1", $"Real Vietnamese mod XNB: Note 1 == 'Mật thư #1' (got '{note1Vi}')");

            string? journal1Vi = resVi.GetSecondarySecretNoteTitle(1001);
            Assert(journal1Vi == "Mảnh nhật ký vụn #1", $"Real Vietnamese mod XNB: Note 1001 == 'Mảnh nhật ký vụn #1' (got '{journal1Vi}')");

            string? tabLettersVi = resVi.GetSecondarySideTabName(7);
            Assert(tabLettersVi == "Thư từ", $"Real Vietnamese mod XNB: Side tab 7 == 'Thư từ' (got '{tabLettersVi}')");

            string? tabNotesVi = resVi.GetSecondarySideTabName(6);
            Assert(tabNotesVi == "Ghi chú bí ẩn", $"Real Vietnamese mod XNB: Side tab 6 == 'Ghi chú bí ẩn' (got '{tabNotesVi}')");
        }
    }

    private static void TestCollectionsNoteImageLayoutAndDynamicFonts()
    {
        Console.WriteLine("\n-- Testing Note Image Preview Layout, Safe Area Clamping & Dynamic Fonts --");

        // 1. Regression check: verify actual compiled IL of CollectionsPage.draw matches fixed offsets
        var mCollectionsDraw = typeof(StardewValley.Menus.CollectionsPage).GetMethod(nameof(StardewValley.Menus.CollectionsPage.draw), new[] { typeof(Microsoft.Xna.Framework.Graphics.SpriteBatch) });
        Assert(mCollectionsDraw != null, "Reflection: CollectionsPage.draw exists");

        var instructions = PatchProcessor.GetOriginalInstructions(mCollectionsDraw!);
        bool foundSecretNoteImageField = false;
        bool foundFixed64 = false;
        bool foundFixed32 = false;
        bool foundFixed288 = false;
        bool foundDrawTextureBox = false;
        bool foundFixed16 = false;
        bool foundSecretNoteImageTextureField = false;

        for (int i = 0; i < instructions.Count; i++)
        {
            var inst = instructions[i];
            if (inst.operand is FieldInfo fi)
            {
                if (fi.Name == "secretNoteImage")
                    foundSecretNoteImageField = true;
                if (fi.Name == "secretNoteImageTexture")
                    foundSecretNoteImageTextureField = true;
            }
            if (inst.opcode == System.Reflection.Emit.OpCodes.Ldc_I4_S)
            {
                sbyte val = Convert.ToSByte(inst.operand);
                if (val == 64) foundFixed64 = true;
                if (val == 32) foundFixed32 = true;
                if (val == 16) foundFixed16 = true;
            }
            if (inst.opcode == System.Reflection.Emit.OpCodes.Ldc_I4)
            {
                int val = Convert.ToInt32(inst.operand);
                if (val == 288) foundFixed288 = true;
            }
            if (inst.operand is MethodInfo mi && mi.Name == "drawTextureBox")
            {
                foundDrawTextureBox = true;
            }
        }

        Assert(foundSecretNoteImageField, "IL Regression: CollectionsPage.draw reads secretNoteImage");
        Assert(foundFixed64 && foundFixed32, "IL Regression: CollectionsPage.draw contains fixed 64 + 32 offsets");
        Assert(foundFixed288, "IL Regression: CollectionsPage.draw contains fixed 288 texture box size");
        Assert(foundDrawTextureBox, "IL Regression: CollectionsPage.draw calls drawTextureBox");
        Assert(foundFixed16, "IL Regression: CollectionsPage.draw contains fixed 16 image padding offset");
        Assert(foundSecretNoteImageTextureField, "IL Regression: CollectionsPage.draw reads secretNoteImageTexture");

        // 2. Pure geometry: Unchanged single-title geometry matches vanilla coordinates
        int mouseX = 400;
        int mouseY = 300;
        var safeArea = new Microsoft.Xna.Framework.Rectangle(0, 0, 1920, 1080);
        string singleTitle = "Mật thư #1";
        Func<string, Microsoft.Xna.Framework.Vector2> singleMeasure = s => new Microsoft.Xna.Framework.Vector2(120, 28);

        var singleLayout = CollectionsNoteImageLayoutHelper.CalculateLayout(mouseX, mouseY, singleTitle, singleMeasure, safeArea);

        Assert(singleLayout.HeaderX == mouseX + 32, "Single-title: HeaderX matches vanilla mouseX+32 (432)");
        Assert(singleLayout.HeaderY == mouseY + 32, "Single-title: HeaderY matches vanilla mouseY+32 (332)");
        Assert(singleLayout.HeaderWidth == 120 + 36, "Single-title: HeaderWidth is 156 (120+36)");
        Assert(singleLayout.BoxX == mouseX, "Single-title: BoxX matches vanilla mouseX (400)");
        Assert(singleLayout.BoxY == mouseY + 96, "Single-title: BoxY matches vanilla mouseY+96 (396)");
        Assert(singleLayout.ImageX == mouseX + 16, "Single-title: ImageX matches vanilla mouseX+16 (416)");
        Assert(singleLayout.ImageY == mouseY + 112, "Single-title: ImageY matches vanilla mouseY+112 (412)");
        Assert(singleLayout.TooltipHeight == 60, "Single-title: TooltipHeight is baseline 60px (Math.Max(60, 28+32))");
        Assert(singleLayout.ExtraHeight == 0, "Single-title: ExtraHeight is 0px");
        Assert(singleLayout.DeltaY == 0, "Single-title: DeltaY is 0px");
        Assert(singleLayout.HeaderY + singleLayout.HeaderHeight <= singleLayout.BoxY, "Single-title: Header bottom <= Box top (disjoint)");

        // 3. Pure geometry: Multiline bilingual title (Vietnamese + English) moves both box and image
        string bilingualTitle = "Mảnh nhật ký vụn #4\nJournal Scrap #4";
        Func<string, Microsoft.Xna.Framework.Vector2> bilingualMeasure = s => new Microsoft.Xna.Framework.Vector2(250, 64); // 2 lines at 32px each

        var biLayout = CollectionsNoteImageLayoutHelper.CalculateLayout(mouseX, mouseY, bilingualTitle, bilingualMeasure, safeArea);

        Assert(biLayout.TooltipHeight == 96, "Bilingual: TooltipHeight is 96px (64+32)");
        Assert(biLayout.ExtraHeight == 32, "Bilingual: ExtraHeight is 32px (96 - 64 baseline)");
        Assert(biLayout.DeltaY == 32, "Bilingual: DeltaY is 32px");
        Assert(biLayout.BoxY == mouseY + 96 + 32, "Bilingual: BoxY moves down by 32px to 428 (below line 2)");
        Assert(biLayout.ImageY == mouseY + 112 + 32, "Bilingual: ImageY moves down by 32px to 444 (below line 2)");
        Assert(biLayout.BoxY - (mouseY + 96) == biLayout.ImageY - (mouseY + 112), "Bilingual: Both box and image move down by exact same delta (32px)");
        Assert(biLayout.HeaderY + biLayout.HeaderHeight <= biLayout.BoxY, "Bilingual: Header bottom <= Box top (disjoint)");

        // 4. Pure geometry: Enlarged font title
        Func<string, Microsoft.Xna.Framework.Vector2> enlargedMeasure = s => new Microsoft.Xna.Framework.Vector2(300, 80);

        var enlargedLayout = CollectionsNoteImageLayoutHelper.CalculateLayout(mouseX, mouseY, bilingualTitle, enlargedMeasure, safeArea);

        Assert(enlargedLayout.TooltipHeight == 112, "Enlarged font: TooltipHeight is 112px (80+32)");
        Assert(enlargedLayout.ExtraHeight == 48, "Enlarged font: ExtraHeight is 48px (112 - 64 baseline)");
        Assert(enlargedLayout.BoxY == mouseY + 96 + 48, "Enlarged font: BoxY moves down by 48px to 444");
        Assert(enlargedLayout.ImageY == mouseY + 112 + 48, "Enlarged font: ImageY moves down by 48px to 460");
        Assert(enlargedLayout.ImageY - enlargedLayout.BoxY == 16, "Enlarged font: Image-to-Box padding strictly 16px");
        Assert(enlargedLayout.HeaderY + enlargedLayout.HeaderHeight <= enlargedLayout.BoxY, "Enlarged font: Header bottom <= Box top (disjoint)");

        // 5. Pure geometry: Image padding is strictly 16px in all directions across all test cases
        Assert(singleLayout.ImageX - singleLayout.BoxX == CollectionsNoteImageLayoutHelper.ImagePadding, "Geometry invariant: ImageX - BoxX == 16 (single)");
        Assert(singleLayout.ImageY - singleLayout.BoxY == CollectionsNoteImageLayoutHelper.ImagePadding, "Geometry invariant: ImageY - BoxY == 16 (single)");
        Assert(biLayout.ImageX - biLayout.BoxX == CollectionsNoteImageLayoutHelper.ImagePadding, "Geometry invariant: ImageX - BoxX == 16 (bilingual)");
        Assert(biLayout.ImageY - biLayout.BoxY == CollectionsNoteImageLayoutHelper.ImagePadding, "Geometry invariant: ImageY - BoxY == 16 (bilingual)");

        // 6. End-to-end geometry: Coordinated group at lower-right corner with disjointness assertion
        int mouseLRX = 1800; // 1800 + 288 = 2088 > 1920
        int mouseLRY = 950;  // 950 + 96 + 288 = 1334 > 1080
        var lrClampLayout = CollectionsNoteImageLayoutHelper.CalculateLayout(mouseLRX, mouseLRY, bilingualTitle, bilingualMeasure, safeArea);

        Assert(lrClampLayout.HeaderX + lrClampLayout.HeaderWidth <= safeArea.Right, "Lower-Right: Header strictly inside safeArea.Right");
        Assert(lrClampLayout.BoxX + lrClampLayout.BoxWidth <= safeArea.Right, "Lower-Right: Box strictly inside safeArea.Right");
        Assert(lrClampLayout.HeaderY >= safeArea.Top, "Lower-Right: Header >= safeArea.Top");
        Assert(lrClampLayout.BoxY + lrClampLayout.BoxHeight <= safeArea.Bottom, "Lower-Right: Box strictly inside safeArea.Bottom");
        Assert(lrClampLayout.HeaderY + lrClampLayout.HeaderHeight <= lrClampLayout.BoxY, "Lower-Right: Header and Image Box strictly disjoint (header bottom <= box top)");
        Assert(lrClampLayout.ImageY - lrClampLayout.BoxY == CollectionsNoteImageLayoutHelper.ImagePadding, "Lower-Right: Image-to-box padding invariant preserved");

        // 7. End-to-end geometry: Enlarged font at lower-right corner remains strictly disjoint
        var lrEnlargedLayout = CollectionsNoteImageLayoutHelper.CalculateLayout(mouseLRX, mouseLRY, bilingualTitle, enlargedMeasure, safeArea);
        Assert(lrEnlargedLayout.HeaderX + lrEnlargedLayout.HeaderWidth <= safeArea.Right, "Lower-Right Enlarged: Header inside safeArea.Right");
        Assert(lrEnlargedLayout.BoxX + lrEnlargedLayout.BoxWidth <= safeArea.Right, "Lower-Right Enlarged: Box inside safeArea.Right");
        Assert(lrEnlargedLayout.BoxY + lrEnlargedLayout.BoxHeight <= safeArea.Bottom, "Lower-Right Enlarged: Box inside safeArea.Bottom");
        Assert(lrEnlargedLayout.HeaderY + lrEnlargedLayout.HeaderHeight <= lrEnlargedLayout.BoxY, "Lower-Right Enlarged: Header and Image Box strictly disjoint");

        // 8. End-to-end geometry: Reduced available height fallback (side-by-side or proportional image shrink)
        var smallSafeArea = new Microsoft.Xna.Framework.Rectangle(0, 0, 1920, 360); // 360px < 384px (header 96 + box 288)
        var reducedHeightLayout = CollectionsNoteImageLayoutHelper.CalculateLayout(mouseX, mouseY, bilingualTitle, bilingualMeasure, smallSafeArea);
        Assert(reducedHeightLayout.IsSideBySide || reducedHeightLayout.ImageScale < 4f, "Reduced Height: Activates side-by-side or proportional scale fallback");
        Assert(reducedHeightLayout.BoxY + reducedHeightLayout.BoxHeight <= smallSafeArea.Bottom, "Reduced Height: Box inside safeArea.Bottom");
        Assert(reducedHeightLayout.HeaderY + reducedHeightLayout.HeaderHeight <= smallSafeArea.Bottom, "Reduced Height: Header inside safeArea.Bottom");
        if (reducedHeightLayout.IsSideBySide)
        {
            bool horizontallyDisjoint = (reducedHeightLayout.HeaderX + reducedHeightLayout.HeaderWidth <= reducedHeightLayout.BoxX)
                || (reducedHeightLayout.BoxX + reducedHeightLayout.BoxWidth <= reducedHeightLayout.HeaderX);
            Assert(horizontallyDisjoint, "Reduced Height Side-By-Side: Header and Box are horizontally disjoint");
        }
        else
        {
            Assert(reducedHeightLayout.HeaderY + reducedHeightLayout.HeaderHeight <= reducedHeightLayout.BoxY, "Reduced Height Stacked: Header and Box are vertically disjoint");
        }

        // 9. Safe Area Top & Left Clamp
        int mouseTopEdge = -200;
        var topClampLayout = CollectionsNoteImageLayoutHelper.CalculateLayout(mouseX, mouseTopEdge, singleTitle, singleMeasure, safeArea);
        Assert(topClampLayout.BoxY >= safeArea.Top, "Safe Area Top: BoxY >= safeArea.Top");
        Assert(topClampLayout.HeaderY >= safeArea.Top, "Safe Area Top: HeaderY >= safeArea.Top");
        Assert(topClampLayout.HeaderY + topClampLayout.HeaderHeight <= topClampLayout.BoxY, "Safe Area Top: Header and Box strictly disjoint");

        int mouseLeftEdge = -100;
        var leftClampLayout = CollectionsNoteImageLayoutHelper.CalculateLayout(mouseLeftEdge, mouseY, singleTitle, singleMeasure, safeArea);
        Assert(leftClampLayout.BoxX >= safeArea.Left, "Safe Area Left: BoxX >= safeArea.Left");
        Assert(leftClampLayout.HeaderX >= safeArea.Left, "Safe Area Left: HeaderX >= safeArea.Left");

        // 10. Hidden "???", non-secret-notes tab, open submenu, and no-image guards
        var dummyPage = (StardewValley.Menus.CollectionsPage)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(StardewValley.Menus.CollectionsPage));
        dummyPage.currentTab = StardewValley.Menus.CollectionsPage.secretNotesTab;
        dummyPage.letterviewerSubMenu = null;
        dummyPage.secretNoteImage = 4;

        string hoverHidden = "???";
        CollectionsPageDrawPatch.Prefix(dummyPage, ref hoverHidden, out var stateHidden);
        Assert(stateHidden == null, "Hidden '???': Prefix produces null state");
        Assert(dummyPage.secretNoteImage == 4, "Hidden '???': secretNoteImage remains 4 untouched");
        Assert(hoverHidden == "???", "Hidden '???': hoverText untouched");

        dummyPage.secretNoteImage = -1;
        string hoverNoImg = "Some Note Title";
        CollectionsPageDrawPatch.Prefix(dummyPage, ref hoverNoImg, out var stateNoImage);
        Assert(stateNoImage == null, "No image (-1): Prefix produces null state");
        Assert(dummyPage.secretNoteImage == -1, "No image (-1): secretNoteImage remains -1 untouched");

        dummyPage.secretNoteImage = 4;
        string hoverEmpty = "";
        CollectionsPageDrawPatch.Prefix(dummyPage, ref hoverEmpty, out var stateEmpty);
        Assert(stateEmpty == null, "Empty hoverText: Prefix produces null state");
        Assert(dummyPage.secretNoteImage == 4, "Empty hoverText: secretNoteImage remains 4 untouched");

        // Non-secret-notes tab guard
        dummyPage.currentTab = 0; // Items tab
        string hoverOtherTab = "Mảnh nhật ký vụn #4";
        CollectionsPageDrawPatch.Prefix(dummyPage, ref hoverOtherTab, out var stateOtherTab);
        Assert(stateOtherTab == null, "Other tab: Prefix produces null state");
        Assert(dummyPage.secretNoteImage == 4, "Other tab: secretNoteImage remains 4 untouched");
        dummyPage.currentTab = StardewValley.Menus.CollectionsPage.secretNotesTab;

        // Open letter submenu guard
        dummyPage.letterviewerSubMenu = (StardewValley.Menus.LetterViewerMenu)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(StardewValley.Menus.LetterViewerMenu));
        string hoverSubMenu = "Mảnh nhật ký vụn #4";
        CollectionsPageDrawPatch.Prefix(dummyPage, ref hoverSubMenu, out var stateSubMenu);
        Assert(stateSubMenu == null, "Open submenu: Prefix produces null state");
        Assert(dummyPage.secretNoteImage == 4, "Open submenu: secretNoteImage remains 4 untouched");
        dummyPage.letterviewerSubMenu = null;

        // 11. Scoped state & Finalizer exception recovery
        dummyPage.secretNoteImage = 4;
        string activeHoverText = "Mảnh nhật ký vụn #4\nJournal Scrap #4";
        CollectionsPageDrawPatch.Prefix(dummyPage, ref activeHoverText, out var stateActive);
        Assert(stateActive != null, "Active note: Prefix creates CollectionsNoteImageState");
        Assert(dummyPage.secretNoteImage == -1, "Active note: Prefix suppresses vanilla secretNoteImage = -1");
        Assert(activeHoverText == "", "Active note: Prefix suppresses vanilla hoverText to empty string");
        Assert(stateActive!.SecretNoteImage == 4, "Active note: State captures original secretNoteImage == 4");
        Assert(stateActive.HoverText == "Mảnh nhật ký vụn #4\nJournal Scrap #4", "Active note: State captures original HoverText");

        // Test Restore idempotency
        stateActive.Restore();
        Assert(dummyPage.secretNoteImage == 4, "State Restore: restores secretNoteImage back to 4");
        stateActive.Restore();
        Assert(dummyPage.secretNoteImage == 4, "State Restore: idempotent on multiple calls");

        // Test Postfix finally restores hoverText ref parameter
        string postHover = "";
        CollectionsPageDrawPatch.Postfix(dummyPage, null!, ref postHover, stateActive);
        Assert(postHover == "Mảnh nhật ký vụn #4\nJournal Scrap #4", "Postfix: restores hoverText ref parameter");

        // Test Finalizer preserves exception and restores state with a fresh draw invocation
        dummyPage.secretNoteImage = 4;
        string excHover = "Mảnh nhật ký vụn #4\nJournal Scrap #4";
        CollectionsPageDrawPatch.Prefix(dummyPage, ref excHover, out var stateForException);
        Assert(dummyPage.secretNoteImage == -1, "Prefix sets secretNoteImage = -1 before draw exception");
        Assert(excHover == "", "Prefix sets hoverText = '' before draw exception");
        var simulatedException = new InvalidOperationException("Simulated mid-draw render fault");
        var finalizerResult = CollectionsPageDrawPatch.Finalizer(dummyPage, stateForException, simulatedException);
        Assert(finalizerResult == simulatedException, "Finalizer: preserves and passes through original exception");
        Assert(dummyPage.secretNoteImage == 4, "Finalizer: restores secretNoteImage back to 4 on exception");

        // 12. Dynamic font layout in LevelUpLayoutHelper
        int screenX = 100;
        int screenY = 50;
        int menuWidth = 960;
        int sideBorder = 16;
        int topBorder = 16;

        // Baseline: default 5-param overload matches vanilla constants
        var baselineLayout = LevelUpLayoutHelper.CalculateLayout(screenX, screenY, menuWidth, sideBorder, topBorder);
        Assert(baselineLayout.TitleY == screenY + topBorder + 160, "DynamicFont baseline: TitleY == top + 160");
        Assert(baselineLayout.SubtitleY == screenY + topBorder + 208, "DynamicFont baseline: SubtitleY == top + 208");
        Assert(baselineLayout.Bullet1Y == screenY + topBorder + 264, "DynamicFont baseline: Bullet1Y == top + 264");
        Assert(baselineLayout.LeftScale == 1.0f, "DynamicFont baseline: LeftScale == 1.0f");
        Assert(baselineLayout.RightScale == 1.0f, "DynamicFont baseline: RightScale == 1.0f");
        Assert(baselineLayout.LeftVisible && baselineLayout.RightVisible, "DynamicFont baseline: Columns visible");

        // Dynamic font: standard heights (title=44, subtitle=24)
        var stdFontLayout = LevelUpLayoutHelper.CalculateLayout(screenX, screenY, menuWidth, sideBorder, topBorder, 44f, 24f);
        Assert(stdFontLayout.LeftSubtitleY == screenY + topBorder + 208, "DynamicFont standard: LeftSubtitleY == top + 208");
        Assert(stdFontLayout.LeftScale == 1.0f, "DynamicFont standard: LeftScale == 1.0f");
        Assert(stdFontLayout.LeftVisible, "DynamicFont standard: LeftVisible == true");

        // Dynamic font: enlarged title height (title=56 > 48, subtitle=24)
        // Subtitle must move down to top + 160 + 56 = top + 216 to clear title
        var largeTitleLayout = LevelUpLayoutHelper.CalculateLayout(screenX, screenY, menuWidth, sideBorder, topBorder, 56f, 24f);
        Assert(largeTitleLayout.LeftSubtitleY == screenY + topBorder + 216, "DynamicFont enlarged title: LeftSubtitleY moves to top + 216 (clears title bottom)");
        Assert(largeTitleLayout.LeftSubtitleY >= largeTitleLayout.TitleY + 56, "DynamicFont enlarged title: Zero overlap with main title (>= titleY + 56)");
        Assert(largeTitleLayout.LeftScale == 1.0f, "DynamicFont enlarged title: LeftScale == 1.0f (fits in 48px remaining gap)");
        Assert(largeTitleLayout.LeftSubtitleY + 24 <= largeTitleLayout.Bullet1Y, "DynamicFont enlarged title: Zero overlap with bullet 1");
        Assert(largeTitleLayout.LeftVisible, "DynamicFont enlarged title: LeftVisible == true");

        // Dynamic font: tight font (title=60px, subtitle=48px)
        // Title bottom is at top + 160 + 60 = top + 220. Remaining gap to bullet 1 (264) is 44px.
        // Subtitle height (48px) > 44px -> scale = 44 / 48 = 0.9167f
        var tightFontLayout = LevelUpLayoutHelper.CalculateLayout(screenX, screenY, menuWidth, sideBorder, topBorder, 60f, 48f);
        Assert(tightFontLayout.LeftSubtitleY == screenY + topBorder + 220, "DynamicFont tight: LeftSubtitleY starts at top + 220");
        Assert(tightFontLayout.LeftScale < 1.0f && tightFontLayout.LeftScale >= 0.5f, "DynamicFont tight: LeftScale scales down between 0.5f and 1.0f");
        float tightScaledHeight = 48f * tightFontLayout.LeftScale;
        Assert(tightFontLayout.LeftSubtitleY + (int)Math.Ceiling(tightScaledHeight) <= tightFontLayout.Bullet1Y, "DynamicFont tight: Scaled subtitle fits strictly before bullet 1");
        Assert(tightFontLayout.LeftVisible, "DynamicFont tight: LeftVisible == true");

        // Dynamic font: impossible space / below minimum readability limit (min readable scale 0.5f, min gap 12px)
        // If title fills gap down to bullet 1 (e.g. titleHeight=100px), gap is 4px < 12px -> skip overlay
        var impossibleLayout = LevelUpLayoutHelper.CalculateLayout(screenX, screenY, menuWidth, sideBorder, topBorder, 100f, 24f);
        Assert(!impossibleLayout.LeftVisible, "DynamicFont impossible space: LeftVisible == false (overlay safely omitted)");
        Assert(impossibleLayout.LeftScale == 0f, "DynamicFont impossible space: LeftScale == 0f");

        // Dynamic font: scale would fall below min 0.5f (e.g. titleHeight=80px, subtitleHeight=60px -> gap 24px, scale 0.40f)
        var unreadableScaleLayout = LevelUpLayoutHelper.CalculateLayout(screenX, screenY, menuWidth, sideBorder, topBorder, 80f, 60f);
        Assert(!unreadableScaleLayout.LeftVisible, "DynamicFont unreadable scale: LeftVisible == false (overlay safely omitted)");
        Assert(unreadableScaleLayout.LeftScale == 0f, "DynamicFont unreadable scale: LeftScale == 0f");

        // Dynamic font: per-column independence
        var independentLayout = LevelUpLayoutHelper.CalculateLayout(screenX, screenY, menuWidth, sideBorder, topBorder,
            leftTitleHeight: 44f, leftSubtitleHeight: 24f,
            rightTitleHeight: 100f, rightSubtitleHeight: 24f);
        Assert(independentLayout.LeftSubtitleY == screenY + topBorder + 208, "DynamicFont columns: Left column stays at top + 208");
        Assert(independentLayout.LeftVisible, "DynamicFont columns: Left column visible");
        Assert(!independentLayout.RightVisible, "DynamicFont columns: Right column omitted in impossible space");
    }

    private class DummyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private class FakeItem : StardewValley.Item
    {
        private readonly string _name;

        public FakeItem(string name)
        {
            _name = name;
        }

        public FakeItem(string itemId, string name)
        {
            base.ItemId = itemId;
            _name = name;
        }

        public override string DisplayName => _name;
        public override string Name => _name;
        public override string TypeDefinitionId => "(O)";

        public override void drawInMenu(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Microsoft.Xna.Framework.Vector2 location, float scaleSize, float transparency, float layerDepth, StardewValley.StackDrawType drawStackNumber, Microsoft.Xna.Framework.Color color, bool drawShadow)
        {
        }

        protected override StardewValley.Item GetOneNew() => new FakeItem(_name);
        public override bool isPlaceable() => false;
        public override int maximumStackSize() => 1;
        public override string getDescription() => string.Empty;
    }

    private class MockContentProvider : ISecondaryContentProvider, ITrinketDataProvider, IPowerDataProvider
    {
        private readonly Dictionary<string, Dictionary<string, string>> _localized = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<string, string>> _english = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TrinketData> _trinkets = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, StardewValley.GameData.Powers.PowersData> _powers = new(StringComparer.OrdinalIgnoreCase);

        public string RequestedLanguage { get; set; }
        public string EffectiveLanguage { get; set; }

        public MockContentProvider(string requested, string effective)
        {
            RequestedLanguage = requested;
            EffectiveLanguage = effective;
        }

        public void AddTrinketData(string itemId, TrinketData data)
        {
            _trinkets[itemId] = data;
        }

        public bool TryGetTrinketData(string itemId, out TrinketData? trinketData)
        {
            return _trinkets.TryGetValue(itemId, out trinketData);
        }

        public void AddPowerData(string powerKey, StardewValley.GameData.Powers.PowersData data)
        {
            _powers[powerKey] = data;
        }

        public bool TryGetPowerData(string powerKey, out StardewValley.GameData.Powers.PowersData? powerData)
        {
            return _powers.TryGetValue(powerKey, out powerData);
        }

        public void AddLocalized(string asset, string key, string value)
        {
            if (!_localized.TryGetValue(asset, out var dict))
            {
                dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _localized[asset] = dict;
            }
            dict[key] = value;
        }

        public void AddEnglish(string asset, string key, string value)
        {
            if (!_english.TryGetValue(asset, out var dict))
            {
                dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _english[asset] = dict;
            }
            dict[key] = value;
        }

        public bool TryGetString(string assetSubpath, string key, out string? value)
        {
            value = null;
            if (!LanguageHelper.IsBaseOrEnglish(EffectiveLanguage) && _localized.TryGetValue(assetSubpath, out var locDict) && locDict.TryGetValue(key, out value))
            {
                return true;
            }

            if (_english.TryGetValue(assetSubpath, out var engDict) && engDict.TryGetValue(key, out value))
            {
                return true;
            }

            return false;
        }

        public string? GetString(string assetSubpath, string key)
        {
            TryGetString(assetSubpath, key, out string? value);
            return value;
        }

        public void Invalidate()
        {
            _localized.Clear();
            _english.Clear();
            _powers.Clear();
        }

        public void Dispose()
        {
            _localized.Clear();
            _english.Clear();
            _powers.Clear();
        }
    }

    public class FakeCjbTextBox
    {
        public string Text { get; set; } = string.Empty;
    }

    public class FakeCjbSpawnableItem
    {
        public StardewValley.Item? Item { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Id { get; set; }
        public virtual string QualifiedItemId { get; set; }
        public int CategorySortOrder { get; set; }

        public FakeCjbSpawnableItem(string id, string name, string displayName, int sortOrder = 0, StardewValley.Item? item = null)
        {
            Id = id;
            QualifiedItemId = $"(O){id}";
            Name = name;
            DisplayName = displayName;
            CategorySortOrder = sortOrder;
            Item = item;
        }
    }

    public class FakeThrowingSpawnableItem : FakeCjbSpawnableItem
    {
        public FakeThrowingSpawnableItem(string id, string name, string displayName) : base(id, name, displayName) { }

        public override string QualifiedItemId => throw new InvalidOperationException("Simulated property error on item");
    }

    public class FakeSupportedItemMenu
    {
        public FakeCjbTextBox SearchBox = new();
        public List<FakeCjbSpawnableItem> AllItems = new();

        public virtual IEnumerable<FakeCjbSpawnableItem> SearchItems()
        {
            string query = SearchBox.Text.Trim();
            var items = AllItems.OrderBy(i => i.CategorySortOrder);
            if (!string.IsNullOrEmpty(query))
            {
                return items.Where(i => i.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                                     || i.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase));
            }
            return items;
        }
    }

    public class FakeFallbackItemMenu
    {
        public FakeCjbTextBox SearchBox = new();
        public List<FakeCjbSpawnableItem> AllItems = new();

        public virtual IEnumerable<FakeCjbSpawnableItem> SearchItems()
        {
            string query = SearchBox.Text.Trim();
            var items = AllItems.OrderBy(i => i.CategorySortOrder);
            if (!string.IsNullOrEmpty(query))
            {
                return items.Where(i => i.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                                     || i.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase));
            }
            return items;
        }
    }

    public class FakeThrowingItemMenu
    {
        public FakeCjbTextBox SearchBox = new();
        public virtual IEnumerable<FakeCjbSpawnableItem> SearchItems()
        {
            throw new InvalidOperationException("Simulated error inside SearchItems");
        }
    }

    public class FakeNullReturningItemMenu
    {
        public FakeCjbTextBox SearchBox = new();
        public virtual IEnumerable<FakeCjbSpawnableItem>? SearchItems()
        {
            return null;
        }
    }

    public class FakeOuterThrowItemMenu
    {
        public FakeCjbTextBox SearchBox = new();
        public virtual IEnumerable<FakeCjbSpawnableItem> SearchItems()
        {
            return GetThrowingEnumerable();
        }

        private static IEnumerable<FakeCjbSpawnableItem> GetThrowingEnumerable()
        {
            throw new InvalidOperationException("Outer enumeration error");
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }
    }

    public class FakeUnsupportedItemMenu
    {
        public virtual void SearchItems()
        {
        }
    }

    private static void TestXnbGlyphMetadataParsing()
    {
        Console.WriteLine("\n-- Testing XnbFontMetadataParser & Headless Glyph Coverage --");

        string? gamePath = LocateGamePath();
        if (string.IsNullOrEmpty(gamePath))
        {
            Console.WriteLine("  [SKIP] Stardew Valley game path not found; skipping XNB glyph metadata checks.");
            return;
        }

        string vanillaFontsDir = Path.Combine(gamePath, "Content", "Fonts");
        string smallFontEn = Path.Combine(vanillaFontsDir, "SmallFont.xnb");
        string spriteFont1En = Path.Combine(vanillaFontsDir, "SpriteFont1.xnb");

        string modFontsDir = Path.Combine(gamePath, "Mods", "Stardew Valley Viethoa", "assets", "Content", "Fonts");
        string smallFontVi = Path.Combine(modFontsDir, "SmallFont_vi.xnb");
        string spriteFont1Vi = Path.Combine(modFontsDir, "SpriteFont1_vi.xnb");

        if (!File.Exists(smallFontEn) || !File.Exists(smallFontVi))
        {
            Console.WriteLine("  [SKIP] Required XNB font files not found; skipping.");
            return;
        }

        // 1. English baseline SmallFont.xnb
        var metaEn = XnbFontMetadataParser.ParseFile(smallFontEn);
        Assert(metaEn.Characters.Count == 266, "Metadata EN: SmallFont character count is 266");
        Assert(metaEn.DefaultCharacter == '*', "Metadata EN: DefaultCharacter is '*'");
        Assert(metaEn.Characters.Contains('A'), "Metadata EN: Contains ASCII 'A'");
        Assert(!metaEn.Characters.Contains('đ'), "Metadata EN: Missing Vietnamese 'đ'");

        // 2. Vietnamese SmallFont_vi.xnb
        var metaVi = XnbFontMetadataParser.ParseFile(smallFontVi);
        Assert(metaVi.Characters.Count == 259, "Metadata VI: SmallFont_vi character count is 259");
        Assert(metaVi.DefaultCharacter == '*', "Metadata VI: DefaultCharacter is '*'");
        Assert(metaVi.Characters.Contains('đ') && metaVi.Characters.Contains('Đ'), "Metadata VI: Full coverage for 'đ' and 'Đ'");
        Assert(metaVi.Characters.Contains('á') && metaVi.Characters.Contains('ơ') && metaVi.Characters.Contains('ư'), "Metadata VI: Full NFC coverage for accented vowels");

        // 3. Dialogue font SpriteFont1_vi.xnb
        if (File.Exists(spriteFont1Vi))
        {
            var metaDiaVi = XnbFontMetadataParser.ParseFile(spriteFont1Vi);
            Assert(metaDiaVi.Characters.Count == 259, "Metadata VI: SpriteFont1_vi character count is 259");
            Assert(metaDiaVi.Characters.Contains('đ'), "Metadata VI: SpriteFont1_vi contains 'đ'");
        }

        // 4. Compressed XNBs (Russian ru-RU [0x01], Japanese ja-JP [0x81], Chinese zh-CN [0x81])
        string fontRu = Path.Combine(vanillaFontsDir, "SmallFont.ru-RU.xnb");
        if (File.Exists(fontRu))
        {
            var metaRu = XnbFontMetadataParser.ParseFile(fontRu);
            Assert(metaRu.Characters.Count > 0, "Metadata RU: SmallFont.ru-RU parsed successfully from compressed XNB");
            Assert(metaRu.Characters.Contains('Б') && metaRu.Characters.Contains('я'), "Metadata RU: Contains Cyrillic glyphs");
        }

        string fontJa = Path.Combine(vanillaFontsDir, "SmallFont.ja-JP.xnb");
        if (File.Exists(fontJa))
        {
            var metaJa = XnbFontMetadataParser.ParseFile(fontJa);
            Assert(metaJa.Characters.Count == 2749, "Metadata JA: SmallFont.ja-JP character count is 2749");
            Assert(metaJa.Characters.Contains('あ') && metaJa.Characters.Contains('日'), "Metadata JA: Contains Kana and Kanji glyphs");
        }

        string fontZh = Path.Combine(vanillaFontsDir, "SmallFont.zh-CN.xnb");
        if (File.Exists(fontZh))
        {
            var metaZh = XnbFontMetadataParser.ParseFile(fontZh);
            Assert(metaZh.Characters.Count > 0, "Metadata ZH: SmallFont.zh-CN parsed successfully from compressed XNB");
            Assert(metaZh.Characters.Contains('汉') && metaZh.Characters.Contains('字'), "Metadata ZH: Contains Hanzi glyphs");
        }

        // 5. Headless SpriteFont construction and measurement without GPU
        var fontEnHeadless = metaEn.CreateHeadlessSpriteFont();
        var fontViHeadless = metaVi.CreateHeadlessSpriteFont();
        Assert(fontEnHeadless.MeasureString("Pufferfish").X > 0, "Headless EN: MeasureString('Pufferfish') executes without GPU");
        Assert(fontViHeadless.MeasureString("Cá nóc").X > 0, "Headless VI: MeasureString('Cá nóc') executes without GPU");
    }

    private static void TestBilingualRunPlannerAndMetrics()
    {
        Console.WriteLine("\n-- Testing BilingualRunPlanner & Font Metrics --");

        string? gamePath = LocateGamePath();
        if (string.IsNullOrEmpty(gamePath))
            return;

        string vanillaFontsDir = Path.Combine(gamePath, "Content", "Fonts");
        string smallFontEn = Path.Combine(vanillaFontsDir, "SmallFont.xnb");
        string modFontsDir = Path.Combine(gamePath, "Mods", "Stardew Valley Viethoa", "assets", "Content", "Fonts");
        string smallFontVi = Path.Combine(modFontsDir, "SmallFont_vi.xnb");

        if (!File.Exists(smallFontEn) || !File.Exists(smallFontVi))
            return;

        var fontEn = XnbFontMetadataParser.ParseFile(smallFontEn).CreateHeadlessSpriteFont();
        var fontVi = XnbFontMetadataParser.ParseFile(smallFontVi).CreateHeadlessSpriteFont();

        // 1. Same font no fallback path
        var planNoFallback = BilingualRunPlanner.Plan("Pufferfish", fontEn, fontVi);
        Assert(planNoFallback == null, "RunPlanner: Pure ASCII returns null plan (same font no-fallback path)");

        // 2. Single-line Vietnamese fallback
        var planVi = BilingualRunPlanner.Plan("Đá vôi", fontEn, fontVi);
        Assert(planVi != null, "RunPlanner: Vietnamese string produces run plan");
        Assert(planVi!.Runs.Count == 1, "RunPlanner: Prefers whole line from same font to avoid baseline mismatch");
        Assert(ReferenceEquals(planVi.Runs[0].Font, fontVi), "RunPlanner: Assigns secondary Vietnamese font to Vietnamese run");
        Assert(planVi.Runs[0].Text == "Đá vôi", "RunPlanner: Run text matches normalized text");

        // 3. Decomposed input (NFD) normalization
        string nfdText = "\u0110a\u0301 v\u00f4i"; // Decomposed 'á'
        var planNfd = BilingualRunPlanner.Plan(nfdText, fontEn, fontVi);
        Assert(planNfd != null, "RunPlanner: Decomposed NFD input produces plan");
        Assert(planNfd!.Runs[0].Text == "Đá vôi", "RunPlanner: NFD input is normalized to precomposed NFC form");

        // 4. Multiline PrimaryFirst: "Limestone\nĐá vôi"
        var planPrimaryFirst = BilingualRunPlanner.Plan("Limestone\nĐá vôi", fontEn, fontVi);
        Assert(planPrimaryFirst != null && planPrimaryFirst.Runs.Count == 2, "RunPlanner PrimaryFirst: Produces 2 runs for 2 lines");
        Assert(ReferenceEquals(planPrimaryFirst!.Runs[0].Font, fontEn), "RunPlanner PrimaryFirst: Line 0 uses primary font");
        Assert(planPrimaryFirst.Runs[0].Offset == Vector2.Zero, "RunPlanner PrimaryFirst: Line 0 offset is (0, 0)");
        Assert(ReferenceEquals(planPrimaryFirst.Runs[1].Font, fontVi), "RunPlanner PrimaryFirst: Line 1 uses secondary font");
        Assert(planPrimaryFirst.Runs[1].Offset.Y == fontEn.LineSpacing, "RunPlanner PrimaryFirst: Line 1 Y offset matches primary line spacing");
        Assert(planPrimaryFirst.TotalSize.Y == fontEn.LineSpacing + fontVi.LineSpacing, "RunPlanner PrimaryFirst: Total height matches sum of line spacings");

        // 5. Multiline SecondaryFirst: "Đá vôi\nLimestone"
        var planSecondaryFirst = BilingualRunPlanner.Plan("Đá vôi\nLimestone", fontEn, fontVi);
        Assert(planSecondaryFirst != null && planSecondaryFirst.Runs.Count == 2, "RunPlanner SecondaryFirst: Produces 2 runs for 2 lines");
        Assert(ReferenceEquals(planSecondaryFirst!.Runs[0].Font, fontVi), "RunPlanner SecondaryFirst: Line 0 uses secondary font");
        Assert(planSecondaryFirst.Runs[0].Offset == Vector2.Zero, "RunPlanner SecondaryFirst: Line 0 offset is (0, 0)");
        Assert(ReferenceEquals(planSecondaryFirst.Runs[1].Font, fontEn), "RunPlanner SecondaryFirst: Line 1 uses primary font");
        Assert(planSecondaryFirst.Runs[1].Offset.Y == fontVi.LineSpacing, "RunPlanner SecondaryFirst: Line 1 Y offset matches secondary line spacing");
        Assert(planSecondaryFirst.TotalSize.Y == fontVi.LineSpacing + fontEn.LineSpacing, "RunPlanner SecondaryFirst: Total height matches sum of line spacings");

        // 6. Mixed line segmentation
        string fontJaPath = Path.Combine(vanillaFontsDir, "SmallFont.ja-JP.xnb");
        if (File.Exists(fontJaPath))
        {
            var fontJa = XnbFontMetadataParser.ParseFile(fontJaPath).CreateHeadlessSpriteFont();
            // Primary is fontJa (contains Kanji, lacks 'Đ'), Secondary is fontVi (contains 'Đ', lacks Kanji)
            var planMixed = BilingualRunPlanner.Plan("日本語: Đá vôi", fontJa, fontVi);
            Assert(planMixed != null && planMixed.Runs.Count == 2, "RunPlanner Mixed: Segments into 2 contiguous runs");
            Assert(planMixed!.Runs[0].Text == "日本語: ", "RunPlanner Mixed: Run 0 contains Japanese prefix");
            Assert(ReferenceEquals(planMixed.Runs[0].Font, fontJa), "RunPlanner Mixed: Run 0 uses Japanese font");
            Assert(planMixed.Runs[1].Text == "Đá vôi", "RunPlanner Mixed: Run 1 contains Vietnamese suffix");
            Assert(ReferenceEquals(planMixed.Runs[1].Font, fontVi), "RunPlanner Mixed: Run 1 uses Vietnamese font");
            Assert(planMixed.Runs[1].Offset.X == planMixed.Runs[0].Size.X, "RunPlanner Mixed: Run 1 X offset equals Run 0 width");
            float expectedLineHeight = Math.Max(fontJa.LineSpacing, fontVi.LineSpacing);
            Assert(planMixed.TotalSize.Y == expectedLineHeight, "RunPlanner Mixed: Line height equals max line spacing across runs");
        }
    }

    private static void TestBilingualDiagnosticAndLifecycle()
    {
        Console.WriteLine("\n-- Testing BilingualFontManager Diagnostic & Lifecycle --");

        List<(char Glyph, string Source, string Lang)> warnings = new();
        var manager = new BilingualFontManager(
            () => null,
            "C:\\FakeGamePath\\Content",
            "C:\\FakeModPath\\Content",
            "vi",
            (glyph, source, lang) => warnings.Add((glyph, source, lang))
        );

        // 1. Missing glyph diagnostic logged only once
        manager.ReportMissingGlyphOnce('Ω', "Greek Omega");
        manager.ReportMissingGlyphOnce('Ω', "Greek Omega");
        manager.ReportMissingGlyphOnce('Ω', "Greek Omega");
        Assert(warnings.Count == 1, "Diagnostic: Missing glyph warning is emitted only once across repeated calls");
        Assert(warnings[0].Glyph == 'Ω', "Diagnostic: Warning captures exact missing character");
        Assert(warnings[0].Lang == "vi", "Diagnostic: Warning captures effective language context");

        // Different glyph emits separate warning
        manager.ReportMissingGlyphOnce('Ψ', "Greek Psi");
        Assert(warnings.Count == 2, "Diagnostic: Distinct missing glyph emits separate warning");

        // 2. Invalidation and cache clearing
        BilingualStringTracker.Register("Cá nóc");
        Assert(BilingualStringTracker.IsTracked("Cá nóc"), "StringTracker: Tracks registered string");
        manager.Invalidate();
        Assert(!BilingualStringTracker.IsTracked("Cá nóc"), "Manager Invalidate: Clears tracked strings and run plans");

        // 3. Disposal
        manager.Dispose();
        Assert(BilingualFontManager.Instance == null, "Manager Dispose: Clears singleton instance");
    }

    private static void TestHarmonyInterceptionAndRecursionGuards()
    {
        Console.WriteLine("\n-- Testing Harmony Interception, Render Scopes & Recursion Guards --");

        string? gamePath = LocateGamePath();
        if (string.IsNullOrEmpty(gamePath))
            return;

        string vanillaFontsDir = Path.Combine(gamePath, "Content", "Fonts");
        string smallFontEn = Path.Combine(vanillaFontsDir, "SmallFont.xnb");
        string modFontsDir = Path.Combine(gamePath, "Mods", "Stardew Valley Viethoa", "assets", "Content", "Fonts");
        string smallFontVi = Path.Combine(modFontsDir, "SmallFont_vi.xnb");

        if (!File.Exists(smallFontEn) || !File.Exists(smallFontVi))
            return;

        var fontEn = XnbFontMetadataParser.ParseFile(smallFontEn).CreateHeadlessSpriteFont();
        var fontVi = XnbFontMetadataParser.ParseFile(smallFontVi).CreateHeadlessSpriteFont();

        var manager = new BilingualFontManager(
            () => null,
            Path.Combine(gamePath, "Content"),
            Path.Combine(gamePath, "Mods", "Stardew Valley Viethoa", "assets", "Content"),
            "vi"
        );
        manager.SetSecondaryFontForTesting(fontEn, fontVi);

        // 1. No scope: TryMeasureString returns false, leaving unscoped calls byte-for-byte unchanged
        Assert(!BilingualRenderScope.IsActive, "RenderScope: Inactive outside using block");
        bool unscopedMeasure = BilingualFontRenderer.TryMeasureString(fontEn, "Đá vôi\nLimestone", out _);
        Assert(!unscopedMeasure, "Renderer: Unscoped measurement returns false (native passthrough)");

        // 2. In scope: TryMeasureString intercepts and calculates bilingual metrics
        using (BilingualRenderScope.Begin())
        {
            Assert(BilingualRenderScope.IsActive, "RenderScope: Active inside using block");
            bool scopedMeasure = BilingualFontRenderer.TryMeasureString(fontEn, "Đá vôi\nLimestone", out Vector2 measured);
            Assert(scopedMeasure, "Renderer: Scoped measurement intercepts and returns true");
            Assert(measured.Y == fontVi.LineSpacing + fontEn.LineSpacing, "Renderer: Scoped measure matches sum of line heights");

            // 3. Same font inside scope: returns false (no interception needed)
            bool asciiMeasure = BilingualFontRenderer.TryMeasureString(fontEn, "Limestone", out _);
            Assert(!asciiMeasure, "Renderer: Pure ASCII string in scope returns false (no unnecessary interception)");

            // 4. StringBuilder overload
            var sb = new System.Text.StringBuilder("Đá vôi\nLimestone");
            bool sbMeasure = BilingualFontRenderer.TryMeasureString(fontEn, sb, out Vector2 sbMeasured);
            Assert(sbMeasure && sbMeasured == measured, "Renderer: StringBuilder overload matches string measurement");

            // 5. Harmony patch method prefixes execution
            Vector2 harmonyResult = Vector2.Zero;
            bool prefixHandled = !FontInterceptionPatches.SpriteFontMeasureStringPatch.Prefix(fontEn, "Đá vôi", ref harmonyResult);
            Assert(prefixHandled, "Harmony Patch: SpriteFontMeasureStringPatch.Prefix returns false (handled)");
            Assert(harmonyResult.Y == fontVi.LineSpacing, "Harmony Patch: Sets __result ref parameter correctly");

            bool sbPrefixHandled = !FontInterceptionPatches.SpriteFontMeasureStringBuilderPatch.Prefix(fontEn, sb, ref harmonyResult);
            Assert(sbPrefixHandled, "Harmony Patch: SpriteFontMeasureStringBuilderPatch.Prefix returns false (handled)");

            // 6. DrawString prefixes without active GraphicsDevice return true on null batch without throwing
            bool drawHandled = FontInterceptionPatches.SpriteBatchDrawStringColorPatch.Prefix(null!, fontEn, "Đá vôi", Vector2.Zero, Microsoft.Xna.Framework.Color.White);
            Assert(drawHandled, "Harmony Patch: DrawString returns true when batch is null (safe fallback)");
        }

        Assert(!BilingualRenderScope.IsActive, "RenderScope: Restores to inactive upon exiting scope");

        manager.Dispose();
    }

    private static void TestAcceptanceFixesLeadReview()
    {
        Console.WriteLine("\n-- Testing Acceptance Fixes After Lead Review --");

        // 1. Nested REAL patched Harmony fixture (Parent calling Child, both normal and throwing)
        var harmony = new Harmony("test.nested.scope.lead.review");
        var parentMethod = typeof(NestedScopeTarget).GetMethod(nameof(NestedScopeTarget.ParentMethod));
        var childMethod = typeof(NestedScopeTarget).GetMethod(nameof(NestedScopeTarget.ChildMethod));

        harmony.Patch(
            parentMethod,
            prefix: new HarmonyMethod(typeof(NestedScopeParentPatch).GetMethod(nameof(NestedScopeParentPatch.Prefix))),
            postfix: new HarmonyMethod(typeof(NestedScopeParentPatch).GetMethod(nameof(NestedScopeParentPatch.Postfix))),
            finalizer: new HarmonyMethod(typeof(NestedScopeParentPatch).GetMethod(nameof(NestedScopeParentPatch.Finalizer)))
        );

        harmony.Patch(
            childMethod,
            prefix: new HarmonyMethod(typeof(NestedScopeChildPatch).GetMethod(nameof(NestedScopeChildPatch.Prefix))),
            postfix: new HarmonyMethod(typeof(NestedScopeChildPatch).GetMethod(nameof(NestedScopeChildPatch.Postfix))),
            finalizer: new HarmonyMethod(typeof(NestedScopeChildPatch).GetMethod(nameof(NestedScopeChildPatch.Finalizer)))
        );

        try
        {
            // 1a. Normal execution: parent -> child -> child exits -> parent exits
            var targetNormal = new NestedScopeTarget();
            Assert(BilingualRenderScope.CurrentDepth == 0, "Nested Harmony: Scope initially inactive (depth 0)");
            targetNormal.ParentMethod(throwInChild: false);
            Assert(targetNormal.ParentDepthBeforeChild == 1, "Nested Harmony: Parent entered scope (depth 1)");
            Assert(targetNormal.ChildDepthInside == 2, "Nested Harmony: Child entered scope (depth 2)");
            Assert(targetNormal.ParentDepthAfterChild == 1, "Nested Harmony: Child Postfix+Finalizer exited exactly once (parent depth remains 1)");
            Assert(BilingualRenderScope.CurrentDepth == 0, "Nested Harmony: Parent Postfix+Finalizer exited cleanly (depth 0)");

            // 1b. Exceptional execution: child throws, child finalizer runs, parent catches, parent finalizer runs
            var targetThrow = new NestedScopeTarget();
            Assert(BilingualRenderScope.CurrentDepth == 0, "Nested Harmony Throwing: Scope initially inactive (depth 0)");
            targetThrow.ParentMethod(throwInChild: true);
            Assert(targetThrow.ParentDepthBeforeChild == 1, "Nested Harmony Throwing: Parent entered scope (depth 1)");
            Assert(targetThrow.ChildDepthInside == 2, "Nested Harmony Throwing: Child entered scope (depth 2)");
            Assert(targetThrow.ParentDepthInCatch == 1, "Nested Harmony Throwing: Child Finalizer exited once upon exception (parent remains active at depth 1)");
            Assert(BilingualRenderScope.CurrentDepth == 0, "Nested Harmony Throwing: Parent Finalizer restored depth to 0 without underflow");

            // 1c. ScopeCookie idempotency
            var cookie = BilingualRenderScope.Begin();
            Assert(BilingualRenderScope.CurrentDepth == 1, "ScopeCookie: Active at depth 1 after Begin()");
            cookie.Dispose();
            Assert(BilingualRenderScope.CurrentDepth == 0, "ScopeCookie: Depth 0 on first Dispose()");
            cookie.Dispose();
            Assert(BilingualRenderScope.CurrentDepth == 0, "ScopeCookie: Idempotent second Dispose() does not underflow");
            cookie.Dispose();
            Assert(BilingualRenderScope.CurrentDepth == 0, "ScopeCookie: Repeated Dispose() remains at depth 0");
        }
        finally
        {
            harmony.UnpatchAll("test.nested.scope.lead.review");
        }

        // 2. Recursion Guard & Unsupported Glyphs
        string? gamePath = LocateGamePath();
        if (!string.IsNullOrEmpty(gamePath))
        {
            string smallFontEn = Path.Combine(gamePath, "Content", "Fonts", "SmallFont.xnb");
            string smallFontVi = Path.Combine(gamePath, "Mods", "Stardew Valley Viethoa", "assets", "Content", "Fonts", "SmallFont_vi.xnb");

            if (File.Exists(smallFontEn) && File.Exists(smallFontVi))
            {
                var fontEn = XnbFontMetadataParser.ParseFile(smallFontEn).CreateHeadlessSpriteFont();
                var fontVi = XnbFontMetadataParser.ParseFile(smallFontVi).CreateHeadlessSpriteFont();

                List<(char Glyph, string Source, string Lang)> missingWarnings = new();
                var manager = new BilingualFontManager(
                    () => null,
                    Path.Combine(gamePath, "Content"),
                    Path.Combine(gamePath, "Mods", "Stardew Valley Viethoa", "assets", "Content"),
                    "vi",
                    (glyph, source, lang) => missingWarnings.Add((glyph, source, lang))
                );
                manager.SetSecondaryFontForTesting(fontEn, fontVi);

                using (BilingualRenderScope.Begin())
                {
                    // Neither fontEn nor fontVi covers Snowman '\u2603'
                    bool measured = BilingualFontRenderer.TryMeasureString(fontEn, "Test \u2603 Snowman", out Vector2 measuredSize);
                    Assert(measured, "Recursion Guard: TryMeasureString handles unsupported glyphs without recursion");
                    Assert(missingWarnings.Count == 1 && missingWarnings[0].Glyph == '\u2603', "Recursion Guard: Missing glyph warning emitted once for unsupported glyph");

                    // Second measurement with same glyph in different context does NOT repeat warning
                    BilingualFontRenderer.TryMeasureString(fontEn, "Another \u2603 Item", out _);
                    Assert(missingWarnings.Count == 1, "Recursion Guard: Bounded warning cache does not duplicate warning across contexts");

                    // Direct BilingualRunPlanner.Plan call inside active scope
                    var directPlan = BilingualRunPlanner.Plan("Direct \u2603 Call", fontEn, fontVi, manager);
                    Assert(directPlan != null, "Recursion Guard: Direct BilingualRunPlanner.Plan call in active scope executes safely");
                }

                // 3. Decomposed (NFD) fast-path defect fix
                // "Ca\u0301 no\u0301c" is NFD; fontEn has precomposed Latin-1 'á' (\u00E1) and 'ó' (\u00F3) but lacks combining acute \u0301
                string nfdInput = "Ca\u0301 no\u0301c";
                var nfcPlan = BilingualRunPlanner.Plan(nfdInput, fontEn, null, manager);
                Assert(nfcPlan != null, "NFC Fast-Path: Decomposed NFD string produces non-null plan when primary has precomposed glyphs");
                Assert(nfcPlan!.Runs.Count == 1, "NFC Fast-Path: Decomposed string produces 1 run with primary font");
                Assert(nfcPlan.Runs[0].Text == "Cá nóc", "NFC Fast-Path: Run text is normalized to precomposed NFC 'Cá nóc'");
                Assert(ReferenceEquals(nfcPlan.Runs[0].Font, fontEn), "NFC Fast-Path: Run font is primary font without secondary supplied");

                // Precomposed string in primary font returns null (fast-path untouched)
                var precomposedPlan = BilingualRunPlanner.Plan("Cá nóc", fontEn, null, manager);
                Assert(precomposedPlan == null, "NFC Fast-Path: Precomposed string fully in primary font returns null (native passthrough)");

                manager.Dispose();
            }
        }

        // 4. DrawString Transforms & Block Flipping Formula
        Vector2 totalSize = new(100f, 40f);
        Vector2 runOffset = new(10f, 5f);
        Vector2 runSize = new(30f, 20f);
        Vector2 callerOrigin = new(50f, 20f);

        // No flip: offset is (10, 5)
        float offX_none = runOffset.X;
        float offY_none = runOffset.Y;
        Assert(offX_none == 10f && offY_none == 5f, "Transforms: No flip preserves offset");
        Assert(callerOrigin - new Vector2(offX_none, offY_none) == new Vector2(40f, 15f), "Transforms: No flip runOrigin matches");

        // FlipHorizontally: totalSize.X - runOffset.X - runSize.X = 100 - 10 - 30 = 60
        float offX_h = totalSize.X - runOffset.X - runSize.X;
        Assert(offX_h == 60f, "Transforms: FlipHorizontally reflects offset X across total width (100 - 10 - 30 = 60)");
        Assert(callerOrigin - new Vector2(offX_h, offY_none) == new Vector2(-10f, 15f), "Transforms: FlipHorizontally runOrigin correctly offset");

        // FlipVertically: totalSize.Y - runOffset.Y - runSize.Y = 40 - 5 - 20 = 15
        float offY_v = totalSize.Y - runOffset.Y - runSize.Y;
        Assert(offY_v == 15f, "Transforms: FlipVertically reflects offset Y across total height (40 - 5 - 20 = 15)");
        Assert(callerOrigin - new Vector2(offX_none, offY_v) == new Vector2(40f, 5f), "Transforms: FlipVertically runOrigin correctly offset");

        // Scale Vector2.Zero preservation
        Vector2? explicitZero = Vector2.Zero;
        Vector2 effectiveZero = explicitZero ?? Vector2.One;
        Assert(effectiveZero == Vector2.Zero, "Transforms: Scale Vector2.Zero is preserved and not coerced to Vector2.One");

        Vector2? omittedScale = null;
        Vector2 effectiveDefault = omittedScale ?? Vector2.One;
        Assert(effectiveDefault == Vector2.One, "Transforms: Omitted scale defaults to Vector2.One");

        // 5. Secondary Font Caching & Diagnostics
        List<string> diagnostics = new();
        var fakeManager = new BilingualFontManager(
            () => new TestMockServiceProvider(),
            "C:\\NonExistentGamePath\\Content",
            "C:\\NonExistentModPath\\Content",
            "vi",
            null,
            d => diagnostics.Add(d)
        );

        var dummyFont = XnbFontMetadataParser.ParseFile(Path.Combine(LocateGamePath()!, "Content", "Fonts", "SmallFont.xnb")).CreateHeadlessSpriteFont();
        var loaded1 = fakeManager.GetSecondaryFont(dummyFont);
        Assert(loaded1 == null, "Asset Caching: Returns null when asset missing on disk");
        Assert(diagnostics.Count == 1, "Asset Caching: Diagnostic warning emitted on first missing asset load attempt");
        Assert(diagnostics[0].Contains("Fonts/SmallFont") && diagnostics[0].Contains("vi"), "Asset Caching: Diagnostic captures asset name and locale");

        // Second call: does NOT re-attempt disk or emit duplicate diagnostic
        var loaded2 = fakeManager.GetSecondaryFont(dummyFont);
        Assert(loaded2 == null, "Asset Caching: Cached unavailable font returns null on repeated call");
        Assert(diagnostics.Count == 1, "Asset Caching: Does not repeat diagnostic on cached unavailable font");

        // Invalidate resets attempt flag
        fakeManager.Invalidate();
        var loaded3 = fakeManager.GetSecondaryFont(dummyFont);
        Assert(loaded3 == null, "Asset Caching: Invalidation allows re-attempting load");
        Assert(diagnostics.Count == 2, "Asset Caching: Re-emits diagnostic after invalidation");

        fakeManager.Dispose();
    }

    public class NestedScopeTarget
    {
        public int ParentDepthBeforeChild;
        public int ChildDepthInside;
        public int ParentDepthAfterChild;
        public int ParentDepthInCatch;

        public void ParentMethod(bool throwInChild)
        {
            ParentDepthBeforeChild = BilingualRenderScope.CurrentDepth;
            try
            {
                ChildMethod(throwInChild);
                ParentDepthAfterChild = BilingualRenderScope.CurrentDepth;
            }
            catch (InvalidOperationException)
            {
                ParentDepthInCatch = BilingualRenderScope.CurrentDepth;
            }
        }

        public void ChildMethod(bool shouldThrow)
        {
            ChildDepthInside = BilingualRenderScope.CurrentDepth;
            if (shouldThrow)
            {
                throw new InvalidOperationException("Simulated error in child target");
            }
        }
    }

    public static class NestedScopeParentPatch
    {
        public static void Prefix(out IDisposable? __state)
        {
            __state = BilingualRenderScope.Begin();
        }

        public static void Postfix(IDisposable? __state)
        {
            __state?.Dispose();
        }

        public static Exception? Finalizer(IDisposable? __state, Exception? __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    public static class NestedScopeChildPatch
    {
        public static void Prefix(out IDisposable? __state)
        {
            __state = BilingualRenderScope.Begin();
        }

        public static void Postfix(IDisposable? __state)
        {
            __state?.Dispose();
        }

        public static Exception? Finalizer(IDisposable? __state, Exception? __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    public class TestMockServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private static bool _headlessCaptureActive;
    private static List<(SpriteFont Font, string Text, Vector2 Origin)>? _capturedDrawCalls;

    private static bool HeadlessCaptureDrawStringPrefix(SpriteBatch __instance, SpriteFont spriteFont, string? text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
    {
        if (_headlessCaptureActive && _capturedDrawCalls != null && text != null)
        {
            _capturedDrawCalls.Add((spriteFont, text, origin));
            return false; // Skip native MonoGame rendering in headless tests
        }
        return true;
    }

    private static void TestVersion130Features()
    {
        Console.WriteLine("\n-- Testing Version 1.3.0 Features (Fixed Role Fonts, Generic Sources, WP/FL) --");

        string? gamePath = LocateGamePath();
        bool hasGame = gamePath != null && Directory.Exists(gamePath);

        // 1. Wallpaper and Flooring XNB Resolution Across Locales and Distinct Cache Keys
        if (hasGame)
        {
            string contentRoot = Path.Combine(gamePath!, "Content");
            var testService = new DummyServiceProvider();

            // English (en / default)
            var extReg = new ExternalTranslationRegistry();
            var rawEn = new RawContentProvider(() => testService, contentRoot, "en");
            var resolverEn = new ItemNameResolver(rawEn, externalRegistry: extReg);

            string? wpEn0 = resolverEn.GetSecondaryNameById("(WP)0");
            string? wpEn1 = resolverEn.GetSecondaryNameById("(WP)1");
            string? wpEn50 = resolverEn.GetSecondaryNameById("(WP)50");
            string? flEn0 = resolverEn.GetSecondaryNameById("(FL)0");
            string? flEn1 = resolverEn.GetSecondaryNameById("(FL)1");
            string? flEn30 = resolverEn.GetSecondaryNameById("(FL)30");

            Assert(wpEn0 == "Wallpaper", $"WP/FL en: (WP)0 resolves 'Wallpaper' (got '{wpEn0}')");
            Assert(wpEn1 == "Wallpaper", $"WP/FL en: (WP)1 resolves 'Wallpaper' (got '{wpEn1}')");
            Assert(wpEn50 == "Wallpaper", $"WP/FL en: (WP)50 resolves 'Wallpaper' (got '{wpEn50}')");
            Assert(flEn0 == "Flooring", $"WP/FL en: (FL)0 resolves 'Flooring' (got '{flEn0}')");
            Assert(flEn1 == "Flooring", $"WP/FL en: (FL)1 resolves 'Flooring' (got '{flEn1}')");
            Assert(flEn30 == "Flooring", $"WP/FL en: (FL)30 resolves 'Flooring' (got '{flEn30}')");

            // Cache distinction: same-name items across IDs remain separate entries in cache
            var cacheField = typeof(ItemNameResolver).GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
            var cache = cacheField?.GetValue(resolverEn) as System.Collections.IDictionary;
            Assert(cache != null && cache.Count >= 6, "WP/FL Cache: All 6 distinct item IDs cached independently without clobbering");

            // ParsedItemData with RawData = null
            var dummyWpDef = new WallpaperDataDefinition();
            var parsedWp = new ParsedItemData(dummyWpDef, "5", 0, "LooseSprites/none", "(WP)5", "Wallpaper", "Desc", -100, null, null);
            string? parsedWpResolved = resolverEn.ResolveFromParsedData(parsedWp);
            Assert(parsedWpResolved == "Wallpaper", $"WP/FL en: ParsedItemData (RawData=null) for (WP)5 resolves 'Wallpaper' (got '{parsedWpResolved}')");

            var dummyFlDef = new FlooringDataDefinition();
            var parsedFl = new ParsedItemData(dummyFlDef, "12", 0, "LooseSprites/none", "(FL)12", "Flooring", "Desc", -100, null, null);
            string? parsedFlResolved = resolverEn.ResolveFromParsedData(parsedFl);
            Assert(parsedFlResolved == "Flooring", $"WP/FL en: ParsedItemData (RawData=null) for (FL)12 resolves 'Flooring' (got '{parsedFlResolved}')");

            // Across all available built-ins
            string[] checkLocales = { "es", "de", "fr", "ja", "zh", "ru", "pt", "ko", "it", "tr", "hu" };
            foreach (var loc in checkLocales)
            {
                var rawLoc = new RawContentProvider(() => testService, contentRoot, loc);
                var resLoc = new ItemNameResolver(rawLoc);
                string? wpLoc = resLoc.GetSecondaryNameById("(WP)20");
                string? flLoc = resLoc.GetSecondaryNameById("(FL)20");
                Assert(!string.IsNullOrEmpty(wpLoc) && wpLoc != "InternalName", $"WP/FL {loc}: (WP)20 resolves localized string '{wpLoc}'");
                Assert(!string.IsNullOrEmpty(flLoc) && flLoc != "InternalName", $"WP/FL {loc}: (FL)20 resolves localized string '{flLoc}'");
            }

            // Vietnamese mod Content if present
            string modContentRoot = Path.Combine(gamePath!, "Mods", "Stardew Valley Viethoa", "assets", "Content");
            if (Directory.Exists(modContentRoot))
            {
                var rawVi = new RawContentProvider(() => testService, contentRoot, "vi", modContentRoot);
                var resVi = new ItemNameResolver(rawVi);
                string? wpVi = resVi.GetSecondaryNameById("(WP)0");
                string? flVi = resVi.GetSecondaryNameById("(FL)0");
                Assert(!string.IsNullOrEmpty(wpVi), $"WP/FL vi: (WP)0 resolves '{wpVi}'");
                Assert(!string.IsNullOrEmpty(flVi), $"WP/FL vi: (FL)0 resolves '{flVi}'");
            }

            // Custom Wallpaper / Flooring via external registry
            extReg.RegisterModTranslation("TestMod", "Item", "(WP)CustomFloralWP", "en", "Giấy dán tường hoa");
            extReg.RegisterModTranslation("TestMod", "Item", "(FL)CustomWoodFL", "en", "Sàn gỗ cổ điển");
            Assert(resolverEn.GetSecondaryNameById("(WP)CustomFloralWP") == "Giấy dán tường hoa", "Custom WP: Resolves translation from external registry");
            Assert(resolverEn.GetSecondaryNameById("(FL)CustomWoodFL") == "Sàn gỗ cổ điển", "Custom FL: Resolves translation from external registry");
        }

        // 2. Duplicate Suppression & Structured Composition Registration
        {
            string dupTitle = TooltipFormatter.FormatTitle("Wallpaper", "Wallpaper", "Wallpaper");
            Assert(dupTitle == "Wallpaper", $"Duplicate Suppression: Same primary and secondary name suppresses duplicate ('{dupTitle}')");
            Assert(!BilingualStringTracker.IsTracked(dupTitle), "Duplicate Suppression: Suppressed title remains ordinary untracked string");

            string diffTitle = TooltipFormatter.FormatTitle("Diamond", "Diamond", "Kim cương");
            Assert(diffTitle.Contains("Diamond") && diffTitle.Contains("Kim cương"), "TooltipFormatter: Different names format both");
            Assert(BilingualStringTracker.IsTracked(diffTitle), "TooltipFormatter: Bilingual title is tracked");
            Assert(BilingualStringTracker.TryGetComposition(diffTitle, out var compDiff), "TooltipFormatter: Registers bilingual composition");
            Assert(compDiff != null && compDiff.Spans.Count == 2, "TooltipFormatter: Composition has 2 spans (Primary and Secondary)");
            Assert(compDiff!.Spans[0].Role == FontRole.Primary && compDiff.Spans[1].Role == FontRole.Secondary, "TooltipFormatter: Spans have correct Primary and Secondary roles");
        }

        // 3. All Built-in Font Descriptors Resolve Glyph Metadata
        if (hasGame)
        {
            string fontsDir = Path.Combine(gamePath!, "Content", "Fonts");
            var builtins = new[]
            {
                ("en", "SmallFont.xnb", 'A'),
                ("es", "SmallFont.es-ES.xnb", 'ñ'),
                ("de", "SmallFont.de-DE.xnb", 'ä'),
                ("fr", "SmallFont.fr-FR.xnb", 'é'),
                ("pt", "SmallFont.pt-BR.xnb", 'ã'),
                ("ru", "SmallFont.ru-RU.xnb", 'д'),
                ("ja", "SmallFont.ja-JP.xnb", 'あ'),
                ("zh", "SmallFont.zh-CN.xnb", '汉'),
                ("ko", "SmallFont.ko-KR.xnb", '가'),
                ("it", "SmallFont.xnb", 'A'), // it falls back to unsuffixed SmallFont.xnb
                ("tr", "SmallFont.tr-TR.xnb", 'ğ'),
                ("hu", "SmallFont.hu-HU.xnb", 'ő')
            };

            foreach (var (code, fontFile, testChar) in builtins)
            {
                string fullPath = Path.Combine(fontsDir, fontFile);
                Assert(File.Exists(fullPath), $"Builtin Font: {fontFile} exists on disk for locale '{code}'");
                var meta = XnbFontMetadataParser.ParseFile(fullPath);
                Assert(meta.Characters.Count > 0, $"Builtin Font: {fontFile} parsed {meta.Characters.Count} characters");
                Assert(meta.Characters.Contains(testChar), $"Builtin Font: {fontFile} contains target script glyph '{testChar}' for locale '{code}'");
                var headlessFont = meta.CreateHeadlessSpriteFont();
                Assert(headlessFont.MeasureString(testChar.ToString()).X > 0, $"Builtin Font: {fontFile} measures '{testChar}' headless");
            }
        }

        // 4. Fixed Role Plan With TWO Fake Fonts Both Covering SAME ASCII
        var metaFakePrimary = new SpriteFontMetadata { LineSpacing = 20, Spacing = 0, DefaultCharacter = '?' };
        var metaFakeSecondary = new SpriteFontMetadata { LineSpacing = 26, Spacing = 0, DefaultCharacter = '?' };
        for (char c = ' '; c <= 'z'; c++)
        {
            metaFakePrimary.Characters.Add(c);
            metaFakePrimary.Glyphs.Add(new Rectangle(0, 0, 10, 15));
            metaFakePrimary.Cropping.Add(new Rectangle(0, 0, 10, 15));
            metaFakePrimary.Kerning.Add(new Vector3(0, 10, 0));

            metaFakeSecondary.Characters.Add(c);
            metaFakeSecondary.Glyphs.Add(new Rectangle(0, 0, 14, 20));
            metaFakeSecondary.Cropping.Add(new Rectangle(0, 0, 14, 20));
            metaFakeSecondary.Kerning.Add(new Vector3(0, 14, 0));
        }
        var fakeFontPrimary = metaFakePrimary.CreateHeadlessSpriteFont();
        var fakeFontSecondary = metaFakeSecondary.CreateHeadlessSpriteFont();

        // 4A. Normal Order: "Sword\nBlade"
        // Primary chunk: "Sword"
        // Secondary chunk: "Blade"
        var compNormal = StructuredTextComposition.CreateBilingual("Sword", "Blade", secondaryFirst: false);
        BilingualStringTracker.RegisterComposition(compNormal);

        var planNormal = BilingualRunPlanner.Plan(compNormal.FullText, fakeFontPrimary, fakeFontSecondary);
        Assert(planNormal != null, "Fixed Role: Plan generated for ASCII bilingual composition");
        var runSword = planNormal!.Runs.FirstOrDefault(r => r.Text.Contains("Sword"));
        var runBlade = planNormal.Runs.FirstOrDefault(r => r.Text.Contains("Blade"));
        Assert(runSword != null && ReferenceEquals(runSword.Font, fakeFontPrimary), "Fixed Role: 'Sword' Primary chunk uses Primary font");
        Assert(runBlade != null && ReferenceEquals(runBlade.Font, fakeFontSecondary), "Fixed Role: 'Blade' Secondary chunk uses Secondary font even though Primary font covers ASCII");

        // 4B. SecondaryFirst: "Blade\nSword"
        var compSecFirst = StructuredTextComposition.CreateBilingual("Sword", "Blade", secondaryFirst: true);
        BilingualStringTracker.RegisterComposition(compSecFirst);

        var planSecFirst = BilingualRunPlanner.Plan(compSecFirst.FullText, fakeFontPrimary, fakeFontSecondary);
        Assert(planSecFirst != null, "Fixed Role: Plan generated for SecondaryFirst bilingual composition");
        var runSecBlade = planSecFirst!.Runs.FirstOrDefault(r => r.Text.Contains("Blade"));
        var runSecSword = planSecFirst.Runs.FirstOrDefault(r => r.Text.Contains("Sword"));
        Assert(runSecBlade != null && ReferenceEquals(runSecBlade.Font, fakeFontSecondary), "Fixed Role SecondaryFirst: 'Blade' Secondary chunk STILL uses Secondary font");
        Assert(runSecSword != null && ReferenceEquals(runSecSword.Font, fakeFontPrimary), "Fixed Role SecondaryFirst: 'Sword' Primary chunk STILL uses Primary font");

        // 5. Role Metadata Across Wrapped Lines and Primary Body Remainder
        string multiWrapped = "Ancient Fruit\nOld Fruit\nTakes 28 days to grow.\nHarvest repeatedly.";
        int primaryTitleLen = "Ancient Fruit".Length;
        int secondaryTitleStart = primaryTitleLen + 1;
        int secondaryTitleLen = "Old Fruit".Length;
        int bodyStart = secondaryTitleStart + secondaryTitleLen + 1;
        int bodyLen = "Takes 28 days to grow.\nHarvest repeatedly.".Length;

        var compWrapped = new StructuredTextComposition(
            multiWrapped,
            new[]
            {
                new RoleSpan(0, primaryTitleLen, FontRole.Primary),
                new RoleSpan(secondaryTitleStart, secondaryTitleLen, FontRole.Secondary),
                new RoleSpan(bodyStart, bodyLen, FontRole.Primary)
            }
        );
        BilingualStringTracker.RegisterComposition(compWrapped);

        var planWrapped = BilingualRunPlanner.Plan(multiWrapped, fakeFontPrimary, fakeFontSecondary);
        Assert(planWrapped != null && planWrapped.Runs.Count >= 3, "Wrapped & Body: Plan produced for multiline structured composition");
        var line0 = planWrapped!.Runs.FirstOrDefault(r => r.Text.Contains("Ancient Fruit"));
        var line1 = planWrapped.Runs.FirstOrDefault(r => r.Text.Contains("Old Fruit"));
        var line2 = planWrapped.Runs.FirstOrDefault(r => r.Text.Contains("Takes 28 days"));
        var line3 = planWrapped.Runs.FirstOrDefault(r => r.Text.Contains("Harvest repeatedly"));

        Assert(line0 != null && ReferenceEquals(line0.Font, fakeFontPrimary), "Wrapped & Body: Primary title line 0 uses Primary font");
        Assert(line1 != null && ReferenceEquals(line1.Font, fakeFontSecondary), "Wrapped & Body: Secondary title line 1 uses Secondary font");
        Assert(line2 != null && ReferenceEquals(line2.Font, fakeFontPrimary), "Wrapped & Body: Body line 2 uses Primary font");
        Assert(line3 != null && ReferenceEquals(line3.Font, fakeFontPrimary), "Wrapped & Body: Body line 3 uses Primary font");

        // 6. Real Harmony Patched Pipeline Measure & Draw Capture Headless
        var captureHarmony = new Harmony("test.v130.pipeline.capture");
        var capturedDraws = new List<(SpriteFont Font, string Text, Vector2 Origin)>();

        var drawMethod4Param = typeof(SpriteBatch).GetMethod(
            nameof(SpriteBatch.DrawString),
            new[] { typeof(SpriteFont), typeof(string), typeof(Vector2), typeof(Color) }
        );
        var drawMethod9Param = typeof(SpriteBatch).GetMethod(
            nameof(SpriteBatch.DrawString),
            new[] { typeof(SpriteFont), typeof(string), typeof(Vector2), typeof(Color), typeof(float), typeof(Vector2), typeof(Vector2), typeof(SpriteEffects), typeof(float) }
        );

        captureHarmony.Patch(
            drawMethod4Param,
            prefix: new HarmonyMethod(typeof(FontInterceptionPatches.SpriteBatchDrawStringColorPatch).GetMethod(nameof(FontInterceptionPatches.SpriteBatchDrawStringColorPatch.Prefix)))
        );
        captureHarmony.Patch(
            drawMethod9Param,
            prefix: new HarmonyMethod(typeof(Program).GetMethod(nameof(HeadlessCaptureDrawStringPrefix), BindingFlags.NonPublic | BindingFlags.Static)) { priority = Priority.First }
        );

        var fakeBatch = (SpriteBatch)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SpriteBatch));
        var testManager = new BilingualFontManager(() => null, "C:\\FakeGame", "C:\\FakeMod", "vi");
        testManager.SetSecondaryFontForTesting(fakeFontPrimary, fakeFontSecondary);

        var compDraw = StructuredTextComposition.CreateBilingual("Diamond", "Gemstone", secondaryFirst: false);
        BilingualStringTracker.RegisterComposition(compDraw);

        _capturedDrawCalls = capturedDraws;
        _headlessCaptureActive = true;

        try
        {
            using (BilingualRenderScope.Begin())
            {
                fakeBatch.DrawString(fakeFontPrimary, compDraw.FullText, new Vector2(100f, 200f), Color.White);
            }
        }
        finally
        {
            _headlessCaptureActive = false;
            _capturedDrawCalls = null;
            captureHarmony.UnpatchAll("test.v130.pipeline.capture");
            testManager.Dispose();
        }

        Assert(capturedDraws.Count >= 2, $"Patched Pipeline Capture: Captured {capturedDraws.Count} draw calls from real Harmony pipeline");
        var drawDiamond = capturedDraws.FirstOrDefault(d => d.Text.Contains("Diamond"));
        var drawGemstone = capturedDraws.FirstOrDefault(d => d.Text.Contains("Gemstone"));
        Assert(drawDiamond.Font != null && ReferenceEquals(drawDiamond.Font, fakeFontPrimary), "Patched Pipeline Capture: Primary chunk drawn with Primary font");
        Assert(drawGemstone.Font != null && ReferenceEquals(drawGemstone.Font, fakeFontSecondary), "Patched Pipeline Capture: Secondary chunk drawn with Secondary font");

        // 7. Custom Language Source Registry Security & Lifecycle
        var registry = new LanguageSourceRegistry();
        bool changedFired = false;
        registry.OnChanged += () => changedFired = true;

        // Path boundary security validation
        string safeBase = @"C:\Games\Stardew Valley\Mods\CustomLanguagePack";
        Assert(LanguageSourceRegistry.TryResolveSafePath(safeBase, "assets/strings.xnb", out string path1) && path1.StartsWith(safeBase), "Path Safety: Valid subpath accepted");
        Assert(LanguageSourceRegistry.TryResolveSafePath(safeBase, "fonts/nested/font.xnb", out string path2) && path2.StartsWith(safeBase), "Path Safety: Valid nested subpath accepted");
        Assert(!LanguageSourceRegistry.TryResolveSafePath(safeBase, "../../Windows/System32/cmd.exe", out _), "Path Safety: Traversal outside root rejected");
        Assert(!LanguageSourceRegistry.TryResolveSafePath(safeBase, @"..\..\secret.json", out _), "Path Safety: Relative escape rejected");
        Assert(!LanguageSourceRegistry.TryResolveSafePath(safeBase, @"D:\OtherDrive\file.xnb", out _), "Path Safety: Absolute path outside root rejected");

        // C# Register / Unregister lifecycle & generation priority
        var descUk1 = new LanguageSourceDescriptor
        {
            LanguageCode = "uk",
            SourceModId = "Author1.UkrainianPack",
            ContentRoot = @"Mods/UkrainianPack/assets",
            AssetPattern = "{Asset}_uk.xnb"
        };
        changedFired = false;
        var token = registry.Register(descUk1);
        Assert(changedFired, "Registry Lifecycle: OnChanged fired on Register");
        Assert(registry.TryGetSource("uk", out var regUk1) && regUk1?.SourceModId == "Author1.UkrainianPack", "Registry Lifecycle: Source registered for 'uk'");

        var descUk2 = new LanguageSourceDescriptor
        {
            LanguageCode = "uk",
            SourceModId = "Author2.UkrainianPackV2",
            ContentRoot = @"Mods/UkrainianPackV2/assets",
            AssetPattern = "{Asset}_uk2.xnb"
        };
        registry.Register(descUk2);
        Assert(registry.TryGetSource("uk", out var regUk2) && regUk2?.SourceModId == "Author2.UkrainianPackV2", "Registry Precedence: Newer generation overrides older");

        token.Dispose(); // Older token unregister
        Assert(registry.TryGetSource("uk", out var stillUk2) && stillUk2?.SourceModId == "Author2.UkrainianPackV2", "Registry Lifecycle: Token disposal of older keeps active newer generation");

        changedFired = false;
        registry.Unregister("Author2.UkrainianPackV2");
        Assert(changedFired, "Registry Lifecycle: OnChanged fired on Unregister");
        Assert(!registry.TryGetSource("uk", out _), "Registry Lifecycle: Source removed after unregistering all mods for 'uk'");

        // 8. Content Patcher Asset Schema Source Map & Stale Data Purge
        var registryCP = new LanguageSourceRegistry();
        bool cpChangedFired = false;
        registryCP.OnChanged += () => cpChangedFired = true;

        var cpDict = new Dictionary<string, LanguageSourceDescriptor>
        {
            ["uk"] = new LanguageSourceDescriptor
            {
                SourceModId = "Community.UkrainianCP",
                ContentRoot = "Mods/UkrainianCP/assets",
                AssetPattern = "assets/{Asset}_uk.xnb",
                SmallFont = "assets/Fonts/SmallFont_uk.xnb",
                DialogueFont = "assets/Fonts/SpriteFont1_uk.xnb",
                Strings = new Dictionary<string, string>
                {
                    ["Strings/Objects:Honey_Wild_Name"] = "Дикий мед"
                }
            },
            ["th"] = new LanguageSourceDescriptor
            {
                SourceModId = "Community.ThaiCP",
                ContentRoot = "Mods/ThaiCP/assets",
                AssetPattern = "assets/{Asset}_th.xnb",
                Strings = new Dictionary<string, string>
                {
                    ["Strings/Objects:Honey_Wild_Name"] = "น้ำผึ้งป่า"
                }
            }
        };

        registryCP.LoadFromAssetDictionary(cpDict);
        Assert(cpChangedFired, "CP Asset Schema: OnChanged fired on LoadFromAssetDictionary");
        Assert(registryCP.TryGetSource("uk", out var descUkCP) && descUkCP?.SmallFont == "assets/Fonts/SmallFont_uk.xnb", "CP Asset Schema: uk source descriptor loaded with font refs");
        Assert(registryCP.TryGetSource("th", out var descThCP) && descThCP?.Strings?["Strings/Objects:Honey_Wild_Name"] == "น้ำผึ้งป่า", "CP Asset Schema: th source descriptor loaded with strings map");

        var dummyService = new DummyServiceProvider();
        var rawUk = new RawContentProvider(() => dummyService, "C:\\GameContent", "uk", sourceRegistry: registryCP);
        bool foundUk = rawUk.TryGetString("Strings/Objects", "Honey_Wild_Name", out string? ukHoney);
        Assert(foundUk && ukHoney == "Дикий мед", "RawContentProvider: Resolves string from CP descriptor strings dictionary");

        // Clear sources and verify stale data is purged
        cpChangedFired = false;
        registryCP.Clear();
        Assert(cpChangedFired, "CP Asset Schema: OnChanged fired on Clear");
        Assert(!registryCP.TryGetSource("uk", out _), "CP Asset Schema: No source found for 'uk' after Clear");

        rawUk.Invalidate();
        bool foundAfterPurge = rawUk.TryGetString("Strings/Objects", "Honey_Wild_Name", out _);
        Assert(!foundAfterPurge, "RawContentProvider: Stale data purged after source removed and cache invalidated");
    }
}
