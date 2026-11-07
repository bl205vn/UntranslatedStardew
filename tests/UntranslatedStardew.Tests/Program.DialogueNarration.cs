using System.Reflection;
using StardewValley;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static void TestDialogueNarration()
    {
        var method = typeof(RawContentProvider).Assembly.GetType("UntranslatedStardew.Core.DialogueTranslation", true)!
            .GetMethod("GetSecondaryLine", BindingFlags.Static | BindingFlags.NonPublic)!;
        string main = "Hello @.$h#$b#One more thing.#$q -1 null#A question?" +
            "#$r Yes 0 accept#Yes please.#$r No 0 refuse#No thanks.";
        string sub = "Chào @.$1#$b#Còn một chuyện.#$q -1 null#Một câu hỏi?" +
            "#$r Yes 0 accept#Vâng ạ.#$r No 0 refuse#Không cảm ơn.";
        string? Resolve(string p, string s, int index, string visible, uint days = 0)
            => (string?)method.Invoke(null, new object?[] { p, s, index, visible, "Alex", Gender.Male, days, null });
        Assert(Resolve(main, sub, 0, "Hello Alex.") == "Chào Alex."
            && Resolve(main, sub, 1, "One more thing.") == "Còn một chuyện.",
            "Pre-question narration preserves authored indices, player name and portrait suffix");
        Assert(Resolve(main, sub, 2, "A question?") == null
            && Resolve(main, sub, 0, "A different live text.") == null,
            "Narration never consumes the question prompt or matches stale live text");
        Assert(Resolve(main, sub.Replace("$r Yes 0 accept", "$r Yes 5 accept"), 0, "Hello Alex.") == null
            && Resolve(main, sub.Replace("Vâng ạ.", "$action AddMoney 1"), 0, "Hello Alex.") == null,
            "Narration rejects mismatched question headers and unsafe later answers");
        Assert(Resolve(main + "||" + main.Replace("One more thing.", "Second week."),
                sub + "||" + sub.Replace("Còn một chuyện.", "Tuần thứ hai."), 1, "Second week.", 7) == "Tuần thứ hai."
            && Resolve(main + "||" + main, sub, 0, "Hello Alex.") == null,
            "Narration uses the same weekly variant in both sources and rejects unequal variant counts");
    }
}
