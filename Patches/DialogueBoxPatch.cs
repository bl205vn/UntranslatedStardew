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

        foreach (CodeInstruction bodyCall in calls.Skip(2))
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
        ModEntry? mod = ModEntry.Instance;
        var preview = mod?.GetDialoguePreview(box);
        if (mod != null && (preview.HasValue || mod.Config.ShowSecondaryInDialogue)
            && !Game1.eventUp && !box.isQuestion
            && (box.isPortraitBox() || !Game1.options.showPortraits)
            && !mod.Helper.ModRegistry.IsLoaded("Mangupix.DialogueDisplayFrameworkContinued")
            && box.characterDialoguesBrokenUp.Count == 1
            && box.characterDialogue is { } dialogue
            && ResolvePairs(mod, dialogue, text, preview?.Pages) is { } pairs
            && mod.FontManager?.GetSecondaryFont(Game1.dialogueFont) is { } font
            && pairs.All(pair => pair.Secondary.All(font.Characters.Contains)))
        {
            int bodyWidth = box.isPortraitBox() ? width : width - 16;
            DialoguePairLayout.PositionedPair[]? Fit(IReadOnlyList<DialogueTextPair> page)
                => DialoguePairLayout.FitPage(page, bodyWidth, box.height - 16,
                    preview?.SecondaryFirst ?? mod.Config.SecondaryFirst,
                    primary => SpriteText.getHeightOfString(primary, bodyWidth - 4),
                    line => font.MeasureString(line).X * DialoguePairLayout.SecondaryScale,
                    lines => Math.Max(font.MeasureString(lines).Y,
                        (1 + lines.Count(c => c == '\n')) * font.LineSpacing) * DialoguePairLayout.SecondaryScale);

            var layout = Fit(pairs);
            if (layout == null && pairs.Length > 1)
                layout = Fit(new[] { new DialogueTextPair(text, string.Join(" ", pairs.Select(pair => pair.Secondary))) });

            if (layout != null)
            {
                int visiblePosition = DialoguePairLayout.GetVisiblePosition(position, text.Length);
                foreach (var pair in layout)
                {
                    int revealed = pair.RevealedCharacters(visiblePosition);
                    if (revealed == 0)
                        break;

                    SpriteText.drawString(batch, pair.Primary, x, y + pair.PrimaryOffset, revealed, bodyWidth, height,
                        alpha, depth, junimo, background, placeholder, color, alignment);

                    if (revealed == pair.Primary.Length)
                    {
                        batch.DrawString(font, pair.Secondary, new Vector2(x, y + pair.SecondaryOffset),
                            Game1.textColor, 0f, Vector2.Zero, DialoguePairLayout.SecondaryScale, SpriteEffects.None, depth);
                    }
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
