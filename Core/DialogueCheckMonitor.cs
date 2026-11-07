using System.Diagnostics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>Observes diagnostic commands without restoring or changing world state.</summary>
internal sealed class DialogueCheckMonitor : IDisposable
{
    internal readonly record struct State(string Location, string PlayerLocation, float X, float Y,
        string PendingWarp, bool Event, bool Fade, string Menu, bool CanMove, bool PendingDialogue, int EventIdentity);
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private State before;
    private State last;
    private long started;
    private bool observing;

    internal DialogueCheckMonitor(IModHelper helper, IMonitor monitor)
    {
        this.helper = helper;
        this.monitor = monitor;
    }

    internal static string? GetUnavailableReason()
    {
        if (!Context.IsWorldReady) return "load a save first";
        if (Game1.eventUp || Game1.currentLocation?.currentEvent != null) return "an event is active";
        if (Game1.locationRequest != null || Game1.warpingForForcedRemoteEvent) return "a warp is pending";
        if (Game1.fadeToBlack || Game1.globalFade) return "a screen transition is active";
        if (Game1.currentMinigame != null || Game1.activeClickableMenu != null) return "close the minigame or menu first";
        if (Game1.afterDialogues != null || Game1.currentObjectDialogue.Count > 0) return "dialogue continuation is pending";
        return Context.CanPlayerMove ? null : "wait until the player can move normally";
    }

    private static State Capture() => new(Game1.currentLocation?.NameOrUniqueName ?? "",
        Game1.player?.currentLocation?.NameOrUniqueName ?? "", Game1.player?.Position.X ?? 0,
        Game1.player?.Position.Y ?? 0, Game1.locationRequest?.Name ?? "", Game1.eventUp,
        Game1.fadeToBlack || Game1.globalFade, Game1.activeClickableMenu?.GetType().Name ?? "", Game1.player?.CanMove ?? false,
        Game1.afterDialogues != null || Game1.currentObjectDialogue.Count > 0,
        Game1.currentLocation?.currentEvent is { } activeEvent ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(activeEvent) : 0);

    internal static bool TransitionChanged(State a, State b) => a.Location != b.Location
        || a.PlayerLocation != b.PlayerLocation || a.PendingWarp != b.PendingWarp || a.Event != b.Event
        || a.Fade != b.Fade || a.Menu != b.Menu || a.CanMove != b.CanMove
        || a.PendingDialogue != b.PendingDialogue || a.EventIdentity != b.EventIdentity;

    internal void Begin()
    {
        Dispose();
        before = last = Capture();
        started = Stopwatch.GetTimestamp();
        monitor.Log($"[Dialogue check state] before: {before}", LogLevel.Debug);
        observing = true;
        helper.Events.GameLoop.UpdateTicked += OnTick;
    }

    internal void EndImmediate()
    {
        last = Capture();
        monitor.Log($"[Dialogue check state] immediate after: {last}; unchanged={before == last}",
            before == last ? LogLevel.Debug : LogLevel.Warn);
    }

    private void OnTick(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady) { Dispose(); return; }
        State current = Capture();
        if (TransitionChanged(last, current))
            monitor.Log($"[Dialogue check state] observed after command: {current}. Timing alone does not establish causation.", LogLevel.Debug);
        last = current;
        if ((Stopwatch.GetTimestamp() - started) / (double)Stopwatch.Frequency >= 5) Dispose();
    }

    public void Dispose()
    {
        if (observing) helper.Events.GameLoop.UpdateTicked -= OnTick;
        observing = false;
    }
}
