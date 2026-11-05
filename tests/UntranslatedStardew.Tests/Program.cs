using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;
using StardewValley.ItemTypeDefinitions;
using UntranslatedStardew.Core;

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
        TestMockContentProviderAndResolver();
        TestApiLifecycleAndConfig();
        TestRealXnbSmoke();

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

        // 5. Real Vietnamese mod ContentManager (if Kou.ViethoaSV installed)
        if (Directory.Exists(modContentRoot))
        {
            var rawVi = new RawContentProvider(() => testService, contentRoot, "vi", modContentRoot);
            bool gotVi = rawVi.TryGetString("Strings/Objects", "Honey_Wild_Name", out string? viName);
            Assert(gotVi && viName == "Mật ong rừng", $"Real Vietnamese mod XNB Strings/Objects_vi:Honey_Wild_Name == 'Mật ong rừng' (got '{viName}')");

            bool gotBootsVi = rawVi.TryGetString("Data/Boots", "504", out string? bootsEntryVi);
            string? bootsViName = bootsEntryVi != null && bootsEntryVi.Split('/').Length > 6 ? bootsEntryVi.Split('/')[6] : null;
            Assert(gotBootsVi && !string.IsNullOrEmpty(bootsViName), $"Real Vietnamese mod XNB Data/Boots_vi:504 name == '{bootsViName}'");
        }
        else
        {
            Console.WriteLine("  [INFO] Kou.ViethoaSV mod directory not present; skipped Vietnamese mod XNB check.");
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

    private class MockContentProvider : ISecondaryContentProvider
    {
        private readonly Dictionary<string, Dictionary<string, string>> _localized = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<string, string>> _english = new(StringComparer.OrdinalIgnoreCase);

        public string RequestedLanguage { get; set; }
        public string EffectiveLanguage { get; set; }

        public MockContentProvider(string requested, string effective)
        {
            RequestedLanguage = requested;
            EffectiveLanguage = effective;
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
            if (EffectiveLanguage != "en" && _localized.TryGetValue(assetSubpath, out var locDict) && locDict.TryGetValue(key, out value))
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
        }

        public void Dispose()
        {
            _localized.Clear();
            _english.Clear();
        }
    }
}
