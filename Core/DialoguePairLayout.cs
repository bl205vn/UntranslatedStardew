namespace UntranslatedStardew.Core;

/// <summary>Fits both translations of one dialogue segment in its available space.</summary>
internal static class DialoguePairLayout
{
    internal const float SecondaryScale = 0.75f;
    private const int LineGap = 4;

    internal static (string Secondary, int PrimaryOffset, int SecondaryOffset)? Fit(
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
        if (primaryHeight + LineGap + secondaryHeight > height)
            return null;

        return secondaryFirst
            ? (wrapped, secondaryHeight + LineGap, 0)
            : (wrapped, 0, primaryHeight + LineGap);
    }
}
