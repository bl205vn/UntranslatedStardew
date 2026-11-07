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

    private static void TestDialogueReportedCases()
    {
        Console.WriteLine("\n-- Reported NPC Dialogue & Question Cases (Caroline, Alex, Gus) --");

        // 1. Caroline Wed6: Multi-page dialogue separated by #$e#
        const string carolinePrimary = "Stardew Valley is nice, but I wish I knew more people.#$e#I'm glad that we've become friends.";
        const string carolineSecondary = "Thung lũng Stardew rất đẹp, nhưng cô ước gì được biết thêm nhiều người nữa.#$e#Cô mừng chúng ta đã trở thành bạn của nhau.";
        Assert(DialogueTranslation.GetSecondaryLine(carolinePrimary, carolineSecondary, 0,
                "Stardew Valley is nice, but I wish I knew more people.", "Farmer", Gender.Female, 0) == "Thung lũng Stardew rất đẹp, nhưng cô ước gì được biết thêm nhiều người nữa."
            && DialogueTranslation.GetSecondaryLine(carolinePrimary, carolineSecondary, 1,
                "I'm glad that we've become friends.", "Farmer", Gender.Female, 0) == "Cô mừng chúng ta đã trở thành bạn của nhau."
            && DialogueTranslation.GetSecondaryLine(carolinePrimary, carolineSecondary, 2, "Out of range.", "Farmer", Gender.Female, 0) == null
            && DialogueTranslation.GetSecondaryLine(carolinePrimary, carolineSecondary, 0, "Mismatched text.", "Farmer", Gender.Female, 0) == null,
            "Caroline Wed6 multi-page dialogue with $e break pairs both pages and preserves index bounds");

        // 2. Alex Wed: Pre-question narration with #$e#, gender switch in prompt, and 3 responses
        const string alexPrimary = "Looks like a good day to play catch, huh?$u#$e#$q 5/6 Wed_01_old#I'd ask you to throw the ball around, but you don't really seem like the sports type.^If you weren't a girl I'd ask you to play catch.#$r 6 0 Wed_01_02#I'm fine just watching you from a distance.#$r 5 15 Wed_01_01#I want to play catch with you!#$r 5 0 Wed_01_03#(angry) What's that supposed to mean?";
        const string alexSecondary = "Có vẻ là một ngày đẹp trời để chơi bóng ném nhỉ?$u#$e#$q 5/6 Wed_01_old#Tôi định nhờ cậu phát bóng, nhưng cậu có vẻ không phải dân thể thao.^Nếu cậu không phải con gái thì tôi đã rủ cậu chơi bóng ném rồi.#$r 6 0 Wed_01_02#Tôi xem cậu từ xa là được rồi.#$r 5 15 Wed_01_01#Tôi cũng muốn chơi!#$r 5 0 Wed_01_03#(giận dữ) Như thế nghĩa là sao?";
        var alexResponses = new (string Key, string Text)[]
        {
            ("Wed_01_02", "I'm fine just watching you from a distance."),
            ("Wed_01_01", "I want to play catch with you!"),
            ("Wed_01_03", "(angry) What's that supposed to mean?")
        };

        // Narration preceding the question
        Assert(DialogueTranslation.GetSecondaryLine(alexPrimary, alexSecondary, 0,
                "Looks like a good day to play catch, huh?", "Farmer", Gender.Female, 0) == "Có vẻ là một ngày đẹp trời để chơi bóng ném nhỉ?"
            && DialogueTranslation.GetSecondaryLine(alexPrimary, alexSecondary, 1, "Non-existent second narration.", "Farmer", Gender.Female, 0) == null,
            "Alex Wed narration preceding $q with $e break resolves correctly and does not treat $e as a dialogue page");

        // Question prompt & answers for Female player
        var alexFemaleQuestion = DialogueQuestionTranslation.Match(alexPrimary, alexSecondary,
            "If you weren't a girl I'd ask you to play catch.", alexResponses, "Farmer", Gender.Female, 0);
        Assert(alexFemaleQuestion != null
            && alexFemaleQuestion.Prompt.Secondary == "Nếu cậu không phải con gái thì tôi đã rủ cậu chơi bóng ném rồi."
            && alexFemaleQuestion.Answers.Length == 3
            && alexFemaleQuestion.Answers[0].Secondary == "Tôi xem cậu từ xa là được rồi."
            && alexFemaleQuestion.Answers[1].Secondary == "Tôi cũng muốn chơi!"
            && alexFemaleQuestion.Answers[2].Secondary == "(giận dữ) Như thế nghĩa là sao?",
            "Alex Wed question for female player resolves prompt and all three response labels");

        // Question prompt for Male player
        var alexMaleQuestion = DialogueQuestionTranslation.Match(alexPrimary, alexSecondary,
            "I'd ask you to throw the ball around, but you don't really seem like the sports type.", alexResponses, "Farmer", Gender.Male, 0);
        Assert(alexMaleQuestion != null
            && alexMaleQuestion.Prompt.Secondary == "Tôi định nhờ cậu phát bóng, nhưng cậu có vẻ không phải dân thể thao.",
            "Alex Wed question for male player resolves gender-appropriate prompt");

        // Question rejection on introduction structure mismatch ($e vs $b)
        Assert(DialogueQuestionTranslation.Match(alexPrimary, alexSecondary.Replace("#$e#$q", "#$b#$q"),
                "If you weren't a girl I'd ask you to play catch.", alexResponses, "Farmer", Gender.Female, 0) == null,
            "Alex question rejects mismatched introduction break types ($e vs $b)");

        // 3. Demetrius Introduction: Mixed #$e# and #$b# in a single dialogue
        const string demetriusPrimary = "Greetings! I'm Demetrius, local scientist and father. Thanks for introducing yourself!#$e#I'm studying the local plants and animals from my home laboratory.#$b#Have you met my daughter Maru? She's interested to meet you.";
        const string demetriusSecondary = "Xin chào! Chú là Demetrius, một nhà khoa học địa phương và cũng là một người cha. Cảm ơn vì đã tới chào hỏi!#$e#Chú đang nghiên cứu hệ thực vật và động vật địa phương từ phòng thí nghiệm tại gia.#$b#Cháu đã gặp con gái Maru của chú chưa? Con bé rất muốn gặp cháu đấy.";
        Assert(DialogueTranslation.GetSecondaryLine(demetriusPrimary, demetriusSecondary, 0,
                "Greetings! I'm Demetrius, local scientist and father. Thanks for introducing yourself!", "Farmer", Gender.Male, 0) != null
            && DialogueTranslation.GetSecondaryLine(demetriusPrimary, demetriusSecondary, 1,
                "I'm studying the local plants and animals from my home laboratory.", "Farmer", Gender.Male, 0) != null
            && DialogueTranslation.GetSecondaryLine(demetriusPrimary, demetriusSecondary, 2,
                "Have you met my daughter Maru? She's interested to meet you.", "Farmer", Gender.Male, 0) != null,
            "Mixed $e and $b breaks in a single dialogue resolve all pages at their proper logical indices");

        // 4. Gus gift advice: Parameterized template runtime safety fallback
        const string gusPrimaryTemplate = "If you want {0} to like you, give her '{1}'.";
        const string gusSecondaryTemplate = "Nếu muốn được {0} thích, hãy tặng '{1}'.";
        Assert(DialogueTranslation.GetSecondaryLine(gusPrimaryTemplate, gusSecondaryTemplate, 0,
                "If you want Pam to like you, give her 'Beer'.", "Farmer", Gender.Male, 0) == null
            && DialogueTranslation.GetSecondaryLine(gusPrimaryTemplate, gusSecondaryTemplate, 0,
                "If you want Pam to like you, give her 'Beer'.%revealtaste:Pam:(O)346", "Farmer", Gender.Male, 0) == null,
            "Parameterized dialogue with substituted arguments safely falls back to primary-only without crashing or mispairing");
    }
}
