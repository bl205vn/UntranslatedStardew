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

        foreach (CodeInstruction bodyCall in calls)
        {
            var loadBox = new CodeInstruction(OpCodes.Ldarg_0);
            loadBox.labels.AddRange(bodyCall.labels);
            bodyCall.labels.Clear();
            code.Insert(code.IndexOf(bodyCall), loadBox);
            bodyCall.operand = AccessTools.Method(typeof(DialogueBoxPatch), nameof(DrawDialogueText));
        }
        return code;
    }

    private static void DrawDialogueText(
        SpriteBatch batch, string text, int x, int y, int position, int width, int height,
        float alpha, float depth, bool junimo, int background, string placeholder, Color? color,
        SpriteText.ScrollTextAlignment alignment, DialogueBox box)
    {
        if (ObjectDialoguePatch.TryDraw(batch, text, x, y, position, alpha, depth, box))
            return;
        if (DialogueQuestionPatch.TryDraw(batch, text, x, y, position, alpha, depth, box))
            return;

        ModEntry? mod = ModEntry.Instance;
        var preview = mod?.GetDialoguePreview(box);
        if (mod != null && (preview.HasValue || mod.Config.ShowSecondaryInDialogue)
            && !Game1.eventUp && !box.isQuestion
            && !mod.Helper.ModRegistry.IsLoaded("Mangupix.DialogueDisplayFrameworkContinued")
            && box.characterDialoguesBrokenUp.Count == 1
            && box.characterDialogue is { } dialogue
            && ResolvePairs(mod, dialogue, text, preview?.Pages) is { } pairs
            && mod.GetSecondaryDialogueFont() is { } font
            && pairs.All(pair => font.Supports(pair.Secondary)))
        {
            int bodyWidth = box.isPortraitBox() ? width : width - 16;
            var layout = DialogueInterleavedLayout.Fit(pairs, bodyWidth, box.height - 16,
                preview?.SecondaryFirst ?? mod.Config.SecondaryFirst,
                primary => SpriteText.getHeightOfString(primary, bodyWidth - 4),
                line => font.Measure(line).X * DialoguePairLayout.SecondaryScale,
                line => font.Measure(line).Y * DialoguePairLayout.SecondaryScale);
            if (layout != null)
            {
                int visiblePosition = DialoguePairLayout.GetVisiblePosition(position, text.Length);
                foreach (var row in layout.Rows)
                {
                    int revealed = row.RevealedCharacters(visiblePosition);
                    if (revealed > 0)
                        SpriteText.drawString(batch, row.Primary, x, y + row.PrimaryOffset, revealed, bodyWidth, height,
                            alpha, depth, junimo, background, placeholder, color, alignment);
                    if (row.Secondary.Length > 0 && visiblePosition >= row.RevealAt)
                        font.Draw(batch, row.Secondary, new Vector2(x, y + row.SecondaryOffset),
                            Game1.textColor * alpha, DialoguePairLayout.SecondaryScale, depth);
                }
                return;
            }
        }

        SpriteText.drawString(batch, text, x, y, position, width, height,
            alpha, depth, junimo, background, placeholder, color, alignment);
    }

    private static DialogueTextPair[]? ResolvePairs(
        ModEntry mod, Dialogue dialogue, string text, DialogueTextPair[][]? previewPages)
    {
        string prefix = ModEntry.GetDialogueSpeakerPrefix(dialogue);
        if (!text.StartsWith(prefix, StringComparison.Ordinal))
            return null;

        string body = text[prefix.Length..];
        var pairs = previewPages == null
            ? mod.GetDialoguePairs(dialogue, body)
            : previewPages.ElementAtOrDefault(dialogue.currentDialogueIndex);
        if (pairs == null || !string.Equals(string.Join(" ", pairs.Select(pair => pair.Primary)), body, StringComparison.Ordinal))
            return null;

        return DialoguePairLayout.PreparePairs(pairs, prefix);
    }
}
