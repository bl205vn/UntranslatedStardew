using System.Runtime.CompilerServices;

namespace UntranslatedStardew.Core;

/// <summary>Keeps exact string-instance provenance; equal text from unknown sources is never guessed.</summary>
internal static class LoadedDialogueSource
{
    private sealed class Entry
    {
        internal string? Path;
        internal Entry(string path) { Path = path; }
    }
    private static readonly object Gate = new();
    private static readonly ConditionalWeakTable<string, Entry> Sources = new();
    internal static int Generation { get; private set; }

    internal static void Capture(string? text, string path)
    {
        if (text == null) return;
        string? source = DialogueTranslation.TryParseSourceKey(path, out string asset, out string key) ? asset + ":" + key : null;
        lock (Gate)
        {
            if (Sources.TryGetValue(text, out var existing))
            {
                if (existing.Path != source) existing.Path = null;
            }
            else if (source != null) Sources.Add(text, new(source));
        }
    }

    internal static string? Get(string text)
    {
        lock (Gate) return Sources.TryGetValue(text, out var value) ? value.Path : null;
    }

    internal static void Invalidate()
    {
        lock (Gate) { Sources.Clear(); Generation++; }
    }
}
