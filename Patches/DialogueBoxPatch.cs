using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>Renders a small translation for eligible vanilla NPC dialogue.</summary>
[HarmonyPatch(typeof(DialogueBox), nameof(DialogueBox.draw), new[] { typeof(SpriteBatch) })]
internal static class DialogueBoxPatch
{
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

        var preview = mod?.GetDialoguePreview(box);
        if (mod != null && (preview.HasValue || mod.Config.ShowSecondaryInDialogue)
            && !Game1.eventUp && !box.isQuestion && box.isPortraitBox()
            && !mod.Helper.ModRegistry.IsLoaded("Mangupix.DialogueDisplayFrameworkContinued")
            && box.characterDialoguesBrokenUp.Count == 1
            && box.characterDialogue is { } dialogue
            && ResolveSecondary(mod, dialogue, text, preview?.Lines) is { } candidate
            && mod.FontManager?.GetSecondaryFont(Game1.dialogueFont) is { } secondaryFont)
        {
            candidate = candidate.Normalize();
            if (candidate.All(secondaryFont.Characters.Contains))
            {
                int primaryHeight = SpriteText.getHeightOfString(text, width - 4);
                var layout = DialoguePairLayout.Fit(candidate, width, box.height - 16, primaryHeight,
                    preview?.SecondaryFirst ?? mod.Config.SecondaryFirst,
                    line => secondaryFont.MeasureString(line).X * DialoguePairLayout.SecondaryScale,
                    lines => Math.Max(secondaryFont.MeasureString(lines).Y,
                        (1 + lines.Count(c => c == '\n')) * secondaryFont.LineSpacing) * DialoguePairLayout.SecondaryScale);
                if (layout is { } fitted)
                {
                    secondary = fitted.Secondary;
                    font = secondaryFont;
                    primaryY += fitted.PrimaryOffset;
                    secondaryY += fitted.SecondaryOffset;
                }
            }
        }

        SpriteText.drawString(batch, text, x, primaryY, position, width, height, alpha, depth,
            junimo, background, placeholder, color, alignment);

        if (secondary != null && font != null && position >= text.Length)
        {
            batch.DrawString(font, secondary, new Vector2(x, secondaryY), Game1.textColor,
                0f, Vector2.Zero, DialoguePairLayout.SecondaryScale, SpriteEffects.None, depth);
        }
    }

    private static string? ResolveSecondary(
        ModEntry mod, Dialogue dialogue, string text, (string Primary, string Secondary)[]? previewLines)
    {
        if (previewLines == null)
            return mod.GetSecondaryDialogueText(dialogue, text);

        var line = previewLines.ElementAtOrDefault(dialogue.currentDialogueIndex);
        return string.Equals(line.Primary, text, StringComparison.Ordinal) ? line.Secondary : null;
    }
}
