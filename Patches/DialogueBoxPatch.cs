using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace UntranslatedStardew.Patches;

/// <summary>Renders a small translation for eligible vanilla NPC dialogue.</summary>
[HarmonyPatch(typeof(DialogueBox), nameof(DialogueBox.draw), new[] { typeof(SpriteBatch) })]
internal static class DialogueBoxPatch
{
    private const float SecondaryScale = 0.75f;
    private const int LineGap = 4;

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var draw = AccessTools.Method(typeof(SpriteText), nameof(SpriteText.drawString));
        var calls = code.Where(instruction => instruction.Calls(draw)).ToArray();
        // 1.6.15 draws question text, responses, portraitless text, then portrait text.
        if (calls.Length != 4)
            return code;

        CodeInstruction portraitText = calls[^1];
        var loadBox = new CodeInstruction(OpCodes.Ldarg_0);
        loadBox.labels.AddRange(portraitText.labels);
        portraitText.labels.Clear();
        code.Insert(code.IndexOf(portraitText), loadBox);
        portraitText.operand = AccessTools.Method(typeof(DialogueBoxPatch), nameof(DrawPortraitText));
        return code;
    }

    private static void DrawPortraitText(
        SpriteBatch batch, string text, int x, int y, int position, int width, int height,
        float alpha, float depth, bool junimo, int background, string placeholder, Color? color,
        SpriteText.ScrollTextAlignment alignment, DialogueBox box)
    {
        int primaryY = y;
        int secondaryY = y;
        string? secondary = null;
        SpriteFont? font = null;
        ModEntry? mod = ModEntry.Instance;

        bool? previewPlacement = mod?.GetDialoguePreviewPlacement(box);
        if (mod != null && (previewPlacement.HasValue || mod.Config.ShowSecondaryInDialogue)
            && !Game1.eventUp && !box.isQuestion && box.isPortraitBox()
            && !mod.Helper.ModRegistry.IsLoaded("Mangupix.DialogueDisplayFrameworkContinued")
            && box.characterDialoguesBrokenUp.Count == 1
            && SpriteText.getWidthOfString(text) < width
            && mod.GetSecondaryDialogueText(box.characterDialogue, text) is { } candidate
            && mod.FontManager?.GetSecondaryFont(Game1.dialogueFont) is { } secondaryFont
            && candidate.All(secondaryFont.Characters.Contains))
        {
            Vector2 size = secondaryFont.MeasureString(candidate) * SecondaryScale;
            int primaryHeight = SpriteText.getHeightOfString(text, width);
            int secondaryHeight = (int)Math.Ceiling(Math.Max(size.Y, secondaryFont.LineSpacing * SecondaryScale));
            if (size.X < width && primaryHeight + LineGap + secondaryHeight <= box.height - 16)
            {
                secondary = candidate;
                font = secondaryFont;
                if (previewPlacement ?? mod.Config.SecondaryFirst)
                    primaryY += secondaryHeight + LineGap;
                else
                    secondaryY += primaryHeight + LineGap;
            }
        }

        SpriteText.drawString(batch, text, x, primaryY, position, width, height, alpha, depth,
            junimo, background, placeholder, color, alignment);

        if (secondary != null && font != null && position >= text.Length)
        {
            batch.DrawString(font, secondary, new Vector2(x, secondaryY), Game1.textColor,
                0f, Vector2.Zero, SecondaryScale, SpriteEffects.None, depth);
        }
    }
}
