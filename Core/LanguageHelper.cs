using System;
using System.Collections.Generic;

namespace UntranslatedStardew.Core;

/// <summary>
/// Helper utilities for recognizing, normalizing, and resolving language codes and XNB asset suffixes.
/// </summary>
public static class LanguageHelper
{
    private static readonly Dictionary<string, (string CanonicalCode, string? Suffix)> SuffixMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = ("en", null),
        ["en-us"] = ("en", null),
        ["es"] = ("es", "es-ES"),
        ["es-es"] = ("es", "es-ES"),
        ["de"] = ("de", "de-DE"),
        ["de-de"] = ("de", "de-DE"),
        ["fr"] = ("fr", "fr-FR"),
        ["fr-fr"] = ("fr", "fr-FR"),
        ["pt"] = ("pt", "pt-BR"),
        ["pt-br"] = ("pt", "pt-BR"),
        ["ru"] = ("ru", "ru-RU"),
        ["ru-ru"] = ("ru", "ru-RU"),
        ["ja"] = ("ja", "ja-JP"),
        ["ja-jp"] = ("ja", "ja-JP"),
        ["zh"] = ("zh", "zh-CN"),
        ["zh-cn"] = ("zh", "zh-CN"),
        ["ko"] = ("ko", "ko-KR"),
        ["ko-kr"] = ("ko", "ko-KR"),
        ["it"] = ("it", "it-IT"),
        ["it-it"] = ("it", "it-IT"),
        ["tr"] = ("tr", "tr-TR"),
        ["tr-tr"] = ("tr", "tr-TR"),
        ["hu"] = ("hu", "hu-HU"),
        ["hu-hu"] = ("hu", "hu-HU")
    };

    /// <summary>
    /// Canonicalizes a language code (e.g. "FR-fr" -> "fr", "vi-VN" -> "vi", null -> "en").
    /// </summary>
    public static string CanonicalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return "en";

        string trimmed = languageCode.Trim();
        if (SuffixMap.TryGetValue(trimmed, out var entry))
            return entry.CanonicalCode;

        if (IsVietnamese(trimmed))
            return "vi";

        return trimmed.ToLowerInvariant();
    }

    /// <summary>
    /// Checks whether the given language code is built-in to the base game Content.
    /// </summary>
    public static bool IsBuiltInLanguage(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return false;
        return SuffixMap.ContainsKey(languageCode.Trim());
    }

    /// <summary>
    /// Gets the standard XNB file suffix for built-in languages, or null for English.
    /// Returns null if the language is not a built-in localized variant.
    /// </summary>
    public static string? GetLanguageSuffix(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return null;
        return SuffixMap.TryGetValue(languageCode.Trim(), out var entry) ? entry.Suffix : null;
    }

    /// <summary>
    /// Checks whether the language code represents Vietnamese.
    /// </summary>
    public static bool IsVietnamese(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return false;
        string trimmed = languageCode.Trim();
        return string.Equals(trimmed, "vi", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "vi-vn", StringComparison.OrdinalIgnoreCase);
    }
}
