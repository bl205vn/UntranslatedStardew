using System.Reflection;
using StardewValley;
using UntranslatedStardew.Core;

namespace UntranslatedStardew.Tests;

public static partial class Program
{
    private static void TestDialogueProgress()
    {
        var assembly = typeof(RawContentProvider).Assembly;
        var method = assembly.GetType("UntranslatedStardew.Core.DialogueQuestionTranslation", true)!
            .GetMethod("GetAnsweredFallbackLine", BindingFlags.Static | BindingFlags.NonPublic)!;
        const string main = "First.#$b#Second.#$q Yes/No old#A question?#$r Yes 0 accept#Yes please.#$r No 0 refuse#No thanks.";
        const string sub = "Đầu.#$b#Sau.#$q Yes/No old#Một câu hỏi?#$r Yes 0 accept#Vâng ạ.#$r No 0 refuse#Không cảm ơn.";
        (string, string)? fallback = ("Fallback one.$h#$b#Fallback two.", "Dự phòng một.$1#$b#Dự phòng hai.");
        int lookups = 0;
        (string, string)? Lookup(string key) { lookups++; return key == "old" ? fallback : null; }
        string? Resolve(string p, string s, int index, string visible, bool answered = true, uint days = 0, Gender gender = Gender.Male)
            => (string?)method.Invoke(null, new object[] { p, s, index, visible, "Alex", gender, days,
                (Func<string, bool>)(id => answered && id == "Yes"), (Func<string, (string, string)?>)Lookup });
        Assert(Resolve(main, sub, 2, "Fallback one.") == "Dự phòng một."
            && Resolve(main, sub, 3, "Fallback two.") == "Dự phòng hai.",
            "Answered $q fallback preserves narration offsets, authored pages and portrait suffixes");
        lookups = 0;
        Assert(Resolve(main, sub, 2, "Fallback one.", false) == null
            && Resolve(main.Replace("Yes/No old", "-1/Yes old"), sub.Replace("Yes/No old", "-1/Yes old"), 2, "Fallback one.") == null
            && Resolve(main.Replace(" old#", " null#"), sub.Replace(" old#", " null#"), 2, "Fallback one.") == null && lookups == 0,
            "Unanswered, repeatable and null-fallback questions never load a fallback");
        Assert(Resolve(main, sub.Replace("Yes/No old", "Yes/No wrong"), 2, "Fallback one.") == null
            && Resolve(main, sub, 1, "Fallback one.") == null
            && Resolve(main, sub, 2, "Wrong live text.") == null,
            "Question fallback requires matching headers, the correct page and exact live primary text");
        fallback = ("Hello.", "%item object 74 1 %%");
        Assert(Resolve(main, sub, 2, "Hello.") == null, "Fallback rejects secondary gift/action commands");
        fallback = ("Hello.||Goodbye.", "Chào.||Tạm biệt.");
        Assert(Resolve(main, sub, 2, "Hello.") == null, "Inlined fallback never re-evaluates a weekly variant");
        fallback = ("$q -1 null#Prompt?#$r -1 0 key#Yes.", "Chào.");
        Assert(Resolve(main, sub, 2, "Prompt?") == null, "Nested question fallback is rejected without recursion");
        fallback = ("Hello ${sir^madam}$.", "Chào ${anh^chị}$.");
        Assert(Resolve(main + "||" + main, sub + "||" + sub, 2, "Hello madam.", days: 7, gender: Gender.Female) == "Chào chị.",
            "Parent weekly selection and gender resolve safely in plain fallback text");
        fallback = ("$p Yes#Hello.|Hello.", "$p Yes#Đã chọn.|Chưa chọn.");
        Assert(Resolve(main, sub, 2, "Hello.") == "Đã chọn.",
            "Flat $p inside an answered question fallback uses primary answer history");
        fallback = null;
        Assert(Resolve(main, sub, 2, "Hello.") == null, "Missing strict fallback translation keeps vanilla text");

        var monitor = assembly.GetType("UntranslatedStardew.Core.DialogueCheckMonitor", true)!;
        var state = monitor.GetNestedType("State", BindingFlags.NonPublic)!;
        object State(float x, string location = "FarmHouse", bool activeEvent = false)
            => Activator.CreateInstance(state, new object[] { location, location, x, 20f, "", activeEvent, false, "", true, false, activeEvent ? 1 : 0 })!;
        var changed = monitor.GetMethod("TransitionChanged", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert(changed.Invoke(null, new[] { State(10), State(11) }) is false
            && changed.Invoke(null, new[] { State(10), State(10, "Farm") }) is true
            && changed.Invoke(null, new[] { State(10), State(10, activeEvent: true) }) is true,
            "Check observer distinguishes ordinary walking from a location or event transition");
    }
}
