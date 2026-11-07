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
    private sealed record Snapshot(DialogueNativeFont Font, int Width, int ViewportHeight, bool Above,
        string Prompt, (string Key, string Text)[] Answers, Dictionary<string, DialogueInterleavedLayout.Page> Rows);
    private sealed record Preview(DialogueQuestionTranslation.Question Question, bool Above);
    private static readonly ConditionalWeakTable<DialogueBox, Snapshot> Layouts = new();
    private static readonly ConditionalWeakTable<DialogueBox, Preview> Previews = new();
    private static readonly MethodInfo Setup = AccessTools.Method(typeof(DialogueBox), "setUpQuestions");
    private static readonly FieldInfo ShowedOptions = AccessTools.Field(typeof(DialogueBox), "_showedOptions");

    internal static bool RegisterPreview(DialogueBox box, DialogueQuestionTranslation.Question question, bool above, out string? failure)
    {
        Previews.Add(box, new(question, above));
        failure = BuildLayout(box);
        if (failure != null)
            return false;
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
                || !ReferenceEquals(old.Font, ModEntry.Instance?.GetSecondaryDialogueFont())
                || old.Above != (Previews.TryGetValue(__instance, out var sample)
                    ? sample.Above : ModEntry.Instance?.Config.SecondaryFirst ?? false)
                || (!Previews.TryGetValue(__instance, out _) && ModEntry.Instance?.Config.ShowSecondaryInDialogue != true)
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

    private static string? BuildLayout(DialogueBox box)
    {
        Layouts.Remove(box);
        ModEntry? mod = ModEntry.Instance;
        bool preview = Previews.TryGetValue(box, out var sample);
        if (mod == null)
            return "mod instance is unavailable";
        if (!preview && !mod.Config.ShowSecondaryInDialogue)
            return "bilingual dialogue is disabled";
        if (!box.isQuestion)
            return "vanilla did not identify the sample as a question";
        if (Game1.eventUp)
            return "an event is active";
        if (mod.Helper.ModRegistry.IsLoaded("Mangupix.DialogueDisplayFrameworkContinued"))
            return "DDFC uses a different dialogue renderer";
        if (mod.GetSecondaryDialogueFont() is not { } font)
            return $"secondary font is unavailable for language '{mod.Config.SecondaryLanguage}'";
        string shown = box.getCurrentString();
        string prefix = box.characterDialogue is { } dialogue ? ModEntry.GetDialogueSpeakerPrefix(dialogue) : "";
        if (!shown.StartsWith(prefix, StringComparison.Ordinal))
            return "speaker prefix does not match the live prompt";
        var question = preview ? sample!.Question
            : box.characterDialogue is { } d && box.characterDialoguesBrokenUp.Count == 1
                ? mod.GetDialogueQuestion(d, shown[prefix.Length..], box.responses) : null;
        if (question == null)
            return "question source does not match the live prompt and answers";
        var pairs = new[] { question.Prompt with { Primary = shown } }.Concat(question.Answers)
            .Select(pair => pair with { Secondary = pair.Secondary.Normalize() }).ToArray();
        string missing = new string(pairs.SelectMany(p => p.Secondary).Where(c => !font.Supports(c.ToString())).Distinct().ToArray());
        if (missing.Length > 0)
            return $"secondary font '{mod.Config.SecondaryLanguage}' is missing "
                + string.Join(", ", missing.Take(12).Select(c => $"U+{(int)c:X4}"));
        int width = box.width - 16;
        bool above = preview ? sample!.Above : mod.Config.SecondaryFirst;
        // NPC boxes have bottom = viewport -64. Leave room above for the question icon and border.
        var rows = DialogueQuestionLayout.FitRows(pairs, width, Game1.uiViewport.Height - 192, above,
            text => SpriteText.getHeightOfString(text, width),
            text => font.Measure(text).X * DialoguePairLayout.SecondaryScale,
            text => font.Measure(text).Y * DialoguePairLayout.SecondaryScale, out string? failure);
        if (rows != null)
            Layouts.Add(box, new(font, width, Game1.uiViewport.Height, above, shown,
                box.responses.Select(r => (r.responseKey, r.responseText)).ToArray(), rows));
        return failure;
    }

    private static int Measure(string text, int width, DialogueBox box)
        => box.isQuestion && Layouts.TryGetValue(box, out var state) && state.Rows.TryGetValue(text, out var row)
            ? row.Height : SpriteText.getHeightOfString(text, width);

    internal static bool TryDraw(SpriteBatch batch, string text, int x, int y, int position,
        float alpha, float depth, DialogueBox box)
    {
        if (!box.isQuestion || !Layouts.TryGetValue(box, out var state) || !state.Rows.TryGetValue(text, out var row))
            return false;
        int visible = DialoguePairLayout.GetVisiblePosition(position, text.Length);
        foreach (var line in row.Rows)
        {
            int revealed = line.RevealedCharacters(visible);
            if (revealed > 0)
                SpriteText.drawString(batch, line.Primary, x, y + line.PrimaryOffset, revealed, state.Width,
                    alpha: alpha, layerDepth: depth);
            if (line.Secondary.Length > 0 && visible >= line.RevealAt)
                state.Font.Draw(batch, line.Secondary, new Vector2(x, y + line.SecondaryOffset),
                    Game1.textColor * alpha, DialoguePairLayout.SecondaryScale, depth);
        }
        return true;
    }
}
