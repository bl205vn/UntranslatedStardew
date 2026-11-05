namespace UntranslatedStardew.Core;

/// <summary>
/// Designates the semantic language role of a text run or chunk within a bilingual composition.
/// </summary>
public enum FontRole
{
    /// <summary>
    /// Chunk belongs to the primary active game language.
    /// Always targets the live Game1 font for the font kind (honoring FontSettings).
    /// </summary>
    Primary,

    /// <summary>
    /// Chunk belongs to the secondary configured language.
    /// Always targets the secondary locale font for the font kind, even if ASCII-only.
    /// </summary>
    Secondary
}
