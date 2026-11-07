namespace UntranslatedStardew.Core;

/// <summary>One measured row serves both question drawing and vanilla input geometry.</summary>
internal static class DialogueQuestionLayout
{
    internal sealed record Row(DialoguePairLayout.PositionedPair Pair, int Height);

    internal static Dictionary<string, Row>? Fit(IReadOnlyList<DialogueTextPair> pairs, int width, int availableHeight,
        bool above, Func<string, int> primaryHeight, Func<string, float> secondaryWidth, Func<string, float> secondaryHeight)
    {
        if (pairs.Count < 2)
            return null;
        var rows = new Dictionary<string, Row>(StringComparer.Ordinal);
        int total = 40 + (pairs.Count - 1) * 16;
        foreach (var pair in pairs)
        {
            if (pair.Primary == pair.Secondary)
            {
                if (rows.TryGetValue(pair.Primary, out var prior) && prior.Pair.Secondary.Length > 0)
                    return null;
                int singleHeight = primaryHeight(pair.Primary);
                rows[pair.Primary] = new(new(pair.Primary, "", 0, 0, 0), singleHeight);
                total += singleHeight;
                continue;
            }
            var fit = DialoguePairLayout.FitPage(new[] { pair }, width, availableHeight, above,
                primaryHeight, secondaryWidth, secondaryHeight);
            if (fit == null)
                return null;
            var positioned = fit[0];
            int height = Math.Max(positioned.PrimaryOffset + primaryHeight(positioned.Primary),
                positioned.SecondaryOffset + (int)Math.Ceiling(secondaryHeight(positioned.Secondary)));
            if (rows.TryGetValue(pair.Primary, out var existing) && existing.Pair.Secondary != positioned.Secondary)
                return null; // A height call only carries text; conflicting labels cannot be distinguished there.
            rows[pair.Primary] = new(positioned, height);
            total += height;
        }
        return total <= availableHeight ? rows : null;
    }
}
