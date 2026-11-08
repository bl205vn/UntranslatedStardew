using System.Runtime.CompilerServices;
using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>Exact strings returned by the active festival's year-aware data lookup.</summary>
internal static class FestivalDialogueSource
{
    private sealed record Entry(WeakReference<Event> Owner, string Path, int Generation);
    private static readonly ConditionalWeakTable<string, Entry> Sources = new();
    internal static void Capture(Event owner, string asset, string key, string text)
    {
        if (!EventDialogueTranslation.TryParseFestivalKey(asset + ":" + key, out string normalized, out string field)) return;
        Sources.Remove(text);
        Sources.Add(text, new(new(owner), normalized + ":" + field, LoadedDialogueSource.Generation));
    }
    internal static string? Get(string text, Event? owner) => owner is { isFestival: true, skipped: false }
        && Sources.TryGetValue(text, out var source) && source.Generation == LoadedDialogueSource.Generation
        && source.Owner.TryGetTarget(out var captured) && ReferenceEquals(captured, owner) ? source.Path : null;
}
