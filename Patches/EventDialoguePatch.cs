using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>Attaches passive translations only to boxes just opened by validated event commands.</summary>
[HarmonyPatch]
internal static class EventDialoguePatch
{
    private sealed record Source(Event Owner, int Command, int Generation, EventDialogueTranslation.Text Text);
    private sealed record QuestionSource(Event Owner, int Command, int Generation, EventDialogueTranslation.QuestionText Question);

    private static readonly ConditionalWeakTable<DialogueBox, Source> Sources = new();
    private static readonly ConditionalWeakTable<DialogueBox, QuestionSource> QuestionSources = new();

    private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Event.DefaultCommands), nameof(Event.DefaultCommands.Speak));
        yield return AccessTools.Method(typeof(Event.DefaultCommands), nameof(Event.DefaultCommands.Message));
        yield return AccessTools.Method(typeof(Event.DefaultCommands), nameof(Event.DefaultCommands.Question));
        yield return AccessTools.Method(typeof(Event.DefaultCommands), nameof(Event.DefaultCommands.QuickQuestion));
    }

    private static void Prefix(out IClickableMenu? __state) => __state = Game1.activeClickableMenu;

    internal static void AttachFestivalMessage(DialogueBox box, Event owner)
    {
        Sources.Remove(box);
        Sources.Add(box, new(owner, -1, LoadedDialogueSource.Generation, new("", "", null)));
    }

    private static void Postfix(Event @event, string[] args, IClickableMenu? __state)
    {
        if (!Game1.eventUp || !ReferenceEquals(Game1.CurrentEvent, @event)
            || Game1.activeClickableMenu is not DialogueBox box || ReferenceEquals(box, __state)) return;
        try
        {
            if (box.isQuestion)
            {
                var q = ModEntry.Instance?.GetEventQuestionText(@event, args);
                if (q == null) return;
                QuestionSources.Remove(box);
                QuestionSources.Add(box, new(@event, @event.CurrentCommand, LoadedDialogueSource.Generation, q));
                DialogueQuestionPatch.RebuildSourceQuestion(box);
                return;
            }

            var text = ModEntry.Instance?.GetEventDialogueText(@event, args);
            if (text == null) return;
            if (text.Actor != null && box.characterDialogue?.speaker?.Name != text.Actor.TrimEnd('?')) return;
            Sources.Remove(box);
            Sources.Add(box, new(@event, @event.CurrentCommand, LoadedDialogueSource.Generation, text));
            if (text.Actor != null) NpcDialogueGeometryPatch.ReflowFreshPage(box);
            if (text.Actor == null)
            {
                var pages = ObjectDialogueTranslation.Match(text.Primary, text.Secondary, text.Primary, Game1.player.Gender);
                if (pages == null || pages.Length != box.dialogues.Count) { Sources.Remove(box); return; }
                for (int i = 0; i < pages.Length; i++)
                {
                    string visible = ObjectDialogueTranslation.VisibleText(box.dialogues[i]);
                    if (ObjectDialogueTranslation.VisibleText(Game1.parseText(pages[i].Primary)) != visible)
                    { Sources.Remove(box); return; }
                    pages[i] = new(visible, pages[i].Secondary);
                }
                ObjectDialoguePatch.Attach(box, pages);
            }
        }
        catch (Exception ex)
        {
            Sources.Remove(box);
            QuestionSources.Remove(box);
            ModEntry.Instance?.Monitor.Log($"Event text kept vanilla: {ex.Message}", StardewModdingAPI.LogLevel.Trace);
        }
    }

    internal static bool IsEligible(DialogueBox box) => Sources.TryGetValue(box, out var source)
        && source.Generation == LoadedDialogueSource.Generation && ReferenceEquals(Game1.CurrentEvent, source.Owner)
        && !source.Owner.skipped && (source.Command < 0 || source.Owner.CurrentCommand == source.Command);

    internal static bool IsQuestionEligible(DialogueBox box) => QuestionSources.TryGetValue(box, out var source)
        && source.Generation == LoadedDialogueSource.Generation && ReferenceEquals(Game1.CurrentEvent, source.Owner)
        && source.Owner.CurrentCommand == source.Command && !source.Owner.skipped;

    internal static DialogueQuestionTranslation.Question? ResolveQuestion(DialogueBox box, string shown)
    {
        if (!IsQuestionEligible(box) || !QuestionSources.TryGetValue(box, out var source)) return null;
        var q = source.Question;
        if (ObjectDialogueTranslation.VisibleText(q.Prompt.Primary) != shown || q.Answers.Length != box.responses.Length) return null;
        for (int i = 0; i < box.responses.Length; i++)
        {
            if (box.responses[i].responseKey != i.ToString(System.Globalization.CultureInfo.InvariantCulture)
                || box.responses[i].responseText != Dialogue.applyGenderSwitch(source.Owner.farmer.Gender, q.Answers[i].Primary, altTokenOnly: true)) return null;
        }
        return new DialogueQuestionTranslation.Question("event", new(shown, q.Prompt.Secondary),
            Array.Empty<string>(), box.responses.Select(r => r.responseKey).ToArray(),
            q.Answers.Select((a, i) => new DialogueTextPair(box.responses[i].responseText,
                Dialogue.applyGenderSwitch(source.Owner.farmer.Gender, a.Secondary, altTokenOnly: true))).ToArray());
    }

    internal static DialogueTextPair[]? GetNpcPairs(DialogueBox box, string displayed)
    {
        if (!IsEligible(box) || !Sources.TryGetValue(box, out var source)
            || source.Text.Actor == null || box.characterDialogue is not { } dialogue) return null;
        string prefix = ModEntry.GetDialogueSpeakerPrefix(dialogue);
        if (!displayed.StartsWith(prefix, StringComparison.Ordinal)) return null;
        string body = displayed[prefix.Length..];
        string? sub = DialogueTranslation.GetSecondaryLine(source.Text.Primary, source.Text.Secondary,
            DialogueSourcePatch.GetRelativeIndex(dialogue), body, Utility.FilterUserName(Game1.player.Name),
            Game1.player.Gender, Game1.stats.DaysPlayed, dialogue.farmer.DialogueQuestionsAnswered.Contains);
        return sub == null ? null : DialoguePairLayout.PreparePairs(new[] { new DialogueTextPair(body, sub) }, prefix);
    }

    [HarmonyPatch(typeof(DialogueBox), MethodType.Constructor, new[] { typeof(Dialogue) })]
    internal static class FestivalNpcDialoguePatch
    {
        private static void Postfix(DialogueBox __instance, Dialogue dialogue)
        {
            if (!Game1.eventUp || Game1.CurrentEvent is not { isFestival: true } @event || dialogue?.speaker == null) return;
            try
            {
                var mod = ModEntry.Instance;
                if (mod != null && mod.TryGetFestivalDialogueSources(dialogue, out string primary, out string secondary))
                {
                    Sources.Remove(__instance);
                    Sources.Add(__instance, new(@event, -1, LoadedDialogueSource.Generation, new(primary, secondary, dialogue.speaker.Name)));
                    NpcDialogueGeometryPatch.ReflowFreshPage(__instance);
                }
            }
            catch { }
        }
    }
}
