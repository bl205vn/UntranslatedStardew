using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>Expands an NPC frame only when the complete validated bilingual page fits on screen.</summary>
[HarmonyPatch]
internal static class NpcDialogueGeometryPatch
{
    private sealed record Resized(int OriginalHeight);
    private static readonly ConditionalWeakTable<DialogueBox, Resized> Resizes = new();
    private static readonly MethodInfo Icons = AccessTools.Method(typeof(DialogueBox), "setUpIcons");

    internal static void ReflowPreview(DialogueBox box)
    {
        // Preview provenance is registered after its constructor. Reflow only this
        // new, isolated sample, never an NPC's partially consumed live stack.
        if (ModEntry.Instance?.GetDialoguePreview(box) == null) return;
        ReflowFreshPage(box);
    }

    internal static void ReflowFreshPage(DialogueBox box)
    {
        if (box.characterDialogue == null || box.isQuestion || box.characterIndexInDialogue > 0) return;
        string full = box.characterDialogue.getCurrentDialogue();
        string text = ModEntry.GetDialogueSpeakerPrefix(box.characterDialogue) + ObjectDialogueTranslation.VisibleText(full);
        if (DialogueBoxPatch.GetLayout(box, text, box.isPortraitBox() ? box.width - 484 : box.width - 16, true) == null) return;
        box.characterDialoguesBrokenUp.Clear();
        box.characterDialoguesBrokenUp.Push(full);
        Refresh(box);
    }
    private static IEnumerable<MethodBase> TargetMethods() => new[]
    {
        AccessTools.Method(typeof(DialogueBox), "checkDialogue"),
        AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.draw), new[] { typeof(SpriteBatch) }),
        AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.gameWindowSizeChanged))
    };

    private static void Prefix(DialogueBox __instance, MethodBase __originalMethod)
    {
        if (__originalMethod.Name != nameof(DialogueBox.gameWindowSizeChanged)) Refresh(__instance);
    }

    private static void Postfix(DialogueBox __instance, MethodBase __originalMethod)
    {
        if (__originalMethod.Name == nameof(DialogueBox.gameWindowSizeChanged)) Refresh(__instance);
    }

    private static void Refresh(DialogueBox box)
    {
        if (box.characterDialogue == null) return;
        bool resized = Resizes.TryGetValue(box, out var old);
        if (box.characterDialogue.isCurrentDialogueAQuestion())
        {
            if (resized) SetHeight(box, old!.OriginalHeight);
            Resizes.Remove(box);
            return;
        }
        var page = DialogueBoxPatch.GetLayout(box, box.getCurrentString(),
            box.isPortraitBox() ? box.width - 484 : box.width - 16);
        int target = page == null ? old?.OriginalHeight ?? box.height : NpcDialogueGeometry.FrameHeight(page.Page.Height);
        if (page != null && !resized) Resizes.Add(box, new(box.height));
        if (box.height != target) SetHeight(box, target);
        if (page == null) Resizes.Remove(box);
    }

    private static void SetHeight(DialogueBox box, int height)
    {
        box.height = height;
        box.y = Game1.uiViewport.Height - height - 64;
        box.yPositionOnScreen = box.y;
        box.friendshipJewel.Y = box.y + NpcDialogueGeometry.PortraitTopOffset(height) + 256;
        Icons.Invoke(box, null);
    }

    // Vanilla's pagination reserves two spare lines. If the *actual* native draw
    // and its translation fit our frame, keep that authored page intact.
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var code = instructions.ToList();
        if (__originalMethod.Name != "checkDialogue") return code;
        var split = AccessTools.Method(typeof(SpriteText), nameof(SpriteText.getSubstringBeyondHeight));
        foreach (var call in code.Where(i => i.Calls(split)).ToArray())
        {
            var load = new CodeInstruction(OpCodes.Ldarg_0);
            load.labels.AddRange(call.labels);
            call.labels.Clear();
            code.Insert(code.IndexOf(call), load);
            call.operand = AccessTools.Method(typeof(NpcDialogueGeometryPatch), nameof(Overflow));
        }
        return code;
    }

    private static string Overflow(string text, int width, int height, DialogueBox box)
        => Resizes.TryGetValue(box, out _) && !box.characterDialogue.isCurrentDialogueAQuestion()
            ? "" : SpriteText.getSubstringBeyondHeight(text, width, height);
}

/// <summary>Centers the fixed portrait artwork within the expanded frame, preserving its native size.</summary>
[HarmonyPatch(typeof(DialogueBox), nameof(DialogueBox.drawPortrait))]
internal static class NpcPortraitFramePatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var draw = AccessTools.Method(typeof(SpriteBatch), nameof(SpriteBatch.Draw), new[]
        {
            typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color), typeof(float),
            typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float)
        });
        foreach (var call in code.Where(i => i.Calls(draw)).ToArray())
        {
            var load = new CodeInstruction(OpCodes.Ldarg_0);
            load.labels.AddRange(call.labels);
            call.labels.Clear();
            code.Insert(code.IndexOf(call), load);
            call.opcode = OpCodes.Call;
            call.operand = AccessTools.Method(typeof(NpcPortraitFramePatch), nameof(Draw));
        }
        return code;
    }

    private static void Draw(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source,
        Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth, DialogueBox box)
    {
        if (source == new Rectangle(583, 411, 115, 97))
            position.Y += NpcDialogueGeometry.PortraitTopOffset(box.height);
        batch.Draw(texture, position, source, color, rotation, origin, scale, effects, depth);
    }
}
