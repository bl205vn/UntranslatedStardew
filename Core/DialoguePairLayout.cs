namespace UntranslatedStardew.Core;

internal readonly record struct DialogueTextPair(string Primary, string Secondary);

/// <summary>Positions each matching pair within a dialogue page.</summary>
internal static class DialoguePairLayout
{
    internal const float SecondaryScale = 0.75f;
    private const int LineGap = 4;

    internal readonly record struct PositionedPair(
        string Primary, string Secondary, int Start, int PrimaryOffset, int SecondaryOffset)
    {
        internal int RevealedCharacters(int position) => Math.Clamp(position - Start, 0, Primary.Length);
    }

    // Vanilla enables its advance action at Length - 1.
    internal static int GetVisiblePosition(int position, int length)
        => position >= length - 1 ? length : position;

    internal static DialogueTextPair[] PreparePairs(DialogueTextPair[] pairs, string speakerPrefix)
    {
        var prepared = pairs.Select(pair => pair with { Secondary = pair.Secondary.Normalize() }).ToArray();
        if (prepared.Length > 0)
            prepared[0] = prepared[0] with { Primary = speakerPrefix + prepared[0].Primary };
        return prepared;
    }

    internal static PositionedPair[]? FitPage(
        IReadOnlyList<DialogueTextPair> pairs, int width, int height, bool secondaryFirst,
        Func<string, int> measurePrimaryHeight, Func<string, float> measureWidth, Func<string, float> measureHeight)
    {
        var result = new PositionedPair[pairs.Count];
        int top = 0;
        int start = 0;
        for (int i = 0; i < pairs.Count; i++)
        {
            DialogueTextPair pair = pairs[i];
            var fit = Fit(pair.Secondary, width, height - top, measurePrimaryHeight(pair.Primary),
                secondaryFirst, measureWidth, measureHeight);
            if (fit is not { } fitted)
                return null;

            result[i] = new PositionedPair(pair.Primary, fitted.Secondary, start,
                top + fitted.PrimaryOffset, top + fitted.SecondaryOffset);
            top += fitted.Height + LineGap;
            start += pair.Primary.Length + 1; // The source page joins primary blocks with one space.
        }
        return result;
    }

    private static (string Secondary, int PrimaryOffset, int SecondaryOffset, int Height)? Fit(
        string secondary, int width, int height, int primaryHeight, bool secondaryFirst,
        Func<string, float> measureWidth, Func<string, float> measureHeight)
    {
        if (width <= 0 || primaryHeight <= 0 || primaryHeight >= height)
            return null;

        string wrapped = measureWidth(secondary) <= width
            ? secondary
            : RoleTextWrapper.Wrap(secondary, width, measureWidth);
        if (string.IsNullOrWhiteSpace(wrapped) || wrapped.Split('\n').Any(line => measureWidth(line) > width))
            return null;

        int secondaryHeight = (int)Math.Ceiling(measureHeight(wrapped));
        int pairHeight = primaryHeight + LineGap + secondaryHeight;
        if (pairHeight > height)
            return null;

        return secondaryFirst
            ? (wrapped, secondaryHeight + LineGap, 0, pairHeight)
            : (wrapped, 0, primaryHeight + LineGap, pairHeight);
    }
}
