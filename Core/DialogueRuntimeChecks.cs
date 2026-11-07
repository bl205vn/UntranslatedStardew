using HarmonyLib;
using StardewValley;
using UntranslatedStardew.Patches;

namespace UntranslatedStardew.Core;

/// <summary>Read-only smoke checks of installed Harmony boundaries; requires a loaded save.</summary>
internal static class DialogueRuntimeChecks
{
    internal static (string Name, bool Passed)[] RunCopies()
    {
        var actor = new NPC { Name = "UntranslatedCheck", displayName = "Check", EventActor = true };
        var source = new Dialogue(actor, "Strings/Characters:PreviewOriginal", "Hello.");
        bool initial = DialogueSourcePatch.GetKey(new Dialogue(source)) == "Strings/Characters:PreviewOriginal";
        var parse = AccessTools.Method(typeof(Dialogue), "parseDialogueString");
        parse.Invoke(source, new object[] { "Reaction.", "Strings/Characters:PreviewReaction" });
        var copy = new Dialogue(source);
        bool reaction = copy.TranslationKey == "Strings/Characters:PreviewOriginal"
            && DialogueSourcePatch.GetKey(copy) == "Strings/Characters:PreviewReaction";
        parse.Invoke(source, new object?[] { "Unkeyed.", null });
        bool cleared = DialogueSourcePatch.GetKey(new Dialogue(source)) == null;
        return new[]
        {
            ("A real Dialogue copy preserves its original source", initial),
            ("A real Dialogue copy keeps a reparsed reaction source over its old key", reaction),
            ("A real Dialogue copy preserves an explicitly cleared source", cleared)
        };
    }
}
