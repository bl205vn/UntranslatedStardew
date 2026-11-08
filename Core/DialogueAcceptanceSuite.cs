using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace UntranslatedStardew.Core;

/// <summary>Runs isolated existing previews in sequence, without borrowing the game's continuation queue.</summary>
internal sealed class DialogueAcceptanceSuite
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Action<string[]> openPreview;
    private DialogueBox? owned;
    private Farmer? player;
    private int next;
    private bool running;
    internal static string[][] Steps => new[]
    {
        new[] { "above", "wrap" }, new[] { "below", "wrap" },
        new[] { "above", "question" }, new[] { "below", "question" }
    };

    internal DialogueAcceptanceSuite(IModHelper helper, IMonitor monitor, Action<string[]> openPreview)
    { this.helper = helper; this.monitor = monitor; this.openPreview = openPreview; }

    internal void Command(string command, string[] args)
    {
        if (args.Length == 1 && args[0].Equals("stop", StringComparison.OrdinalIgnoreCase))
        {
            Stop("Preview sequence stopped; finish or close the current sample normally.");
            return;
        }
        if (args.Length != 0) { monitor.Log("Usage: untranslated_dialogue_suite [stop]", LogLevel.Info); return; }
        if (running) { monitor.Log("A preview sequence is already running.", LogLevel.Info); return; }
        if (DialogueCheckMonitor.GetUnavailableReason() is { } reason)
        { monitor.Log($"Preview sequence unavailable: {reason}.", LogLevel.Info); return; }
        player = Game1.player;
        next = 0;
        running = true;
        helper.Events.GameLoop.UpdateTicked += Tick;
        OpenNext();
    }

    private void Tick(object? sender, UpdateTickedEventArgs e)
    {
        if (!running) return;
        if (!Context.IsWorldReady || !ReferenceEquals(player, Game1.player) || Game1.eventUp
            || Game1.currentMinigame != null)
        { Stop("Preview sequence stopped because the game context changed."); return; }
        if (ReferenceEquals(Game1.activeClickableMenu, owned)) return;
        if (Game1.activeClickableMenu != null)
        { Stop("Preview sequence stopped because another menu opened."); return; }
        // Wait for vanilla to finish closing its own box and restore movement.
        if (DialogueCheckMonitor.GetUnavailableReason() != null) return;
        OpenNext();
    }

    private void OpenNext()
    {
        var steps = Steps;
        if (next >= steps.Length)
        { Stop("Preview sequence finished. Visual acceptance still needs your observation; no automatic UI PASS is claimed."); return; }
        var args = steps[next++];
        monitor.Log($"Preview sequence {next}/{steps.Length}: {args[1]}, secondary {args[0]}. Read all pages or choose an answer to continue.", LogLevel.Info);
        try { openPreview(args); }
        catch (Exception ex)
        { Stop($"Preview sequence stopped: {ex.Message}"); return; }
        if (Game1.activeClickableMenu is DialogueBox box) owned = box;
        else Stop("Preview sequence stopped: this language pair/font could not open the sample. See the preceding diagnostic.");
    }

    private void Stop(string message)
    {
        helper.Events.GameLoop.UpdateTicked -= Tick;
        running = false;
        owned = null;
        player = null;
        monitor.Log(message, LogLevel.Info);
    }
}
