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

[HarmonyPatch]
internal static class DialogueQuestionPatch
{
    private sealed record Snapshot(SpriteFont Font, int Width, int ViewportHeight, bool Above,
        string Prompt, (string Key, string Text)[] Answers, Dictionary<string, DialogueQuestionLayout.Row> Rows);
    private sealed record Preview(DialogueQuestionTranslation.Question Question, bool Above);
    private static readonly ConditionalWeakTable<DialogueBox, Snapshot> Layouts = new();
    private static readonly ConditionalWeakTable<DialogueBox, Preview> Previews = new();
    private static readonly MethodInfo Setup = AccessTools.Method(typeof(DialogueBox), "setUpQuestions");
    private static readonly FieldInfo ShowedOptions = AccessTools.Field(typeof(DialogueBox), "_showedOptions");

    internal static bool RegisterPreview(DialogueBox box, DialogueQuestionTranslation.Question question, bool above)
    {
        Previews.Add(box, new(question, above));
        Setup.Invoke(box, null);
        return Layouts.TryGetValue(box, out _);
    }

    private static IEnumerable<MethodBase> TargetMethods()
        => new[] { Setup, AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.draw), new[] { typeof(SpriteBatch) }),
            AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.update)),
            AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.performHoverAction)) };

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var measure = AccessTools.Method(typeof(SpriteText), nameof(SpriteText.getHeightOfString));
        foreach (var call in code.Where(i => i.Calls(measure)).ToArray())
        {
            var box = new CodeInstruction(OpCodes.Ldarg_0);
            box.labels.AddRange(call.labels);
            call.labels.Clear();
            code.Insert(code.IndexOf(call), box);
            call.operand = AccessTools.Method(typeof(DialogueQuestionPatch), nameof(Measure));
        }
        return code;
    }

    // On resize or a changed question, refresh geometry before drawing or processing input.
    private static void Prefix(DialogueBox __instance, MethodBase __originalMethod)
    {
        if (__originalMethod.Name == "setUpQuestions")
        {
            BuildLayout(__instance);
            return;
        }
        if (__instance.isQuestion && Layouts.TryGetValue(__instance, out var old)
            && (old.Width != __instance.width - 16 || old.ViewportHeight != Game1.uiViewport.Height
                || old.Prompt != __instance.getCurrentString()
                || !old.Answers.SequenceEqual(__instance.responses.Select(r => (r.responseKey, r.responseText)))))
        {
            if (old.ViewportHeight != Game1.uiViewport.Height)
                __instance.y = Game1.uiViewport.Height - __instance.height - 64;
            Setup.Invoke(__instance, null);
            ShowedOptions.SetValue(__instance, false);
            __instance.responseCC?.Clear();
        }
    }

    private static void BuildLayout(DialogueBox box)
    {
        Layouts.Remove(box);
        ModEntry? mod = ModEntry.Instance;
        bool preview = Previews.TryGetValue(box, out var sample);
        if (mod == null || (!preview && !mod.Config.ShowSecondaryInDialogue) || !box.isQuestion
            || Game1.eventUp || mod.Helper.ModRegistry.IsLoaded("Mangupix.DialogueDisplayFrameworkContinued")
            || mod.FontManager?.GetSecondaryFont(Game1.dialogueFont) is not { } font)
            return;
        string shown = box.getCurrentString();
        string prefix = box.characterDialogue is { } dialogue ? ModEntry.GetDialogueSpeakerPrefix(dialogue) : "";
        if (!shown.StartsWith(prefix, StringComparison.Ordinal))
            return;
        var question = preview ? sample!.Question
            : box.characterDialogue is { } d && box.characterDialoguesBrokenUp.Count == 1
                ? mod.GetDialogueQuestion(d, shown[prefix.Length..], box.responses) : null;
        if (question == null)
            return;
        var pairs = new[] { question.Prompt with { Primary = shown } }.Concat(question.Answers)
            .Select(pair => pair with { Secondary = pair.Secondary.Normalize() }).ToArray();
        if (pairs.Any(p => !p.Secondary.Normalize().All(font.Characters.Contains)))
            return;
        int width = box.width - 16;
        bool above = preview ? sample!.Above : mod.Config.SecondaryFirst;
        // NPC boxes have bottom = viewport -64. Leave room above for the question icon and border.
        var rows = DialogueQuestionLayout.Fit(pairs, width, Game1.uiViewport.Height - 192, above,
            text => SpriteText.getHeightOfString(text, width),
            text => font.MeasureString(text).X * DialoguePairLayout.SecondaryScale,
            text => Math.Max(font.MeasureString(text).Y, (1 + text.Count(c => c == '\n')) * font.LineSpacing)
                * DialoguePairLayout.SecondaryScale);
        if (rows != null)
            Layouts.Add(box, new(font, width, Game1.uiViewport.Height, above, shown,
                box.responses.Select(r => (r.responseKey, r.responseText)).ToArray(), rows));
    }

    private static int Measure(string text, int width, DialogueBox box)
        => box.isQuestion && Layouts.TryGetValue(box, out var state) && state.Rows.TryGetValue(text, out var row)
            ? row.Height : SpriteText.getHeightOfString(text, width);

    internal static bool TryDraw(SpriteBatch batch, string text, int x, int y, int position,
        float alpha, float depth, DialogueBox box)
    {
        if (!box.isQuestion || !Layouts.TryGetValue(box, out var state) || !state.Rows.TryGetValue(text, out var row))
            return false;
        var pair = row.Pair;
        int visible = DialoguePairLayout.GetVisiblePosition(position, text.Length);
        SpriteText.drawString(batch, text, x, y + pair.PrimaryOffset, visible, state.Width,
            alpha: alpha, layerDepth: depth);
        if (visible >= text.Length)
            batch.DrawString(state.Font, pair.Secondary, new Vector2(x, y + pair.SecondaryOffset),
                Game1.textColor * alpha, 0, Vector2.Zero, DialoguePairLayout.SecondaryScale, SpriteEffects.None, depth);
        return true;
    }
}
