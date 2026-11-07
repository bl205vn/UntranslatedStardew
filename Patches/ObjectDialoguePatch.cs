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
    private readonly record struct CacheKey(int Page, int Width, int Viewport, int Generation, bool Enabled, bool Above, DialogueNativeFont? Font);
    private sealed class State
    {
        internal readonly DialogueTextPair[] Pages;
        internal readonly int Generation = LoadedDialogueSource.Generation;
        internal CacheKey? Key;
        internal DialogueBlockLayout.Page? Layout;
        internal DialogueNativeFont? Font;
        internal State(DialogueTextPair[] pages) { Pages = pages; }
    }
    private static readonly ConditionalWeakTable<DialogueBox, State> States = new();

    internal static bool Attach(DialogueBox box, DialogueTextPair[] pages)
    {
        if (box == null || pages == null || pages.Length == 0) return false;
        States.Remove(box);
        States.Add(box, new(pages));
        Refresh(box);
        return true;
    }

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
        var font = enabled ? mod?.GetSecondaryDialogueFont() : null;
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
            || !font.Supports(pair.Secondary)) return;
        var layout = DialogueBlockLayout.Fit(new[] { pair }, width, Game1.uiViewport.Height - 144, above,
            text => SpriteText.getHeightOfString(text, width),
            text => font.Measure(text).X * DialoguePairLayout.SecondaryScale,
            text => font.Measure(text).Y * DialoguePairLayout.SecondaryScale);
        if (layout == null) return;
        SetHeight(box, layout.Height + 16);
        state.Layout = layout;
        state.Font = font;
    }

    internal static bool TryDraw(SpriteBatch batch, string text, int x, int y, int position,
        float alpha, float depth, DialogueBox box)
    {
        Refresh(box);
        if (!States.TryGetValue(box, out var state) || state.Layout is not { } page || state.Font == null
            || state.Key is not { } key || key.Page < 0 || text != state.Pages[key.Page].Primary) return false;
        int visible = DialoguePairLayout.GetVisiblePosition(position, text.Length);
        foreach (var row in page.Rows)
        {
            int revealed = row.RevealedCharacters(visible);
            if (revealed > 0)
                SpriteText.drawString(batch, row.Primary, x, y + row.PrimaryOffset, revealed, box.width - 16, alpha: alpha, layerDepth: depth);
            if (row.Secondary.Length > 0 && visible >= row.RevealAt)
                state.Font.Draw(batch, row.Secondary, new Vector2(x, y + row.SecondaryOffset), Game1.textColor * alpha,
                    DialoguePairLayout.SecondaryScale, depth);
        }
        return true;
    }
}
