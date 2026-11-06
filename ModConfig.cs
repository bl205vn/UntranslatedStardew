namespace UntranslatedStardew;

/// <summary>
/// Configuration settings for Untranslated Stardew.
/// </summary>
public class ModConfig
{
    /// <summary>
    /// The language code for secondary display names (e.g. "default", "en", "vi", "es", "fr", "ja", "ko", "zh").
    /// Default is "default" (base unlocalized game and mod content).
    /// </summary>
    public string SecondaryLanguage { get; set; } = "default";

    /// <summary>
    /// Whether to display secondary language item names in tooltips.
    /// </summary>
    public bool ShowSecondaryInTooltip { get; set; } = true;

    /// <summary>Enable the experimental secondary line in simple NPC dialogue.</summary>
    public bool ShowSecondaryInDialogue { get; set; } = false;

    /// <summary>
    /// If true, displays the secondary name on top and primary name below.
    /// If false (default), displays primary name on top and secondary below.
    /// </summary>
    public bool SecondaryFirst { get; set; } = false;

    /// <summary>
    /// Whether search query matching ignores diacritics and normalizes Vietnamese 'đ'/'Đ'.
    /// </summary>
    public bool DiacriticInsensitiveSearch { get; set; } = true;
}
