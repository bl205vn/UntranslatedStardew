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
        var playerField = AccessTools.Field(typeof(Game1), "_player");
        object? oldPlayer = playerField.GetValue(null);
        try
        {
            var farmer = (Farmer)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Farmer));
            AccessTools.Field(typeof(Farmer), "netGender").SetValue(farmer, new Netcode.NetEnum<Gender>(Gender.Female));
            playerField.SetValue(null, farmer);
            var font = MakeRoleFont(8, 16);
            string Parse(string text) => Game1.parseText(text, font, 120);
            const string description = "A very long museum description that needs several lines of text.";
            string raw = Parse(" - Ancient Seed - ^" + description);
            var box = (StardewValley.Menus.DialogueBox)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(StardewValley.Menus.DialogueBox));
            box.dialogues = new() { raw };
            string visible = box.getCurrentString();
            var paired = MuseumArtifactTranslation.Match("Ancient Seed", description, "Hạt cổ đại", "Mô tả dài.", visible, Parse);
            Assert(raw.Contains(Environment.NewLine) && !visible.Contains(Environment.NewLine) && paired != null,
                "Real Game1.parseText and DialogueBox.getCurrentString preserve a long museum pairing after newline removal");
            Assert(paired != null && ObjectDialogueTranslation.CurrentPage(new[] { paired.Value }, box.dialogues) == 0
                && paired.Value.Primary == visible,
                "Museum page selection and draw guard agree with the real normalized current string");
            box.dialogues[0] += " changed";
            Assert(paired != null && ObjectDialogueTranslation.CurrentPage(new[] { paired.Value }, box.dialogues) == -1,
                "Normalized museum page matching still rejects stale text");
        }
        finally { playerField.SetValue(null, oldPlayer); }
        var content = new LocalizedContentManager(new DummyServiceProvider(), Path.Combine(LocateGamePath()!, "Content"));
        try
        {
            var objects = content.Load<Dictionary<string, ObjectData>>("Data/Objects");
            foreach (string id in new[] { "96", "106", "114" })
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
