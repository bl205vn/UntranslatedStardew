using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Pants;
using StardewValley.GameData.Shirts;
using StardewValley.GameData.Tools;
using StardewValley.GameData.Weapons;
using StardewValley.GameData;
using StardewValley.ItemTypeDefinitions;

namespace UntranslatedStardew.Core;

/// <summary>
/// Resolves secondary display names for items across all categories (objects, flavored items,
/// big craftables, weapons, tools, boots, hats, rings, clothing, trinkets, furniture).
/// Separated from UI hooks and search logic.
/// </summary>
public sealed class ItemNameResolver
{
    private readonly ISecondaryContentProvider _contentProvider;
    private readonly ConcurrentDictionary<ItemCacheKey, string?> _cache = new();

    private static readonly Regex LocalizedTokenRegex = new(
        @"\[LocalizedText\s+([^:]+):([^\]]+)\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ISecondaryContentProvider ContentProvider => _contentProvider;
    public string EffectiveLanguage => _contentProvider.EffectiveLanguage;

    public ItemNameResolver(ISecondaryContentProvider contentProvider)
    {
        _contentProvider = contentProvider ?? throw new ArgumentNullException(nameof(contentProvider));
    }

    /// <summary>
    /// Clears the resolved item name cache.
    /// </summary>
    public void Invalidate()
    {
        _cache.Clear();
        _contentProvider.Invalidate();
    }

    /// <summary>
    /// Resolves the secondary display name for an item instance.
    /// </summary>
    public string? GetSecondaryName(Item? item)
    {
        if (item == null)
            return null;

        ItemCacheKey key = ItemCacheKey.FromItem(item);
        if (_cache.TryGetValue(key, out string? cachedName))
            return cachedName;

        string? resolved = ResolveItem(item);
        _cache[key] = resolved;
        return resolved;
    }

    /// <summary>
    /// Resolves the secondary display name by qualified or unqualified item ID.
    /// </summary>
    public string? GetSecondaryNameById(string? itemId, int? upgradeLevel = null)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        string? qualifiedId = ItemRegistry.QualifyItemId(itemId);
        if (string.IsNullOrEmpty(qualifiedId))
            return null;

        var key = new ItemCacheKey(qualifiedId, null, null, null, upgradeLevel);
        if (_cache.TryGetValue(key, out string? cachedName))
            return cachedName;

        ParsedItemData? data = ItemRegistry.GetData(qualifiedId);
        if (data != null)
        {
            string? resolved = ResolveFromParsedData(data, upgradeLevel);
            if (!string.IsNullOrEmpty(resolved))
            {
                _cache[key] = resolved;
                return resolved;
            }
        }

        // Direct lookup from secondary content provider (Strings/Objects or Data/Objects)
        // This handles standalone / mock provider testing and unparsed/custom items
        string unqualifiedId = itemId.StartsWith("(O)", StringComparison.OrdinalIgnoreCase) ? itemId.Substring(3) : itemId;
        if (_contentProvider.TryGetString("Strings/Objects", $"{unqualifiedId}_Name", out string? directString) && !string.IsNullOrEmpty(directString))
        {
            _cache[key] = directString;
            return directString;
        }

        return null;
    }

    private string? ResolveItem(Item item)
    {
        // 1. In vanilla GetObjectDisplayName, displayNameFormat is prioritized over preserveType
        if (item is StardewValley.Object objWithFormat && !string.IsNullOrEmpty(objWithFormat.displayNameFormat))
        {
            string? formatResult = ResolveDisplayNameFormat(objWithFormat.QualifiedItemId, objWithFormat.GetPreservedItemId(), objWithFormat.displayNameFormat);
            if (!string.IsNullOrEmpty(formatResult))
                return formatResult;
        }

        // 2. Flavored / Preserved objects (Wine, Jelly, Pickles, Honey, Juice, Roe, etc.)
        if (item is StardewValley.Object obj && obj.preserve.Value.HasValue)
        {
            return ResolveFlavoredObject(obj.QualifiedItemId, obj.preserve.Value.Value, obj.preservedParentSheetIndex.Value);
        }

        // 3. Tools with upgrade levels
        int? toolUpgradeLevel = null;
        if (item is StardewValley.Tool tool && tool.UpgradeLevel > 0)
        {
            toolUpgradeLevel = tool.UpgradeLevel;
        }

        // 4. Resolve by ParsedItemData from registry
        ParsedItemData? data = ItemRegistry.GetData(item.QualifiedItemId);
        if (data != null)
        {
            return ResolveFromParsedData(data, toolUpgradeLevel);
        }

        return null;
    }

    /// <summary>
    /// Pure testable resolution from ParsedItemData.
    /// </summary>
    public string? ResolveFromParsedData(ParsedItemData data, int? upgradeLevel = null)
    {
        object? raw = data.RawData;

        // A. StardewValley.GameData.Objects.ObjectData
        if (raw is ObjectData objData)
        {
            if (TryParseLocalizedToken(objData.DisplayName, out string asset, out string key))
            {
                if (_contentProvider.TryGetString(asset, key, out string? val))
                    return val;
            }

            if (_contentProvider.TryGetString("Strings/Objects", $"{data.ItemId}_Name", out string? byId))
                return byId;

            if (_contentProvider.EffectiveLanguage == "en" && IsUsableLiteralName(objData.DisplayName))
                return objData.DisplayName;
        }

        // B. BigCraftableData
        if (raw is BigCraftableData bcData)
        {
            if (TryParseLocalizedToken(bcData.DisplayName, out string asset, out string key))
            {
                if (_contentProvider.TryGetString(asset, key, out string? val))
                    return val;
            }

            if (_contentProvider.TryGetString("Strings/BigCraftables", $"{data.ItemId}_Name", out string? byId))
                return byId;

            if (_contentProvider.EffectiveLanguage == "en" && IsUsableLiteralName(bcData.DisplayName))
                return bcData.DisplayName;
        }

        // C. WeaponData
        if (raw is WeaponData weaponData)
        {
            if (TryParseLocalizedToken(weaponData.DisplayName, out string asset, out string key))
            {
                if (_contentProvider.TryGetString(asset, key, out string? val))
                    return val;
            }

            if (_contentProvider.TryGetString("Strings/Weapons", $"{data.ItemId}_Name", out string? byId))
                return byId;

            if (_contentProvider.EffectiveLanguage == "en" && IsUsableLiteralName(weaponData.DisplayName))
                return weaponData.DisplayName;
        }

        // D. ToolData & Upgraded Tools
        if (raw is ToolData toolData)
        {
            string targetItemId = data.ItemId;

            // Handle tool upgrade tier mapping if upgradeLevel is provided and itemId is base tool
            if (upgradeLevel.HasValue && upgradeLevel.Value > 0)
            {
                string? upgradedId = GetUpgradedToolId(data.ItemId, upgradeLevel.Value);
                if (upgradedId != null && ItemRegistry.Exists($"(T){upgradedId}"))
                {
                    targetItemId = upgradedId;
                    ParsedItemData? upgradedData = ItemRegistry.GetData($"(T){upgradedId}");
                    if (upgradedData?.RawData is ToolData upgradedToolData)
                    {
                        toolData = upgradedToolData;
                    }
                }
            }

            if (TryParseLocalizedToken(toolData.DisplayName, out string asset, out string key))
            {
                if (_contentProvider.TryGetString(asset, key, out string? val))
                    return val;
            }

            if (_contentProvider.TryGetString("Strings/Tools", $"{targetItemId}_Name", out string? byId))
                return byId;

            if (!string.IsNullOrEmpty(toolData.Name) && _contentProvider.TryGetString("Strings/Tools", $"{toolData.Name}_Name", out string? byName))
                return byName;

            if (_contentProvider.EffectiveLanguage == "en" && IsUsableLiteralName(toolData.DisplayName))
                return toolData.DisplayName;
        }

        // E. Boots, Hats, Furniture (string[] raw fields)
        if (raw is string[] fields && fields.Length > 0)
        {
            // Boots: Field 0 is English, Field 6 is localized name in Data/Boots
            if (data.ItemType.Identifier == ItemRegistry.type_boots)
            {
                if (_contentProvider.EffectiveLanguage == "en")
                {
                    return ArgUtility.Get(fields, 0);
                }

                // Query localized Data/Boots dictionary
                if (_contentProvider.TryGetString("Data/Boots", data.ItemId, out string? bootsEntry) && !string.IsNullOrEmpty(bootsEntry))
                {
                    string[] bootsFields = bootsEntry.Split('/');
                    string? locName = ArgUtility.Get(bootsFields, 6);
                    if (!string.IsNullOrWhiteSpace(locName))
                        return locName;
                }

                // Truthful fallback: if no localized name found for non-English, do not label English as localized
                return null;
            }

            // Hats: Field 0 is English, Field 5 is localized name in Data/hats
            if (data.ItemType.Identifier == ItemRegistry.type_hat)
            {
                if (_contentProvider.EffectiveLanguage == "en")
                {
                    return ArgUtility.Get(fields, 0);
                }

                // Query localized Data/hats dictionary
                if (_contentProvider.TryGetString("Data/hats", data.ItemId, out string? hatsEntry) && !string.IsNullOrEmpty(hatsEntry))
                {
                    string[] hatsFields = hatsEntry.Split('/');
                    string? locName = ArgUtility.Get(hatsFields, 5);
                    if (!string.IsNullOrWhiteSpace(locName))
                        return locName;
                }

                return null;
            }

            // Furniture: Field 0 is English, Field 7 can be [LocalizedText Strings\Furniture:...]
            if (data.ItemType.Identifier == ItemRegistry.type_furniture)
            {
                string? locField = ArgUtility.Get(fields, 7);
                if (TryParseLocalizedToken(locField, out string fAsset, out string fKey))
                {
                    if (_contentProvider.TryGetString(fAsset, fKey, out string? fVal))
                        return fVal;
                }

                if (_contentProvider.EffectiveLanguage == "en")
                {
                    return ArgUtility.Get(fields, 0);
                }

                return null;
            }

            // General string[] fallback
            if (_contentProvider.EffectiveLanguage == "en")
            {
                return ArgUtility.Get(fields, 0);
            }
        }

        // F. TrinketData
        if (raw is TrinketData trinketData)
        {
            if (TryParseLocalizedToken(trinketData.DisplayName, out string asset, out string key))
            {
                if (_contentProvider.TryGetString(asset, key, out string? val))
                    return val;
            }

            if (_contentProvider.EffectiveLanguage == "en" && IsUsableLiteralName(trinketData.DisplayName))
                return trinketData.DisplayName;
        }

        // G. PantsData & ShirtData
        if (raw is PantsData pantsData)
        {
            if (TryParseLocalizedToken(pantsData.DisplayName, out string asset, out string key))
            {
                if (_contentProvider.TryGetString(asset, key, out string? val))
                    return val;
            }
            if (_contentProvider.EffectiveLanguage == "en" && IsUsableLiteralName(pantsData.DisplayName))
                return pantsData.DisplayName;
        }

        if (raw is ShirtData shirtData)
        {
            if (TryParseLocalizedToken(shirtData.DisplayName, out string asset, out string key))
            {
                if (_contentProvider.TryGetString(asset, key, out string? val))
                    return val;
            }
            if (_contentProvider.EffectiveLanguage == "en" && IsUsableLiteralName(shirtData.DisplayName))
                return shirtData.DisplayName;
        }

        // H. Fallback: InternalName if secondary is English and InternalName is human-readable
        if (_contentProvider.EffectiveLanguage == "en" && IsHumanReadableName(data.InternalName, data.ItemId))
        {
            return data.InternalName;
        }

        return null;
    }

    /// <summary>
    /// Pure testable resolution for flavored / preserved objects.
    /// </summary>
    public string? ResolveFlavoredObject(string qualifiedItemId, StardewValley.Object.PreserveType preserveType, string? preservedId, string? defaultBaseName = null, string? defaultPreservedName = null)
    {
        // Wild Honey special case
        if (preserveType == StardewValley.Object.PreserveType.Honey && preservedId == "-1")
        {
            return _contentProvider.GetString("Strings/Objects", "Honey_Wild_Name") ?? "Wild Honey";
        }

        string? preservedItemId = StardewValley.Object.GetPreservedItemId(preserveType, preservedId) ?? preservedId;
        string? qualifiedPreservedId = preservedItemId != null ? ItemRegistry.QualifyItemId(preservedItemId) : null;

        string? prefix = preserveType switch
        {
            StardewValley.Object.PreserveType.Honey => "Honey",
            StardewValley.Object.PreserveType.Wine => "Wine",
            StardewValley.Object.PreserveType.Jelly => "Jelly",
            StardewValley.Object.PreserveType.Pickle => "Pickles",
            StardewValley.Object.PreserveType.Juice => "Juice",
            StardewValley.Object.PreserveType.Roe => "Roe",
            StardewValley.Object.PreserveType.AgedRoe => "AgedRoe",
            StardewValley.Object.PreserveType.Bait => "SpecificBait",
            StardewValley.Object.PreserveType.DriedFruit => "DriedFruit",
            StardewValley.Object.PreserveType.DriedMushroom => "DriedFruit",
            StardewValley.Object.PreserveType.SmokedFish => "SmokedFish",
            _ => null
        };

        // 1. Check for item-specific flavor override (e.g. DriedFruit_Flavored_(O)258_Name)
        if (prefix != null && qualifiedPreservedId != null)
        {
            if (_contentProvider.TryGetString("Strings/Objects", $"{prefix}_Flavored_{qualifiedPreservedId}_Name", out string? specificTemplate) && !string.IsNullOrEmpty(specificTemplate))
            {
                // Item-specific template used directly without double pluralization
                return specificTemplate;
            }
        }

        string? preservedSecondaryName = preservedItemId != null ? (GetSecondaryNameById(preservedItemId) ?? defaultPreservedName) : null;
        string? baseSecondaryName = GetSecondaryNameById(qualifiedItemId) ?? defaultBaseName;

        if (string.IsNullOrEmpty(preservedSecondaryName))
        {
            return baseSecondaryName;
        }

        if (prefix == null)
            return baseSecondaryName;

        string lowerPreserved = preservedSecondaryName.ToLowerInvariant();

        _contentProvider.TryGetString("Strings/Objects", $"{prefix}_Flavored_Name", out string? template);

        // Default English templates if asset strings are not found
        template ??= preserveType switch
        {
            StardewValley.Object.PreserveType.Honey => "{0} Honey",
            StardewValley.Object.PreserveType.Wine => "{0} Wine",
            StardewValley.Object.PreserveType.Jelly => "{0} Jelly",
            StardewValley.Object.PreserveType.Pickle => "Pickled {0}",
            StardewValley.Object.PreserveType.Juice => "{0} Juice",
            StardewValley.Object.PreserveType.Roe => "{0} Roe",
            StardewValley.Object.PreserveType.AgedRoe => "Aged {0} Roe",
            StardewValley.Object.PreserveType.Bait => "{0} Bait",
            StardewValley.Object.PreserveType.DriedFruit => "Dried {0}",
            StardewValley.Object.PreserveType.DriedMushroom => "Dried {0}",
            StardewValley.Object.PreserveType.SmokedFish => "Smoked {0}",
            _ => "{0}"
        };

        if (preserveType is StardewValley.Object.PreserveType.Roe or StardewValley.Object.PreserveType.AgedRoe)
        {
            preservedSecondaryName = preservedSecondaryName.TrimEnd('鱼');
            lowerPreserved = lowerPreserved.TrimEnd('鱼');
        }

        string formatted;
        try
        {
            formatted = string.Format(template, preservedSecondaryName, lowerPreserved);
        }
        catch (FormatException)
        {
            formatted = $"{preservedSecondaryName} {prefix}";
        }

        // Generic template pluralization
        if (preserveType is StardewValley.Object.PreserveType.DriedFruit or StardewValley.Object.PreserveType.DriedMushroom
            && _contentProvider.EffectiveLanguage == "en")
        {
            formatted = LexiconPluralize(formatted);
        }

        return formatted;
    }

    /// <summary>
    /// Pure testable resolution for custom displayNameFormat.
    /// </summary>
    public string? ResolveDisplayNameFormat(string qualifiedItemId, string? preservedItemId, string displayNameFormat, string? defaultBaseName = null, string? defaultPreservedName = null)
    {
        if (string.IsNullOrEmpty(displayNameFormat))
            return null;

        string? baseName = GetSecondaryNameById(qualifiedItemId) ?? defaultBaseName ?? qualifiedItemId;

        string? preservedName = preservedItemId != null ? (GetSecondaryNameById(preservedItemId) ?? defaultPreservedName ?? preservedItemId) : null;
        string? lowerPreserved = preservedName?.ToLowerInvariant();

        // Resolve any [LocalizedText ...] tokens in the format string
        string parsedFormat = ResolveLocalizedTokens(displayNameFormat);

        if (parsedFormat.Contains('%'))
        {
            parsedFormat = parsedFormat
                .Replace("%DISPLAY_NAME_LOWERCASE", baseName.ToLowerInvariant())
                .Replace("%DISPLAY_NAME", baseName);

            if (preservedName != null)
            {
                parsedFormat = parsedFormat
                    .Replace("%PRESERVED_DISPLAY_NAME_LOWERCASE", lowerPreserved)
                    .Replace("%PRESERVED_DISPLAY_NAME", preservedName);
            }
        }

        return parsedFormat;
    }

    private string ResolveLocalizedTokens(string text)
    {
        return LocalizedTokenRegex.Replace(text, match =>
        {
            string asset = match.Groups[1].Value.Trim().Replace('\\', '/');
            string key = match.Groups[2].Value.Trim();
            if (_contentProvider.TryGetString(asset, key, out string? value))
            {
                return value ?? string.Empty;
            }
            return match.Value;
        });
    }

    public static bool TryParseLocalizedToken(string? text, out string assetSubpath, out string key)
    {
        assetSubpath = string.Empty;
        key = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        string trimmed = text.Trim();
        Match match = LocalizedTokenRegex.Match(trimmed);
        if (match.Success)
        {
            assetSubpath = match.Groups[1].Value.Trim().Replace('\\', '/');
            key = match.Groups[2].Value.Trim();
            return true;
        }

        // Check raw "Strings\\Objects:128_Name"
        int colonIdx = trimmed.IndexOf(':');
        if (colonIdx > 0 && colonIdx < trimmed.Length - 1 && !trimmed.Contains('['))
        {
            assetSubpath = trimmed.Substring(0, colonIdx).Trim().Replace('\\', '/');
            key = trimmed.Substring(colonIdx + 1).Trim();
            return true;
        }

        return false;
    }

    private static string? GetUpgradedToolId(string baseToolId, int upgradeLevel)
    {
        string prefix = upgradeLevel switch
        {
            1 => "Copper",
            2 => "Steel",
            3 => "Gold",
            4 => "Iridium",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(prefix))
            return null;

        return baseToolId switch
        {
            "Axe" => $"{prefix}Axe",
            "Pickaxe" => $"{prefix}Pickaxe",
            "Hoe" => $"{prefix}Hoe",
            "WateringCan" => $"{prefix}WateringCan",
            "Pan" => $"{prefix}Pan",
            "TrashCan" => $"{prefix}TrashCan",
            _ => null
        };
    }

    private static bool IsUsableLiteralName(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        return !text.StartsWith("[", StringComparison.Ordinal);
    }

    private static bool IsHumanReadableName(string? internalName, string? itemId)
    {
        if (string.IsNullOrWhiteSpace(internalName))
            return false;
        if (string.Equals(internalName, itemId, StringComparison.OrdinalIgnoreCase))
            return false;
        if (int.TryParse(internalName, out _))
            return false;
        return true;
    }

    /// <summary>
    /// Exact vanilla Stardew Lexicon.makePlural logic (avoiding CurrentLanguageCode != en bail-out).
    /// </summary>
    public static string LexiconPluralize(string word)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        switch (word)
        {
            case "Dragon Tooth": return "Dragon Teeth";
            case "Rice Pudding": return "bowls of Rice Pudding";
            case "Algae Soup": return "bowls of Algae Soup";
            case "Coal": return "lumps of Coal";
            case "Salt": return "pieces of Salt";
            case "Jelly": return "Jellies";
            case "Wheat": return "bushels of Wheat";
            case "Ginger": return "pieces of Ginger";
            case "Garlic": return "bulbs of Garlic";
            case "Glass Shards":
            case "Hashbrowns":
            case "Tea Leaves":
            case "Crab Cakes":
            case "Carp":
            case "Chub":
            case "Clay":
            case "Hops":
            case "Bream":
            case "Weeds":
            case "Bok Choy":
            case "Pancakes":
            case "Sandfish":
            case "Broken Glasses":
            case "Pepper Poppers":
            case "Cranberries":
            case "Glazed Yams":
            case "Green Canes":
            case "Mixed Seeds":
            case "Star Shards":
            case "Dried Cranberries":
            case "Roasted Hazelnuts":
            case "Driftwood":
            case "Ghostfish":
            case "Red Canes":
            case "Fossilized Ribs":
            case "Largemouth Bass":
            case "Smallmouth Bass":
            case "Dried Sunflowers":
            case "Hay":
            case "Pickles":
            case "Warp Totem: Mountains":
                return word;
            default:
                char last = word[word.Length - 1];
                switch (last)
                {
                    case 'y':
                        return word.Substring(0, word.Length - 1) + "ies";
                    case 's':
                        if (!word.EndsWith(" Seeds") && !word.EndsWith(" Shorts") && !word.EndsWith(" Bass") && !word.EndsWith(" Flowers") && !word.EndsWith(" Peach"))
                        {
                            return word + "es";
                        }
                        return word;
                    case 'x':
                    case 'z':
                        return word + "es";
                    default:
                        if (word.Length > 2)
                        {
                            string ending = word.Substring(word.Length - 2);
                            if (ending == "sh" || ending == "ch")
                            {
                                return word + "es";
                            }
                        }
                        return word + "s";
                }
        }
    }
}
