using System.Reflection;
using HarmonyLib;
using StardewValley;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Patches;

/// <summary>Bounds formatted-string provenance to one vanilla NPC dialogue construction.</summary>
[HarmonyPatch(typeof(NPC), "loadCurrentDialogue")]
internal static class GiftAdviceCapturePatch
{
    internal sealed class Scope
    {
        internal readonly NPC Speaker;
        internal readonly List<GiftAdviceTranslation.Piece> Pieces = new();
        internal Scope(NPC speaker) { Speaker = speaker; }
    }
    [ThreadStatic] internal static Scope? Current;
    private static void Prefix(NPC __instance, out Scope? __state)
    { __state = Current; Current = new(__instance); }
    private static Exception? Finalizer(Exception? __exception, Scope? __state)
    { Current = __state; return __exception; }

    internal static GiftAdviceTranslation.Advice? Resolve(NPC speaker, string raw)
    {
        if (Current is not { } scope || !ReferenceEquals(scope.Speaker, speaker)) return null;
        const string token = "%revealtaste:";
        int at = raw.IndexOf(token, StringComparison.Ordinal);
        if (at < 0) return null;
        var args = raw[(at + token.Length)..].Split(':', 2);
        if (args.Length != 2) return null;
        string name = NPC.GetDisplayName(args[0]);
        string? item = ItemRegistry.GetData(args[1])?.DisplayName;
        return item == null ? null : GiftAdviceTranslation.Match(raw, scope.Pieces, name, item);
    }
}

[HarmonyPatch]
internal static class GiftAdviceFormattedStringPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(LocalizedContentManager).GetMethods()
        .Where(m => m.Name == nameof(LocalizedContentManager.LoadString) && m.GetParameters().Length >= 1);
    private static void Postfix(LocalizedContentManager __instance, object[] __args, string __result)
    {
        if (!ReferenceEquals(__instance, Game1.content) || GiftAdviceCapturePatch.Current is not { } scope
            || __args[0] is not string path || !GiftAdviceTranslation.IsAdvicePath(path)) return;
        object[] args = __args.Skip(1).SelectMany(arg => arg is object[] many ? many : new[] { arg }).ToArray();
        // Discard the unformatted inner LoadString call when an overload formats it.
        if (scope.Pieces.LastOrDefault() is { } previous && previous.Path == path.Replace('\\', '/')
            && previous.Arguments.Length == 0 && args.Length > 0) scope.Pieces.RemoveAt(scope.Pieces.Count - 1);
        scope.Pieces.Add(new(path.Replace('\\', '/'), __result, args));
    }
}

[HarmonyPatch]
internal static class GiftAdviceGenderStringPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(Game1).GetMethods()
        .Where(m => m.Name == nameof(Game1.LoadStringByGender));
    private static void Prefix(out int __state) => __state = GiftAdviceCapturePatch.Current?.Pieces.Count ?? 0;
    private static void Postfix(Gender npcGender, string key, object[] __args, string __result, int __state)
    {
        if (GiftAdviceCapturePatch.Current is not { } scope || !GiftAdviceTranslation.IsAdvicePath(key)) return;
        if (scope.Pieces.Count > __state) scope.Pieces.RemoveRange(__state, scope.Pieces.Count - __state);
        scope.Pieces.Add(new(key.Replace('\\', '/'), __result,
            __args.Length > 2 && __args[2] is object[] args ? args.ToArray() : Array.Empty<object>(), npcGender));
    }
}
