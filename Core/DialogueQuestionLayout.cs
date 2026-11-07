namespace UntranslatedStardew.Core;

/// <summary>One measured row serves both question drawing and vanilla input geometry.</summary>
internal static class DialogueQuestionLayout
{
    internal sealed record Row(DialoguePairLayout.PositionedPair Pair, int Height);

    internal static Dictionary<string, DialogueInterleavedLayout.Page>? FitRows(IReadOnlyList<DialogueTextPair> pairs,
        int width, int availableHeight, bool above, Func<string, int> primaryHeight,
        Func<string, float> secondaryWidth, Func<string, float> secondaryHeight, out string? failure)
    {
        failure = null;
        var rows = new Dictionary<string, DialogueInterleavedLayout.Page>(StringComparer.Ordinal);
        var translations = new Dictionary<string, string>(StringComparer.Ordinal);
        int total = 40 + (pairs.Count - 1) * 16;
        foreach (var pair in pairs)
        {
            if (translations.TryGetValue(pair.Primary, out string? previous) && previous != pair.Secondary)
            { failure = "identical primary labels have different translations"; return null; }
            var page = DialogueInterleavedLayout.Fit(new[] { pair }, width, availableHeight, above,
                primaryHeight, secondaryWidth, secondaryHeight);
            if (page == null) { failure = "an interleaved question row does not fit"; return null; }
            translations[pair.Primary] = pair.Secondary;
            rows[pair.Primary] = page;
            total += page.Height;
        }
        if (total > availableHeight)
        { failure = $"question height={total}px exceeds budget={availableHeight}px"; return null; }
        return rows;
    }

    internal static Dictionary<string, Row>? Fit(IReadOnlyList<DialogueTextPair> pairs, int width, int availableHeight,
        bool above, Func<string, int> primaryHeight, Func<string, float> secondaryWidth, Func<string, float> secondaryHeight)
        => Fit(pairs, width, availableHeight, above, primaryHeight, secondaryWidth, secondaryHeight, out _);

    internal static Dictionary<string, Row>? Fit(IReadOnlyList<DialogueTextPair> pairs, int width, int availableHeight,
        bool above, Func<string, int> primaryHeight, Func<string, float> secondaryWidth, Func<string, float> secondaryHeight,
        out string? failure)
    {
        failure = null;
        if (pairs.Count < 2 || width <= 0 || availableHeight <= 0)
        {
            failure = $"invalid question dimensions: rows={pairs.Count}, width={width}, height={availableHeight}";
            return null;
        }
        var rows = new Dictionary<string, Row>(StringComparer.Ordinal);
        int total = 40 + (pairs.Count - 1) * 16;
        foreach (var pair in pairs)
        {
            if (pair.Primary == pair.Secondary)
            {
                if (rows.TryGetValue(pair.Primary, out var prior) && prior.Pair.Secondary.Length > 0)
                {
                    failure = "identical primary labels have different translations";
                    return null;
                }
                int singleHeight = primaryHeight(pair.Primary);
                rows[pair.Primary] = new(new(pair.Primary, "", 0, 0, 0), singleHeight);
                total += singleHeight;
                continue;
            }
            var fit = DialoguePairLayout.FitPage(new[] { pair }, width, availableHeight, above,
                primaryHeight, secondaryWidth, secondaryHeight);
            if (fit == null)
            {
                failure = $"a question row does not fit: width={width}, height budget={availableHeight}";
                return null;
            }
            var positioned = fit[0];
            int height = Math.Max(positioned.PrimaryOffset + primaryHeight(positioned.Primary),
                positioned.SecondaryOffset + (int)Math.Ceiling(secondaryHeight(positioned.Secondary)));
            if (rows.TryGetValue(pair.Primary, out var existing) && existing.Pair.Secondary != positioned.Secondary)
            {
                failure = "identical primary labels have different translations";
                return null;
            } // A height call only carries text; conflicting labels cannot be distinguished there.
            rows[pair.Primary] = new(positioned, height);
            total += height;
        }
        if (total > availableHeight)
        {
            failure = $"question height={total}px exceeds budget={availableHeight}px (width={width}px)";
            return null;
        }
        return rows;
    }
}
