using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>Resizes and renders plain object messages without changing their page or input lifecycle.</summary>
[HarmonyPatch]
internal static class ObjectDialoguePatch
{
    private readonly record struct CacheKey(int Page, int Width, int Viewport, int Generation, bool Enabled, bool Above, SpriteFont? Font);
    private sealed class State
    {
        internal readonly DialogueTextPair[] Pages;
        internal readonly int Generation = LoadedDialogueSource.Generation;
        internal CacheKey? Key;
        internal DialoguePairLayout.PositionedPair? Layout;
        internal SpriteFont? Font;
        internal State(DialogueTextPair[] pages) { Pages = pages; }
    }
    private static readonly ConditionalWeakTable<DialogueBox, State> States = new();

    private static IEnumerable<MethodBase> TargetMethods() => new MethodBase[]
    {
        AccessTools.Constructor(typeof(DialogueBox), new[] { typeof(string) }),
        AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.receiveLeftClick)),
        AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.update)),
        AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.draw), new[] { typeof(SpriteBatch) })
    };

    private static void Postfix(DialogueBox __instance, MethodBase __originalMethod, object[] __args)
    {
        if (__originalMethod.IsConstructor && __args[0] is string text
            && LoadedDialogueSource.Get(text) is { } source
            && ModEntry.Instance?.GetObjectDialoguePages(source, text) is { } pages)
            States.Add(__instance, new(pages));
        // Input handlers finish changing the page and vanilla geometry before our refresh.
        Refresh(__instance);
    }

    private static void Prefix(DialogueBox __instance, MethodBase __originalMethod)
    {
        if (!__originalMethod.IsConstructor && __originalMethod.Name != nameof(DialogueBox.receiveLeftClick)) Refresh(__instance);
    }

    private static void SetHeight(DialogueBox box, int height)
    {
        box.height = height;
        box.y = Game1.uiViewport.Height - height - 64;
    }

    private static void Refresh(DialogueBox box)
    {
        if (!States.TryGetValue(box, out var state) || box.characterDialogue != null || box.isQuestion) return;
        var mod = ModEntry.Instance;
        int index = ObjectDialogueTranslation.CurrentPage(state.Pages, box.dialogues);
        bool enabled = mod?.Config.ShowSecondaryInDialogue == true && !Game1.eventUp
            && !mod.Helper.ModRegistry.IsLoaded("Mangupix.DialogueDisplayFrameworkContinued")
            && state.Generation == LoadedDialogueSource.Generation;
        var font = enabled ? mod?.FontManager?.GetSecondaryFont(Game1.dialogueFont) : null;
        bool above = mod?.Config.SecondaryFirst == true;
        var key = new CacheKey(index, box.width, Game1.uiViewport.Height, LoadedDialogueSource.Generation, enabled, above, font);
        if (state.Key == key) return;
        bool wasBilingual = state.Layout != null;
        state.Key = key;
        state.Layout = null;
        state.Font = null;
        if (index < 0)
        {
            if (wasBilingual && box.dialogues.Count > 0)
                SetHeight(box, SpriteText.getHeightOfString(box.getCurrentString(), box.width - 16) + 4);
            return;
        }
        var pair = state.Pages[index];
        int width = box.width - 16;
        int primaryHeight = SpriteText.getHeightOfString(pair.Primary, width);
        if (wasBilingual) SetHeight(box, primaryHeight + 4);
        if (!enabled || font == null || string.IsNullOrWhiteSpace(pair.Primary)
            || string.IsNullOrWhiteSpace(pair.Secondary) || pair.Primary == pair.Secondary
            || pair.Secondary.Any(c => c != '\n' && !font.Characters.Contains(c))) return;
        float SecondaryHeight(string text) => Math.Max(font.MeasureString(text).Y,
            (1 + text.Count(c => c == '\n')) * font.LineSpacing) * DialoguePairLayout.SecondaryScale;
        var layout = DialoguePairLayout.FitPage(new[] { pair }, width, Game1.uiViewport.Height - 144, above,
            _ => primaryHeight, text => font.MeasureString(text).X * DialoguePairLayout.SecondaryScale, SecondaryHeight);
        if (layout == null) return;
        var row = layout[0];
        int height = Math.Max(row.PrimaryOffset + primaryHeight, row.SecondaryOffset + (int)Math.Ceiling(SecondaryHeight(row.Secondary))) + 16;
        SetHeight(box, height);
        state.Layout = row;
        state.Font = font;
    }

    internal static bool TryDraw(SpriteBatch batch, string text, int x, int y, int position,
        float alpha, float depth, DialogueBox box)
    {
        Refresh(box);
        if (!States.TryGetValue(box, out var state) || state.Layout is not { } pair
            || state.Font == null || text != pair.Primary) return false;
        int visible = DialoguePairLayout.GetVisiblePosition(position, text.Length);
        SpriteText.drawString(batch, text, x, y + pair.PrimaryOffset, visible, box.width - 16, alpha: alpha, layerDepth: depth);
        if (visible >= text.Length)
            batch.DrawString(state.Font, pair.Secondary, new Vector2(x, y + pair.SecondaryOffset), Game1.textColor * alpha,
                0, Vector2.Zero, DialoguePairLayout.SecondaryScale, SpriteEffects.None, depth);
        return true;
    }
}
