using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewValley;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>Remembers the source of a branch only after vanilla actually replaces its command array.</summary>
[HarmonyPatch]
internal static class EventScriptSourcePatch
{
    private sealed record Source(string Asset, string Key, string[] Commands, int Generation);
    private static readonly ConditionalWeakTable<Event, Source> Sources = new();
    private static IEnumerable<MethodBase> TargetMethods() => new[]
    {
        AccessTools.Method(typeof(Event.DefaultCommands), nameof(Event.DefaultCommands.Fork)),
        AccessTools.Method(typeof(Event.DefaultCommands), nameof(Event.DefaultCommands.SwitchEvent))
    };
    private static void Prefix(Event @event, out string[] __state) => __state = @event.eventCommands;
    private static void Postfix(Event @event, string[] args, string[] __state, MethodBase __originalMethod)
    {
        if (ReferenceEquals(__state, @event.eventCommands)) return;
        Sources.Remove(@event);
        if (args.Length < 2) return;
        bool fork = __originalMethod.Name == nameof(Event.DefaultCommands.Fork);
        string key = fork && args.Length >= 3 ? args[2] : args[1];
        bool translatedKey = fork && args.Length >= 4 && bool.TryParse(args[3], out bool value) && value;
        try
        {
            string asset;
            if (translatedKey)
            {
                int separator = key.IndexOf(':');
                if (separator <= 0) return;
                asset = key[..separator].Replace('\\', '/');
                key = key[(separator + 1)..];
                if (!EventDialogueTranslation.TryNormalizeAsset(asset, out _, @event.isFestival)
                    && !DialogueTranslation.TryParseSourceKey(asset + ":" + key, out _, out _)) return;
            }
            else if (@event.isFestival)
            {
                asset = (string?)AccessTools.Field(typeof(Event), "festivalDataAssetName").GetValue(@event) ?? "";
                if (!@event.TryGetFestivalDataForYear(key, out _, out string actualKey)) return;
                key = actualKey;
            }
            else asset = "Data/Events/" + Game1.currentLocation.Name;
            Capture(@event, asset, key);
        }
        catch (Exception ex)
        { ModEntry.Instance?.Monitor.Log($"Event branch source kept vanilla: {ex.Message}", StardewModdingAPI.LogLevel.Trace); }
    }
    internal static void Capture(Event owner, string asset, string key)
    {
        Sources.Remove(owner);
        Sources.Add(owner, new(asset.Replace('\\', '/'), key, owner.eventCommands, LoadedDialogueSource.Generation));
    }
    internal static (string Asset, string Key)? Get(Event owner) => !owner.skipped && Sources.TryGetValue(owner, out var source)
        && source.Generation == LoadedDialogueSource.Generation && ReferenceEquals(source.Commands, owner.eventCommands)
        ? (source.Asset, source.Key) : null;
}
