using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StardewValley;
using StardewValley.GameData.Movies;
using StardewValley.Locations;
using StardewValley.Menus;
using UntranslatedStardew.Core;
using UntranslatedStardew.Patches;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static void TestMoviePosterDialogue()
    {
        Console.WriteLine("\n-- Testing Movie Poster Bilingual Adapter --");

        // 1. Token parsing and validation
        Assert(MoviePosterAdapter.TryParseLocalizedToken("[LocalizedText Strings\\Movies:BraveLittleSapling_Title]", out string asset1, out string key1)
            && asset1 == "Strings/Movies" && key1 == "BraveLittleSapling_Title",
            "Movie token parser parses standard backslash LocalizedText token");

        Assert(MoviePosterAdapter.TryParseLocalizedToken("[LocalizedText Strings/Movies:BraveLittleSapling_Description]", out string asset2, out string key2)
            && asset2 == "Strings/Movies" && key2 == "BraveLittleSapling_Description",
            "Movie token parser parses slash LocalizedText token");

        Assert(!MoviePosterAdapter.TryParseLocalizedToken("Plain Movie Title", out _, out _)
            && !MoviePosterAdapter.TryParseLocalizedToken("[LocalizedText Data/Objects:74]", out _, out _)
            && !MoviePosterAdapter.TryParseLocalizedToken("[LocalizedText Strings/Movies:Key arg1 arg2]", out _, out _)
            && !MoviePosterAdapter.TryParseLocalizedToken(null, out _, out _),
            "Movie token parser rejects plain text, unsupported assets, extra args, and nulls");

        // 2. Safe formatting
        Assert(MoviePosterAdapter.FormatSafely("Now Playing: \"{0}.\"", "The Brave Little Sapling") == "Now Playing: \"The Brave Little Sapling.\"",
            "FormatSafely formats valid template with argument");
        Assert(MoviePosterAdapter.FormatSafely("\"{0}\"", "A great movie.") == "\"A great movie.\"",
            "FormatSafely formats quotes around description");
        Assert(MoviePosterAdapter.FormatSafely("No placeholder", "Movie") == null,
            "FormatSafely rejects template without {0}");
        Assert(MoviePosterAdapter.FormatSafely("Invalid {format", "Movie") == null,
            "FormatSafely catches format exception safely");
        Assert(MoviePosterAdapter.FormatSafely(null, "Movie") == null
            && MoviePosterAdapter.FormatSafely("Template", null) == null,
            "FormatSafely rejects null arguments safely");

        // 3. Mock content provider for movie poster pairing
        string game = LocateGamePath()!;
        var testContent = new LocalizedContentManager(new DummyServiceProvider(), Path.Combine(game, "Content"));
        var mockProvider = new MockContentProvider("vi", "vi");

        mockProvider.AddLocalized("Strings/Locations", "Theater_Poster_0", "Đang chiếu: \"{0}.\"");
        mockProvider.AddLocalized("Strings/Locations", "Theater_Poster_1", "\"{0}\"");
        mockProvider.AddLocalized("Strings/Locations", "Theater_Poster_Coming_Soon", "Sắp chiếu: \"{0}.\"");

        mockProvider.AddLocalized("Strings/Movies", "BraveLittleSapling_Title", "Cây Con Dũng Cảm");
        mockProvider.AddLocalized("Strings/Movies", "BraveLittleSapling_Description", "Câu chuyện hoạt hình về một mầm cây nhỏ với ước mơ lớn.");

        var saplingMovie = new MovieData
        {
            Id = "spring_movie_0",
            Title = "[LocalizedText Strings\\Movies:BraveLittleSapling_Title]",
            Description = "[LocalizedText Strings\\Movies:BraveLittleSapling_Description]"
        };

        // 4. Two pages now-playing poster pairing
        var pages = MoviePosterAdapter.GetMoviePosterPages(saplingMovie, isUpcoming: false, mockProvider, testContent);
        Assert(pages != null && pages.Length == 2, "Now-playing poster produces exactly two bilingual pages");
        Assert(pages![0].Primary.Contains("The Brave Little Sapling")
            && pages[0].Secondary == "Đang chiếu: \"Cây Con Dũng Cảm.\"",
            "Now-playing page 0 binds primary title and secondary translated template");
        Assert(pages[1].Primary.Contains("little sprout on a magical adventure")
            && pages[1].Secondary == "\"Câu chuyện hoạt hình về một mầm cây nhỏ với ước mơ lớn.\"",
            "Now-playing page 1 binds primary description and secondary translated template");

        // 5. Upcoming poster single-page pairing
        var upcomingPages = MoviePosterAdapter.GetMoviePosterPages(saplingMovie, isUpcoming: true, mockProvider, testContent);
        Assert(upcomingPages != null && upcomingPages.Length == 1, "Upcoming poster produces exactly one bilingual page");
        Assert(upcomingPages![0].Primary.Contains("The Brave Little Sapling")
            && upcomingPages[0].Secondary == "Sắp chiếu: \"Cây Con Dũng Cảm.\"",
            "Upcoming poster binds primary title and secondary translated coming soon template");

        // 6. Visible dialogues mismatch / stale primary guard
        var staleVisible = new List<string> { "Stale Old Title", "Stale Old Description" };
        Assert(MoviePosterAdapter.GetMoviePosterPages(saplingMovie, isUpcoming: false, mockProvider, testContent, visibleDialogues: staleVisible) == null,
            "Rejects attachment when visible primary text on dialogue box does not match movie");

        var correctVisible = new List<string> { pages[0].Primary, pages[1].Primary };
        Assert(MoviePosterAdapter.GetMoviePosterPages(saplingMovie, isUpcoming: false, mockProvider, testContent, visibleDialogues: correctVisible) != null,
            "Accepts attachment when visible primary text on dialogue box matches movie");

        // 7. Missing translated fields fall back to primary-only
        var incompleteMovie = new MovieData
        {
            Id = "incomplete_movie",
            Title = "[LocalizedText Strings\\Movies:Missing_Title]",
            Description = "[LocalizedText Strings\\Movies:BraveLittleSapling_Description]"
        };
        Assert(MoviePosterAdapter.GetMoviePosterPages(incompleteMovie, isUpcoming: false, mockProvider) == null,
            "Falls back to primary-only when movie title translation is missing");

        var incompleteDescMovie = new MovieData
        {
            Id = "incomplete_desc_movie",
            Title = "[LocalizedText Strings\\Movies:BraveLittleSapling_Title]",
            Description = "[LocalizedText Strings\\Movies:Missing_Description]"
        };
        Assert(MoviePosterAdapter.GetMoviePosterPages(incompleteDescMovie, isUpcoming: false, mockProvider) == null,
            "Falls back to primary-only when movie description translation is missing");

        // 8. Unsafe token / command parameter injection rejection
        mockProvider.AddLocalized("Strings/Movies", "Injected_Title", "Title#$action AddMoney 1000");
        var injectedMovie = new MovieData
        {
            Id = "injected_movie",
            Title = "[LocalizedText Strings\\Movies:Injected_Title]",
            Description = "[LocalizedText Strings\\Movies:BraveLittleSapling_Description]"
        };
        Assert(MoviePosterAdapter.GetMoviePosterPages(injectedMovie, isUpcoming: false, mockProvider) == null,
            "Rejects injected commands or unsafe characters in secondary movie title");

        mockProvider.AddLocalized("Strings/Movies", "Injected_Desc", "Desc [74] %item");
        var injectedDescMovie = new MovieData
        {
            Id = "injected_desc_movie",
            Title = "[LocalizedText Strings\\Movies:BraveLittleSapling_Title]",
            Description = "[LocalizedText Strings\\Movies:Injected_Desc]"
        };
        Assert(MoviePosterAdapter.GetMoviePosterPages(injectedDescMovie, isUpcoming: false, mockProvider) == null,
            "Rejects injected tokens in secondary movie description");

        // 9. Structured Data/Movies CP pack field extraction via RawContentProvider
        using (var rawEn = new RawContentProvider(() => new DummyServiceProvider(), Path.Combine(game, "Content"), "en"))
        {
            Assert(rawEn.TryGetDialogueString("Strings/Movies", "BraveLittleSapling_Title", out string? realTitleEn)
                && realTitleEn == "The Brave Little Sapling",
                "RawContentProvider loads real vanilla Strings/Movies:BraveLittleSapling_Title");

            rawEn.TryGetDialogueString("Strings/Movies", "BraveLittleSapling_Description", out string? realDescEn);
            Assert(realDescEn != null && realDescEn.Contains("sprout on a magical adventure"),
                "RawContentProvider loads real vanilla Strings/Movies:BraveLittleSapling_Description");
        }

        // Custom packs may remap movie fields to a different LocalizedText key.
        string packRoot = Path.Combine(Path.GetTempPath(), "UntranslatedStardew.MoviePoster." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(packRoot);
        string contentFile = Path.Combine(packRoot, "content.json");
        try
        {
            var fields = new Dictionary<string, Dictionary<string, string?>>
            {
                [saplingMovie.Id] = new() { ["Title"] = "[LocalizedText Strings/Movies:CustomTitle]", ["Description"] = "Mô tả từ metadata." }
            };
            object Patch(Dictionary<string, Dictionary<string, string?>> f) => new
                { Action = "EditData", Target = "Data/Movies", When = new { Language = "vi" }, Fields = f };
            File.WriteAllText(contentFile, System.Text.Json.JsonSerializer.Serialize(new { Changes = new[] { Patch(fields) } }));
            var reader = new CustomLanguagePackReader("vi", null, _ => { });
            var strings = reader.Read(new[] { ("tests.movie", packRoot) });
            Assert(strings["Data/Movies/Title:" + saplingMovie.Id] == fields[saplingMovie.Id]["Title"]
                && strings["Data/Movies/Description:" + saplingMovie.Id] == "Mô tả từ metadata.",
                "Custom movie metadata projects Title and Description from locale-specific Fields edits");
            strings["Strings/Movies:CustomTitle"] = "Tên phim đã chuyển khóa";
            var registry = new LanguageSourceRegistry();
            using var registration = registry.Register(new LanguageSourceDescriptor
                { LanguageCode = "vi", SourceModId = "tests.movie", Strings = strings });
            using var provider = new RawContentProvider(() => null, game, "vi", sourceRegistry: registry);
            Assert(MoviePosterAdapter.ResolveSecondaryField(saplingMovie.Title, saplingMovie.Id, true, provider) == "Tên phim đã chuyển khóa",
                "Structured custom movie title resolves its own LocalizedText key in the secondary locale");
            Assert(MoviePosterAdapter.ResolveSecondaryField(saplingMovie.Description, saplingMovie.Id, false, provider) == "Mô tả từ metadata.",
                "Structured custom movie description preserves plain translated metadata");
            Assert(MoviePosterAdapter.ResolveSecondaryField(saplingMovie.Title, "different_movie", true, provider) == null,
                "Custom movie metadata cannot leak from a different movie ID");
            var clearTitle = new Dictionary<string, Dictionary<string, string?>> { [saplingMovie.Id] = new() { ["Title"] = null } };
            File.WriteAllText(contentFile, System.Text.Json.JsonSerializer.Serialize(new { Changes = new[] { Patch(fields), Patch(clearTitle) } }));
            var cleared = new CustomLanguagePackReader("vi", null, _ => { }).Read(new[] { ("tests.movie", packRoot) });
            Assert(!cleared.ContainsKey("Data/Movies/Title:" + saplingMovie.Id)
                && cleared.ContainsKey("Data/Movies/Description:" + saplingMovie.Id),
                "Clearing a movie title removes stale metadata while preserving untouched description");
        }
        finally
        {
            File.Delete(contentFile);
            Directory.Delete(packRoot);
        }
        const string uncertainPrimary = "[Random Unstable Movie Title]";
        Assert(MoviePosterAdapter.ResolvePrimaryText(uncertainPrimary, testContent) == uncertainPrimary,
            "Poster reconstruction leaves unsupported primary tokens unexpanded instead of evaluating them twice");

        // 10. Multi-page navigation bounds and CurrentPage verification
        var pairList = pages.ToList();
        var remaining2 = new List<string> { pages[0].Primary, pages[1].Primary };
        Assert(ObjectDialogueTranslation.CurrentPage(pairList, remaining2) == 0,
            "Page 0 index correctly identified from remaining dialogues");

        remaining2.RemoveAt(0); // Player clicks to advance to page 1
        Assert(ObjectDialogueTranslation.CurrentPage(pairList, remaining2) == 1,
            "Page 1 index correctly identified after page 0 is removed");

        remaining2.RemoveAt(0); // Dialogue ends
        Assert(ObjectDialogueTranslation.CurrentPage(pairList, remaining2) == -1,
            "Empty dialogues returns -1 (closing dialogue)");

        // 11. Harmony patch installation on MovieTheater and GameLocation
        var harmony = new Harmony("UntranslatedStardew.Tests.MoviePoster");
        try
        {
            harmony.CreateClassProcessor(typeof(MoviePosterPatch)).Patch();
            var getMovieToday = AccessTools.Method(typeof(MovieTheater), nameof(MovieTheater.GetMovieToday));
            var getUpcoming = AccessTools.Method(typeof(MovieTheater), nameof(MovieTheater.GetUpcomingMovie));
            var performAction = AccessTools.Method(typeof(GameLocation), nameof(GameLocation.performAction),
                new[] { typeof(string[]), typeof(Farmer), AccessTools.TypeByName("xTile.Dimensions.Location") });

            Assert(Harmony.GetPatchInfo(getMovieToday)?.Postfixes.Any(p => p.owner == harmony.Id) == true,
                "MovieTheater.GetMovieToday postfix installs correctly");
            Assert(Harmony.GetPatchInfo(getUpcoming)?.Postfixes.Any(p => p.owner == harmony.Id) == true,
                "MovieTheater.GetUpcomingMovie postfix installs correctly");
            Assert(Harmony.GetPatchInfo(performAction)?.Prefixes.Any(p => p.owner == harmony.Id) == true
                && Harmony.GetPatchInfo(performAction)?.Postfixes.Any(p => p.owner == harmony.Id) == true,
                "GameLocation.performAction prefix and postfix install correctly");
            Assert(Harmony.GetPatchInfo(performAction)?.Finalizers.Any(p => p.owner == harmony.Id) == true,
                "Movie action context installs finalizer cleanup for exceptions");
            var prefix = AccessTools.Method(typeof(MoviePosterPatch), "PrefixPerformAction");
            var capture = AccessTools.Method(typeof(MoviePosterPatch), "PostfixGetMovieToday");
            var finalizer = AccessTools.Method(typeof(MoviePosterPatch), "FinalizerPerformAction");
            var active = AccessTools.Field(typeof(MoviePosterPatch), "_active");
            object? originalContext = active.GetValue(null);
            try
            {
                object?[] outer = { new[] { "Theater_Poster" }, null };
                prefix.Invoke(null, outer);
                capture.Invoke(null, new object[] { saplingMovie });
                object? captured = active.GetValue(null);
                Assert(captured != null && ReferenceEquals(AccessTools.Field(captured.GetType(), "Movie").GetValue(captured), saplingMovie),
                    "Poster scope captures the exact movie instance returned by vanilla");
                object?[] nested = { new[] { "UnrelatedAction" }, null };
                prefix.Invoke(null, nested);
                capture.Invoke(null, new object[] { incompleteMovie });
                Assert(active.GetValue(null) == null, "Nested unrelated actions cannot capture a movie into the outer poster scope");
                var failure = new InvalidOperationException("fixture");
                Assert(ReferenceEquals(finalizer.Invoke(null, new object?[] { failure, nested[1] }), failure)
                    && ReferenceEquals(active.GetValue(null), captured),
                    "Nested action failure preserves the exception and restores the outer movie context");
                finalizer.Invoke(null, new object?[] { failure, outer[1] });
                Assert(ReferenceEquals(active.GetValue(null), originalContext), "Poster failure restores its original context without leaking movie data");
            }
            finally { active.SetValue(null, originalContext); }

        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }

        // 12. Headless DialogueBox boundary test with existing snapshot fixture
        var namesTranslated = AccessTools.Field(typeof(Dialogue), "nameArraysTranslated");
        bool originalNamesTranslated = (bool)namesTranslated.GetValue(null)!;
        var originalGame = Game1.game1;
        try
        {
            Game1.game1 ??= (Game1)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Game1));
            namesTranslated.SetValue(null, true);

            var dialogueBox = (DialogueBox)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(DialogueBox));
            dialogueBox.dialogues = new List<string> { pages[0].Primary, pages[1].Primary };
            dialogueBox.width = 1200;
            dialogueBox.height = 384;

            bool attached = ObjectDialoguePatch.Attach(dialogueBox, pages);
            Assert(attached, "ObjectDialoguePatch.Attach successfully attaches movie poster pages to DialogueBox");
        }
        finally
        {
            namesTranslated.SetValue(null, originalNamesTranslated);
            Game1.game1 = originalGame;
        }
    }
}
