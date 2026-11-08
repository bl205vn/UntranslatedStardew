using System.Xml.Linq;
using HarmonyLib;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using UntranslatedStardew.Core;
using UntranslatedStardew.Patches;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static void TestNpcDialogueGeometry()
    {
        Assert(NpcDialogueGeometry.AvailableBody(720) == 528
            && NpcDialogueGeometry.FrameHeight(376) == 424
            && NpcDialogueGeometry.FrameHeight(100) == 384,
            "NPC frame grows for the real 376px Shane pair without shrinking the native portrait");
        Assert(NpcDialogueGeometry.AvailableBody(480) == 0
            && NpcDialogueGeometry.PortraitTopOffset(424) == 20,
            "NPC frame rejects insufficient viewport space and centers fixed portrait artwork");
        var harmony = new Harmony("UntranslatedStardew.Tests.NpcGeometry");
        try
        {
            harmony.CreateClassProcessor(typeof(NpcDialogueGeometryPatch)).Patch();
            harmony.CreateClassProcessor(typeof(NpcPortraitFramePatch)).Patch();
            Assert(Harmony.GetPatchInfo(AccessTools.Method(typeof(DialogueBox), "checkDialogue"))!
                .Transpilers.Any(p => p.owner == harmony.Id),
                "Real NPC pagination accepts the geometry hook before splitting an authored page");
            Assert(Harmony.GetPatchInfo(AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.drawPortrait)))!
                .Transpilers.Any(p => p.owner == harmony.Id),
                "Real portrait IL accepts native-size artwork centering in an expanded frame");
        }
        finally { harmony.UnpatchAll(harmony.Id); }

        string? game = LocateGamePath();
        if (game == null) return;
        string pack = Path.Combine(game, "Mods", "Stardew Valley Viethoa");
        string folder = Path.Combine(pack, "assets", "Content", "Fonts", "Font-Vi");
        string path = Path.Combine(folder, "NewFont.fnt");
        if (!File.Exists(path)) return;
        var services = new TestMockServiceProvider();
        var strings = new CustomLanguagePackReader("vi", services, _ => { }).Read(new[] { ("Kou.ViethoaSV", pack) });
        using var content = new LocalizedContentManager(services, Path.Combine(game, "Content"));
        string main = content.Load<Dictionary<string, string>>("Characters/Dialogue/Shane")["Wed8"];
        string shown = DialogueTranslation.SplitPages(main)[0];
        string? secondary = DialogueTranslation.GetSecondaryLine(main, strings["Characters/Dialogue/Shane:Wed8"],
            0, shown, "Tofu", Gender.Female, 0);
        string xml = File.ReadAllText(path);
        var dimensions = XDocument.Parse(xml).Root!.Element("pages")!.Elements("page").Select(page =>
        {
            string name = (string)page.Attribute("file")!;
            byte[] png = File.ReadAllBytes(Path.Combine(folder, Path.HasExtension(name) ? name : name + ".png"));
            return new DialogueNativeFont.TextureDimensions(
                System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)),
                System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));
        }).ToArray();
        var font = DialogueNativeFont.CreateBitmapFromXml(xml, 1, 1, dimensions)!;
        var oldLocale = LocalizedContentManager.CurrentLanguageCode;
        float oldZoom = SpriteText.fontPixelZoom;
        bool oldForce = SpriteText.forceEnglishFont;
        try
        {
            LocalizedContentManager.CurrentLanguageCode = LocalizedContentManager.LanguageCode.en;
            SpriteText.fontPixelZoom = 3;
            SpriteText.forceEnglishFont = false;
            Assert(secondary != null && font.Supports(secondary),
                "Installed Shane Wed8 source resolves with actual Vietnamese native dialogue glyphs");
            var pairs = new[] { new DialogueTextPair(shown, secondary!) };
            DialogueBlockLayout.Page? Fit(int height, bool above) => DialogueBlockLayout.Fit(pairs, 716, height, above,
                s => PrimaryDialogueMetrics.Measure(s, 716), s => font.Measure(s).X * .75f,
                s => font.Measure(s).Y * .75f);
            var below = Fit(NpcDialogueGeometry.AvailableBody(720), false);
            var above = Fit(NpcDialogueGeometry.AvailableBody(720), true);
            Assert(Fit(368, false) == null && below is { Height: 376 } && above?.Height == below.Height,
                "Installed Shane Wed8 reproduces old overflow and fits expanded frame in both language orders");
            Assert(below != null && NpcDialogueGeometry.FrameHeight(below.Height) <= 720 - 144
                && NpcDialogueGeometry.FrameHeight(below.Height) - below.Height >= 48,
                "Expanded real-font dialogue stays within viewport and reserves advance-icon space");
        }
        finally
        {
            LocalizedContentManager.CurrentLanguageCode = oldLocale;
            SpriteText.fontPixelZoom = oldZoom;
            SpriteText.forceEnglishFont = oldForce;
        }
    }
}
