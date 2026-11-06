using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using UntranslatedStardew.Core;
using UntranslatedStardew.Patches;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static SpriteFont MakeRoleFont(int advance, int spacing)
    {
        var meta = new SpriteFontMetadata { LineSpacing = spacing, DefaultCharacter = '?' };
        foreach (char c in (Enumerable.Range(32, 95).Select(i => (char)i).Concat("áé中日韓")).Distinct().OrderBy(c => c))
        {
            meta.Characters.Add(c);
            meta.Glyphs.Add(new Rectangle(0, 0, advance, spacing));
            meta.Cropping.Add(new Rectangle(0, 0, advance, spacing));
            meta.Kerning.Add(new Vector3(0, advance, 0));
        }
        return meta.CreateHeadlessSpriteFont();
    }

    private static void TestRoleFontAcceptance()
    {
        Console.WriteLine("\n-- Role fonts: NFC, real wrap/measure, source lifecycle --");
        var primary = MakeRoleFont(7, 15);
        var secondary = MakeRoleFont(14, 27);
        using (var manager = new BilingualFontManager(() => null, "", null, "en"))
        {
            manager.SetSecondaryFontForTesting(primary, secondary);
            foreach (bool first in new[] { false, true })
            {
                var composition = StructuredTextComposition.CreateBilingual("Ca\u0301", "Blade", first, body: "Body");
                BilingualStringTracker.RegisterComposition(composition);
                var plan = BilingualRunPlanner.Plan(composition.FullText, primary, secondary);
                Assert(plan != null && plan.Runs.Any(r => r.Text == "Cá" && ReferenceEquals(r.Font, primary)), $"NFC primary offsets correct, SecondaryFirst={first}");
                Assert(plan != null && plan.Runs.Any(r => r.Text == "Blade" && ReferenceEquals(r.Font, secondary)), $"ASCII secondary fixed font, SecondaryFirst={first}");
                Assert(plan != null && plan.Runs.Any(r => r.Text == "Body" && ReferenceEquals(r.Font, primary)), "Merged body retains primary font");
            }
            var rawComposition = new StructuredTextComposition("Ca\u0301\nBlade", new[] { new RoleSpan(4, 5, FontRole.Secondary) });
            Assert(rawComposition.FullText == "Cá\nBlade" && rawComposition.GetRoleAt(3) == FontRole.Secondary, "Explicit NFD spans remapped to NFC offsets");
            BilingualStringTracker.RegisterRole("Same", FontRole.Secondary);
            Assert(!BilingualStringTracker.TryGetComposition("Same", out _), "Single role never leaks into persistent text dictionary");

            var harmony = new Harmony("test.role.scope.measure.acceptance");
            harmony.Patch(typeof(SpriteFont).GetMethod(nameof(SpriteFont.MeasureString), new[] { typeof(string) }),
                prefix: new HarmonyMethod(typeof(FontInterceptionPatches.SpriteFontMeasureStringPatch), nameof(FontInterceptionPatches.SpriteFontMeasureStringPatch.Prefix)));
            try
            {
                // Reflection enters the patched method itself; direct calls in this already-JITted test
                // may have been inlined by MonoGame before Harmony applied the patch.
                float Measured() => ((Vector2)typeof(SpriteFont).GetMethod(nameof(SpriteFont.MeasureString), new[] { typeof(string) })!.Invoke(primary, new object[] { "Same" })!).X;
                float native = Measured();
                float expectedSecondary = secondary.MeasureString("Same").X;
                using (BilingualRenderScope.Begin(FontRole.Secondary))
                {
                    Assert(Measured() == expectedSecondary, "Real Harmony MeasureString honors explicit secondary for ASCII");
                    using (BilingualRenderScope.Begin())
                        Assert(Measured() == expectedSecondary, "Nested ordinary scope inherits secondary role");
                    using (BilingualRenderScope.Begin(FontRole.Primary))
                        Assert(Measured() == native, "Nested primary override restores original metrics for identical text");
                }
                Assert(Measured() == native && !BilingualRenderScope.IsActive, "Identical text outside scope keeps original font");
                try { using (BilingualRenderScope.Begin(FontRole.Secondary)) throw new InvalidOperationException(); }
                catch (InvalidOperationException) { }
                Assert(!BilingualRenderScope.IsActive && BilingualRenderScope.ActiveRole == null, "Throwing role scope restores context");

                string text = "Blade Blade Blade";
                string wrappedP = RoleTextWrapper.WrapForRole(text, primary, 90, FontRole.Primary);
                string wrappedS = RoleTextWrapper.WrapForRole(text, primary, 90, FontRole.Secondary);
                Assert(wrappedS.Split('\n').Length > wrappedP.Split('\n').Length, "Actual collection wrapper uses secondary width rather than primary width");
                Assert(wrappedS.Split('\n').All(line => secondary.MeasureString(line).X <= 90), "Wrapped secondary lines fit measured secondary font");
                string cjk = RoleTextWrapper.WrapForRole("中日韓中日韓中日韓", primary, 45, FontRole.Secondary);
                Assert(cjk.Replace("\n", "") == "中日韓中日韓中日韓" && cjk.Split('\n').All(line => secondary.MeasureString(line).X <= 45), "CJK wrapping independent of selected primary language");
            }
            finally { harmony.UnpatchAll(harmony.Id); }
        }

        var registry = new LanguageSourceRegistry();
        using var provider = new RawContentProvider(() => null, "", "xx-test", sourceRegistry: registry);
        using var resolver = new ItemNameResolver(provider);
        using var fonts = new BilingualFontManager(() => null, "", null, "xx-test", sourceRegistry: registry);
        Assert(!provider.IsSecondaryAvailable && resolver.GetSecondarySideTabName(7) == null, "Unknown custom locale does not emit English side-tab labels");
        var descriptor = new LanguageSourceDescriptor { SourceModId = "Owner.Test", LanguageCode = "xx-test",
            Strings = new() { ["Strings/StringsFromCSFiles:Wallpaper.cs.13204"] = "Paper Test" }, SmallFont = "Fonts/Test" };
        using var token = registry.Register(descriptor);
        Assert(provider.IsSecondaryAvailable && fonts.IsSecondaryAvailable && fonts.EffectiveLanguage == "xx-test", "Late source activates existing content and font managers without recreating them");
        Assert(resolver.GetSecondaryNameById("(WP)71") == "Paper Test", "Existing resolver reads late registered custom wallpaper source");
        descriptor.Strings!["Strings/StringsFromCSFiles:Wallpaper.cs.13204"] = "Mutated";
        registry.TryGetSource("xx-test", out var copy);
        copy!.Strings!["Strings/StringsFromCSFiles:Wallpaper.cs.13204"] = "Mutated Again";
        Assert(provider.GetString("Strings/StringsFromCSFiles", "Wallpaper.cs.13204") == "Paper Test", "Registry input/output snapshots cannot be mutated externally");
        registry.LoadFromAssetDictionary(new Dictionary<string, LanguageSourceDescriptor> { ["xx-test"] = new() { SourceModId = "Owner.Test", Strings = new() { ["Wallpaper.cs.13204"] = "CP Paper" } } });
        Assert(resolver.GetSecondaryNameById("(WP)71") == "CP Paper", "CP snapshot refresh invalidates actual item resolver cache");
        registry.LoadFromAssetDictionary(new Dictionary<string, LanguageSourceDescriptor>());
        Assert(resolver.GetSecondaryNameById("(WP)71") == "Paper Test", "Removed CP snapshot restores API entry with same owner");
        registry.LoadFromAdditionalLanguages(new[] { new LanguageSourceDescriptor { SourceModId = "Owner.Test", LanguageCode = "xx-test", Strings = new() { ["Wallpaper.cs.13204"] = "Additional" } } });
        registry.LoadFromAdditionalLanguages(Array.Empty<LanguageSourceDescriptor>());
        Assert(resolver.GetSecondaryNameById("(WP)71") == "Paper Test", "AdditionalLanguages snapshot removal preserves API registration");
        token.Dispose();
        Assert(!provider.IsSecondaryAvailable && !fonts.IsSecondaryAvailable && resolver.GetSecondaryNameById("(WP)71") == null, "Source removal disables stale content and fonts, including cached item names");

        string? game = LocateGamePath();
        if (game != null)
        {
            var customRegistry = new LanguageSourceRegistry();
            using var customProvider = new RawContentProvider(() => new TestMockServiceProvider(), Path.Combine(game, "Content"), "xx-fr", sourceRegistry: customRegistry);
            using var customNames = new ItemNameResolver(customProvider);
            var api = new UntranslatedStardewApi(() => customNames, () => new ModConfig { SecondaryLanguage = "xx-fr" }, getLanguageSourceRegistry: () => customRegistry);
            using var customToken = api.RegisterLanguageSource("Example.Custom", "xx-fr", Path.Combine(game, "Content"), "{asset}.fr-FR", "Fonts/SmallFont", "Fonts/SpriteFont1");
            using var frenchProvider = new RawContentProvider(() => new TestMockServiceProvider(), Path.Combine(game, "Content"), "fr");
            using var frenchNames = new ItemNameResolver(frenchProvider);
            Assert(customProvider.IsSecondaryAvailable && customNames.GetSecondaryNameById("(WP)31") == frenchNames.GetSecondaryNameById("(WP)31"), "Public C# source API reads real custom-pattern XNBs without Vietnamese adapter");
            Assert(customNames.GetSecondarySkillName(0) == frenchNames.GetSecondarySkillName(0) && customNames.GetSecondarySideTabName(7) == frenchNames.GetSecondarySideTabName(7), "Generic custom pattern feeds skill and collection resolvers");
            customToken.Dispose();
            Assert(!customProvider.IsSecondaryAvailable && customNames.GetSecondaryNameById("(WP)31") == null, "Public API token removal invalidates previously cached real XNB result");
            registry.ModDirectoryResolver = id => id == "Owner.Test" ? game : null;
            Assert(registry.TryResolveSourceContentRoot(new() { SourceModId = "Owner.Test", ContentRoot = "Content" }, out var root) && root == Path.Combine(game, "Content"), "Pack-relative root resolves from declared owner");
            Assert(!registry.TryResolveSourceContentRoot(new() { SourceModId = "Missing.Owner", ContentRoot = "Content" }, out _), "Unknown owner cannot borrow application Content folder");
            foreach (string locale in new[] { "default", "en", "es", "de", "fr", "pt", "ru", "ja", "zh", "ko", "it", "tr", "hu" })
            {
                using var raw = new RawContentProvider(() => new TestMockServiceProvider(), Path.Combine(game, "Content"), locale);
                using var names = new ItemNameResolver(raw);
                Assert(!string.IsNullOrWhiteSpace(names.GetSecondarySkillName(0)) && !string.IsNullOrWhiteSpace(names.GetSecondaryProfessionTitle(0)), $"Real XNB skill and profession names: {locale}");
                Assert(!string.IsNullOrWhiteSpace(names.GetSecondarySideTabName(7)) && !string.IsNullOrWhiteSpace(names.GetSecondarySecretNoteTitle(23)), $"Real XNB collection and note titles: {locale}");
            }
        }
        var external = new ExternalTranslationRegistry();
        external.RegisterModTranslation("T", "Item", "(O)0", "en", "Object zero");
        external.RegisterModTranslation("T", "Item", "(WP)0", "en", "Wallpaper zero");
        external.RegisterModTranslation("T", "Item", "(FL)0", "en", "Floor zero");
        Assert(external.GetTranslation("Item", "0", "en") == "Object zero" && external.GetTranslation("Item", "(WP)0", "en") == "Wallpaper zero" && external.GetTranslation("Item", "(FL)0", "en") == "Floor zero", "Shared numeric IDs retain type identity");
        external.RegisterModTranslation("T", "Item", "1", "en", "Unqualified object");
        Assert(external.GetTranslation("Item", "(WP)1", "en") == null && external.GetTranslation("Item", "(FL)1", "en") == null, "Wallpaper/flooring cannot consume unqualified Object aliases");
    }
    private static void TestStandardCustomLanguagePack()
    {
        string folder = Path.Combine(Path.GetTempPath(), "untranslated-language-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string[] files = { "content.json", "names.json", "translation.json", "wallpaper.xnb" };
        try
        {
            File.WriteAllText(Path.Combine(folder, "content.json"), @"
                {
                  // Content Patcher accepts comments and trailing commas.
                  ""Changes"": [
                    { ""Action"": ""Include"", ""FromFile"": ""translation.json"", ""When"": { ""Language"": ""eo"" } },
                    { ""Action"": ""Load"", ""Target"": ""Strings/Objects.eo"", ""FromFile"": ""names.json"" },
                    { ""Action"": ""Load"", ""Target"": ""Strings/StringsFromCSFiles.eo"", ""FromFile"": ""wallpaper.xnb"" },
                    { ""Action"": ""EditData"", ""Target"": ""Strings/Objects"", ""Entries"": { ""Test_Name"": ""Wrong locale"" }, ""When"": { ""Language"": ""fr"" } },
                  ]
                }
                ");
            File.WriteAllText(Path.Combine(folder, "names.json"), @"{""Test_Name"":""Esperanto name""}");
            File.WriteAllText(Path.Combine(folder, "translation.json"), @"
                { ""Changes"": [
                  { ""Action"": ""EditData"", ""Target"": ""Strings/UI"", ""Entries"": { ""Example"": ""Esperanto menu"" } },
                  { ""Action"": ""EditData"", ""Target"": ""Data/Powers"", ""Fields"": { ""Example"": { ""DisplayName"": ""Esperanto power"" } } }
                ] }
                ");
            string? game = LocateGamePath();
            string? original = game == null ? null : Path.Combine(game, "Content", "Strings", "StringsFromCSFiles.fr-FR.xnb");
            if (original != null && File.Exists(original))
                File.Copy(original, Path.Combine(folder, "wallpaper.xnb"));

            Type readerType = typeof(RawContentProvider).Assembly.GetType("UntranslatedStardew.Core.CustomLanguagePackReader", throwOnError: true)!;
            var errors = new List<string>();
            var reader = Activator.CreateInstance(readerType, "eo", new TestMockServiceProvider(), (Action<string>)errors.Add)!;
            var packs = new[] { ("Example.CustomLocale", folder) };
            var strings = (Dictionary<string, string>)readerType.GetMethod("Read")!.Invoke(reader, new object[] { packs })!;
            var registry = new LanguageSourceRegistry();
            registry.LoadFromAdditionalLanguages(new[] { new LanguageSourceDescriptor { LanguageCode = "eo", SourceModId = "Example.CustomLocale", Strings = strings } });
            using var provider = new RawContentProvider(() => null, "", "eo", sourceRegistry: registry);
            using var resolver = new ItemNameResolver(provider);
            Assert(provider.GetString("Strings/Objects", "Test_Name") == "Esperanto name", "Custom locale reads locale-suffixed JSON load and skips mismatched edits");
            Assert(provider.GetString("Strings/UI", "Example") == "Esperanto menu", "Custom locale inherits language condition through Include");
            Assert(resolver.GetSecondaryPowerName("Example") == "Esperanto power", "Custom locale exposes structured EditData title through power resolver");
            if (File.Exists(Path.Combine(folder, "wallpaper.xnb")))
            {
                using var french = new RawContentProvider(() => new TestMockServiceProvider(), Path.Combine(game!, "Content"), "fr");
                string? expected = french.GetString("Strings/StringsFromCSFiles", "Wallpaper.cs.13204");
                Assert(expected != null && resolver.GetSecondaryNameById("(WP)0") == expected, "Custom locale reads real XNB Load via normal wallpaper resolver");
            }
            Assert(errors.Count == 0, "JSON/XNB custom pack fixture loads without diagnostic errors");
        }
        finally
        {
            foreach (string file in files) File.Delete(Path.Combine(folder, file));
            Directory.Delete(folder);
        }
    }

    private static void TestInstalledCustomLanguagePairs()
    {
        string game = LocateGamePath() ?? throw new InvalidOperationException("Game installation required for locale-pair audit.");
        string content = Path.Combine(game, "Content");
        string pack = Path.Combine(game, "Mods", "Stardew Valley Viethoa");
        if (!Directory.Exists(pack))
        {
            Console.WriteLine("Skipping installed custom locale pairs: Vietnamese pack not installed.");
            return;
        }

        var services = new TestMockServiceProvider();
        Type readerType = typeof(RawContentProvider).Assembly.GetType("UntranslatedStardew.Core.CustomLanguagePackReader", throwOnError: true)!;
        object reader = Activator.CreateInstance(readerType, "vi", services, (Action<string>)Console.WriteLine)!;
        var strings = (Dictionary<string, string>)readerType.GetMethod("Read")!.Invoke(reader, new object[] { new[] { ("Kou.ViethoaSV", pack) } })!;
        var fontAssets = (Dictionary<string, (string Root, string Asset)>)readerType.GetProperty("SpriteFontAssets")!.GetValue(reader)!;
        var registry = new LanguageSourceRegistry();
        registry.LoadFromAdditionalLanguages(new[] { new LanguageSourceDescriptor { LanguageCode = "vi", SourceModId = "Fixture.CustomLanguage", Strings = strings } });
        Assert(strings.Keys.Any(key => key.StartsWith("Characters/Dialogue/Abigail:", StringComparison.Ordinal)), "Installed CP pack loads NPC dialogue XNB");
        Assert(strings.ContainsKey("Strings/StringsFromCSFiles:Wallpaper.cs.13204"), "Installed CP pack resolves actual wallpaper XNB through Target token");
        Assert(fontAssets.ContainsKey("Fonts/SmallFont") && fontAssets.ContainsKey("Fonts/SpriteFont1"), "Installed CP pack resolves both UI font paths through configured patches");

        foreach (var (primary, secondary) in new[] { ("en", "vi"), ("vi", "en"), ("zh", "vi"), ("vi", "zh") })
        {
            using var primaryData = new RawContentProvider(() => services, content, primary, sourceRegistry: registry);
            using var secondaryData = new RawContentProvider(() => services, content, secondary, sourceRegistry: registry);
            using var primaryNames = new ItemNameResolver(primaryData);
            using var secondaryNames = new ItemNameResolver(secondaryData);
            string? first = primaryNames.GetSecondaryNameById("(WP)0");
            string? second = secondaryNames.GetSecondaryNameById("(WP)0");
            string pair = primary + "-" + secondary;
            Assert(!string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(second) && first != second, $"{pair}: two distinct wallpaper names from actual locale data");
            Assert(primaryNames.GetSecondarySkillName(0) is { Length: > 0 } primarySkill
                && secondaryNames.GetSecondarySkillName(0) is { Length: > 0 } secondarySkill && primarySkill != secondarySkill,
                $"{pair}: localized skill names from both sources");
            if (first == null || second == null)
                continue;

            foreach (string asset in new[] { "Fonts/SmallFont", "Fonts/SpriteFont1" })
            {
                SpriteFont Load(string locale)
                {
                    string path;
                    if (locale == "vi")
                    {
                        var file = fontAssets[asset];
                        path = Path.Combine(file.Root, file.Asset + ".xnb");
                    }
                    else
                        path = Path.Combine(content, asset + (locale == "zh" ? ".zh-CN" : "") + ".xnb");
                    return XnbFontMetadataParser.ParseFile(path).CreateHeadlessSpriteFont();
                }

                var primaryFont = Load(primary);
                var secondaryFont = Load(secondary);
                foreach (bool secondaryFirst in new[] { false, true })
                {
                    var text = StructuredTextComposition.CreateBilingual(first, second, secondaryFirst);
                    BilingualStringTracker.RegisterComposition(text);
                    var plan = BilingualRunPlanner.Plan(text.FullText, primaryFont, secondaryFont);
                    Assert(plan?.Runs.Count == 2
                        && plan.Runs[0].Text == (secondaryFirst ? second : first)
                        && ReferenceEquals(plan.Runs[0].Font, secondaryFirst ? secondaryFont : primaryFont)
                        && ReferenceEquals(plan.Runs[1].Font, secondaryFirst ? primaryFont : secondaryFont),
                        $"{pair}: {asset} uses the correct XNB glyphs for both roles, SecondaryFirst={secondaryFirst}");
                }
            }
        }
        BilingualStringTracker.Clear();
        BilingualRunPlanner.ClearCache();
    }
}
