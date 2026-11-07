using HarmonyLib;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.Locations;
using UntranslatedStardew.Core;
using UntranslatedStardew.Patches;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static void TestMuseumArtifacts()
    {
        const string shown = "- Dwarf Scroll I - ^A yellowed scroll.";
        DialogueTextPair? Pair(string? name = "Cuộn giấy I", string? desc = "Cuộn giấy cũ.", string visible = shown)
            => MuseumArtifactTranslation.Match("Dwarf Scroll I", "A yellowed scroll.", name, desc, visible, text => text.Trim());
        Assert(Pair()?.Secondary == "- Cuộn giấy I - ^Cuộn giấy cũ.",
            "Museum examination keeps name and description together in one secondary block");
        Assert(Pair(visible: "A different exhibit") == null && Pair(desc: null) == null && Pair(name: "") == null,
            "Museum examination rejects stale display and partially translated fields");
        Assert(Pair(desc: "Another#Page") == null && Pair(desc: "[Random text]") == null,
            "Museum examination rejects extra pages and unresolved secondary tokens");
        var content = new LocalizedContentManager(new DummyServiceProvider(), Path.Combine(LocateGamePath()!, "Content"));
        try
        {
            var objects = content.Load<Dictionary<string, ObjectData>>("Data/Objects");
            foreach (string id in new[] { "96", "106" })
            {
                var item = objects[id];
                Assert(MoviePosterAdapter.TryParseLocalizedToken(item.DisplayName, out string nameAsset, out string nameKey)
                    && MoviePosterAdapter.TryParseLocalizedToken(item.Description, out string descAsset, out string descKey)
                    && !string.IsNullOrWhiteSpace(content.LoadString(nameAsset.Replace('/', '\\') + ":" + nameKey))
                    && !string.IsNullOrWhiteSpace(content.LoadString(descAsset.Replace('/', '\\') + ":" + descKey)),
                    "Real museum item " + id + " binds Data/Objects name and description to installed string keys");
            }
        }
        finally { content.Dispose(); }
        var harmony = new Harmony("UntranslatedStardew.Tests.MuseumArtifacts");
        try
        {
            harmony.CreateClassProcessor(typeof(MuseumArtifactPatch)).Patch();
            var info = Harmony.GetPatchInfo(AccessTools.Method(typeof(LibraryMuseum), nameof(LibraryMuseum.checkAction)));
            Assert(info?.Prefixes.Any(p => p.owner == harmony.Id) == true && info.Postfixes.Any(p => p.owner == harmony.Id),
                "Museum examination capture installs on the real LibraryMuseum checkAction boundary");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }
}
