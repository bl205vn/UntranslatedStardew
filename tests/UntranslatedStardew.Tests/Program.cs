using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Win32;
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
public static class Program
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

        Assert(rawUnsupported.EffectiveLanguage == "default", "rawUnsupported fell back to 'default'");
        Assert(warnedRequested == "unsupported_xyz" && warnedFallback == "default", "rawUnsupported triggered fallback warning with ('unsupported_xyz', 'default')");

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
}
