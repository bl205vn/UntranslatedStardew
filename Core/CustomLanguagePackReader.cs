using Microsoft.Xna.Framework.Content;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace UntranslatedStardew.Core;

/// <summary>Reads the locale-specific subset of Content Patcher packs without changing the live locale.</summary>
internal sealed class CustomLanguagePackReader
{
    private readonly string _locale;
    private readonly IServiceProvider? _services;
    private readonly Action<string> _trace;
    private readonly Dictionary<string, JsonObject?> _i18n = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _strings = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, (string Root, string Asset)> SpriteFontAssets { get; } = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> _config = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _fontPriority = new(StringComparer.OrdinalIgnoreCase);

    public CustomLanguagePackReader(string locale, IServiceProvider? services, Action<string> trace)
    {
        _locale = locale;
        _services = services;
        _trace = trace;
    }

    public Dictionary<string, string> Read(IEnumerable<(string Id, string Directory)> packs)
    {
        foreach (var (id, directory) in packs)
        {
            try
            {
                _config = new(StringComparer.OrdinalIgnoreCase);
                string configPath = Path.Combine(directory, "config.json");
                var config = File.Exists(configPath) ? Parse(configPath) : null;
                if (Parse(Path.Combine(directory, "content.json"))["ConfigSchema"] is JsonObject schema)
                    foreach (var option in schema)
                    {
                        if (option.Value is not JsonObject definition)
                            continue;
                        string? value = config?[option.Key]?.ToString() ?? definition["Default"]?.ToString();
                        if (value != null)
                            _config[option.Key] = value;
                    }
                using var content = _services == null ? null : new ContentManager(_services, directory);
                ReadFile(directory, id, "content.json", false, content, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _trace($"Custom locale {_locale}, pack {id}: {ex.Message}");
            }
        }

        return _strings;
    }

    private void ReadFile(string root, string id, string relative, bool inheritedLocale, ContentManager? content, HashSet<string> stack)
    {
        if (!LanguageSourceRegistry.TryResolveSafePath(root, relative, out string path) || !File.Exists(path)
            || stack.Count >= 32 || !stack.Add(path))
            return;

        try
        {
            if (Parse(path)["Changes"] is not JsonArray changes)
                return;

            foreach (JsonObject patch in changes.OfType<JsonObject>())
            {
                try { ApplyPatch(root, id, patch, inheritedLocale, content, stack); }
                catch (Exception ex) { _trace($"Custom locale {_locale}, pack {id}, {relative}: {ex.Message}"); }
            }
        }
        finally { stack.Remove(path); }
    }

    private void ApplyPatch(string root, string id, JsonObject patch, bool inheritedLocale, ContentManager? content, HashSet<string> stack)
    {
        bool localized = inheritedLocale;
        if (patch["When"] is JsonObject conditions)
        {
            foreach (var condition in conditions)
            {
                bool languageCondition = condition.Key.Equals("Language", StringComparison.OrdinalIgnoreCase);
                string? actual = languageCondition ? _locale : _config.GetValueOrDefault(condition.Key.Trim());
                string? expected = Expand(condition.Value?.ToString(), root, id);
                if (actual == null || !Split(expected).Contains(actual, StringComparer.OrdinalIgnoreCase))
                    return;

                localized |= languageCondition;
            }
        }

        string action = Text(patch["Action"]) ?? "";
        string? fromTemplate = Text(patch["FromFile"]);
        localized |= fromTemplate?.Contains("{{Language}}", StringComparison.OrdinalIgnoreCase) == true;
        string? from = Expand(fromTemplate, root, id);
        if (action.Equals("Include", StringComparison.OrdinalIgnoreCase))
        {
            foreach (string file in Split(from))
                ReadFile(root, id, file, localized, content, stack);
            return;
        }

        foreach (string rawTarget in Split(Expand(Text(patch["Target"]), root, id)))
        {
            string target = rawTarget.Replace('\\', '/');
            string suffix = "." + _locale;
            bool hasSuffix = target.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
            if (!localized && !hasSuffix)
                continue;
            if (hasSuffix)
                target = target[..^suffix.Length];
            from = Expand(fromTemplate?.Replace("{{Target}}", rawTarget, StringComparison.OrdinalIgnoreCase), root, id);
            if (action.Equals("Load", StringComparison.OrdinalIgnoreCase)
                && (target.Equals("Fonts/SmallFont", StringComparison.OrdinalIgnoreCase) || target.Equals("Fonts/SpriteFont1", StringComparison.OrdinalIgnoreCase))
                && from?.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase) == true
                && LanguageSourceRegistry.TryResolveSafePath(root, from, out string fontFile) && File.Exists(fontFile))
            {
                int priority = (Text(patch["Priority"]) ?? "").ToLowerInvariant() switch
                {
                    "low" => -1,
                    "high" => 1,
                    "exclusive" => 2,
                    _ => 0
                };
                if (!_fontPriority.TryGetValue(target, out int previous) || priority >= previous)
                {
                    SpriteFontAssets[target] = (root, from[..^4]);
                    _fontPriority[target] = priority;
                }
                continue;
            }
            if (!target.StartsWith("Strings/", StringComparison.OrdinalIgnoreCase)
                && !target.StartsWith("Data/", StringComparison.OrdinalIgnoreCase))
                continue;

            bool load = action.Equals("Load", StringComparison.OrdinalIgnoreCase);
            if (!load && !action.Equals("EditData", StringComparison.OrdinalIgnoreCase))
                continue;

            if (from != null && LanguageSourceRegistry.TryResolveSafePath(root, from, out string file) && File.Exists(file))
            {
                if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    var data = Parse(file);
                    ReadEntries(target, load ? data : data["Entries"] as JsonObject, root, id);
                    if (!load)
                        ReadEntries(target, data["Fields"] as JsonObject, root, id);
                }
                else if (load && content != null && file.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
                {
                    string asset = from[..^4];
                    switch (content.Load<object>(asset))
                    {
                        case Dictionary<int, string> indexed:
                            foreach (var entry in indexed)
                                _strings[$"{target}:{entry.Key}"] = entry.Value;
                            break;
                        case Dictionary<string, string> named:
                            foreach (var entry in named)
                                _strings[$"{target}:{entry.Key}"] = entry.Value;
                            break;
                    }
                }
            }
            if (!load)
            {
                ReadEntries(target, patch["Entries"] as JsonObject, root, id);
                ReadEntries(target, patch["Fields"] as JsonObject, root, id);
            }
        }
    }

    private void ReadEntries(string target, JsonObject? entries, string root, string id)
    {
        if (entries == null)
            return;
        foreach (var entry in entries)
        {
            string? key = Expand(entry.Key, root, id);
            if (key == null)
                continue;
            bool structured = entry.Value is JsonObject;
            JsonNode? token = structured ? ((JsonObject)entry.Value!).FirstOrDefault(field => field.Key.Equals("DisplayName", StringComparison.OrdinalIgnoreCase)).Value : entry.Value;
            string storageKey = structured ? $"{target}/DisplayName:{key}" : $"{target}:{key}";
            if (token == null && !structured)
                _strings.Remove(storageKey);
            else if (token is JsonValue field && field.TryGetValue<string>(out string? raw) && Expand(raw, root, id) is string value)
                _strings[storageKey] = value;
        }
    }

    private string? Expand(string? text, string root, string id)
    {
        if (text == null)
            return null;
        text = text.Replace("{{Language}}", _locale, StringComparison.OrdinalIgnoreCase)
            .Replace("{{ModId}}", id, StringComparison.OrdinalIgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\{\{\s*i18n:\s*([^{}|]+?)\s*\}\}", match =>
        {
            string key = match.Groups[1].Value;
            foreach (string locale in new[] { _locale, "default" })
            {
                if (!LanguageSourceRegistry.TryResolveSafePath(root, $"i18n/{locale}.json", out string path))
                    continue;
                if (!_i18n.TryGetValue(path, out var entries))
                    _i18n[path] = entries = File.Exists(path) ? Parse(path) : null;
                if (Text(entries?[key]) is string translated)
                    return translated;
            }
            return match.Value;
        }, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        foreach (var option in _config)
            text = text.Replace("{{" + option.Key + "}}", option.Value, StringComparison.OrdinalIgnoreCase);
        return text.Contains("{{", StringComparison.Ordinal) ? null : text;
    }

    private static string? Text(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static JsonObject Parse(string path)
        => JsonNode.Parse(NormalizePackJson(File.ReadAllText(path)), new JsonNodeOptions { PropertyNameCaseInsensitive = true },
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })?.AsObject()
            ?? throw new FormatException("Expected a JSON object.");

    // Some packs accepted by Content Patcher contain literal tabs or newlines inside quoted values.
    // Preserve their value while escaping control characters for System.Text.Json.
    private static string NormalizePackJson(string json)
        => System.Text.RegularExpressions.Regex.Replace(json, @"//[^\r\n]*|/\*[\s\S]*?\*/|""(?:\\[\s\S]|[^""\\])*""", match =>
            match.Value[0] == '"' ? System.Text.RegularExpressions.Regex.Replace(match.Value, @"[\x00-\x1f]",
                control => $"\\u{(int)control.Value[0]:x4}") : match.Value);

    private static IEnumerable<string> Split(string? value)
        => (value ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
