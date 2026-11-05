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
public sealed class ItemNameResolver : IDisposable
{
    private readonly ISecondaryContentProvider _contentProvider;
    private readonly Func<string, TrinketData?>? _trinketDataResolver;
    private readonly Func<string, StardewValley.GameData.Powers.PowersData?>? _powerDataResolver;
    private readonly ExternalTranslationRegistry _externalRegistry;
    private readonly ConcurrentDictionary<ItemCacheKey, string?> _cache = new();

    private static readonly Regex LocalizedTokenRegex = new(
        @"\[LocalizedText\s+([^:]+):([^\]]+)\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ISecondaryContentProvider ContentProvider => _contentProvider;
    public ExternalTranslationRegistry ExternalRegistry => _externalRegistry;
    public string EffectiveLanguage => _contentProvider.EffectiveLanguage;

    public ItemNameResolver(
        ISecondaryContentProvider contentProvider,
        Func<string, TrinketData?>? trinketDataResolver = null,
        Func<string, StardewValley.GameData.Powers.PowersData?>? powerDataResolver = null,
        ExternalTranslationRegistry? externalRegistry = null)
    {
        _contentProvider = contentProvider ?? throw new ArgumentNullException(nameof(contentProvider));
        _trinketDataResolver = trinketDataResolver;
        _powerDataResolver = powerDataResolver;
        _externalRegistry = externalRegistry ?? new ExternalTranslationRegistry();
        _externalRegistry.OnChanged += InvalidateCache;
        if (_contentProvider is RawContentProvider raw && raw.SourceRegistry != null)
            raw.SourceRegistry.OnChanged += InvalidateCache;
    }

    /// <summary>
    /// Clears only the resolved item name memory cache.
    /// </summary>
    public void Dispose()
    {
        _externalRegistry.OnChanged -= InvalidateCache;
        if (_contentProvider is RawContentProvider raw && raw.SourceRegistry != null)
            raw.SourceRegistry.OnChanged -= InvalidateCache;
        _cache.Clear();
    }

    public void InvalidateCache()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Clears the resolved item name cache, external registry cache, and content provider cache.
    /// </summary>
    public void Invalidate()
    {
        _cache.Clear();
        _externalRegistry.Invalidate();
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

        // 1. External registered translations (Content Patcher or C# mods) have highest priority
        string? external = _externalRegistry.GetTranslation("Item", item.QualifiedItemId, EffectiveLanguage);
        if (string.IsNullOrWhiteSpace(external) && item.QualifiedItemId.StartsWith("(O)", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(item.ItemId))
        {
            external = _externalRegistry.GetTranslation("Item", item.ItemId, EffectiveLanguage);
        }
        if (!string.IsNullOrWhiteSpace(external))
        {
            _cache[key] = external;
            return external;
        }

        if (!_contentProvider.IsSecondaryAvailable)
            return null;

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

        // 0. External registered translations (Content Patcher or C# mods) have highest priority
        string? external = _externalRegistry.GetTranslation("Item", itemId, EffectiveLanguage);
        if (!string.IsNullOrWhiteSpace(external))
            return external;

        string? qualifiedId = ItemRegistry.QualifyItemId(itemId);
        if (!string.IsNullOrEmpty(qualifiedId) && qualifiedId != itemId)
        {
            external = _externalRegistry.GetTranslation("Item", qualifiedId, EffectiveLanguage);
            if (!string.IsNullOrWhiteSpace(external))
                return external;
        }

        if (string.IsNullOrEmpty(qualifiedId))
        {
            // For custom/unregistered items without qualified ID from registry
            qualifiedId = itemId.StartsWith("(", StringComparison.Ordinal) ? itemId : $"(O){itemId}";
        }

        var key = new ItemCacheKey(qualifiedId, null, null, null, upgradeLevel);
        if (_cache.TryGetValue(key, out string? cachedName))
            return cachedName;

        if (!_contentProvider.IsSecondaryAvailable)
            return null;

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

        // Direct lookup from secondary content provider (Strings/Objects, Strings/1_6_Strings, etc.)
        // This handles standalone / mock provider testing and unparsed/custom items
        string unqualifiedId = itemId.StartsWith("(O)", StringComparison.OrdinalIgnoreCase) ? itemId.Substring(3) : itemId;
        if (_contentProvider.TryGetString("Strings/Objects", $"{unqualifiedId}_Name", out string? directString) && !string.IsNullOrEmpty(directString))
        {
            _cache[key] = directString;
            return directString;
        }

        if (itemId.StartsWith("(TR)", StringComparison.OrdinalIgnoreCase) || itemId.StartsWith("TR", StringComparison.OrdinalIgnoreCase))
        {
            string trId = itemId.StartsWith("(TR)", StringComparison.OrdinalIgnoreCase) ? itemId.Substring(4) : itemId.Substring(2);
            if (_contentProvider.TryGetString("Strings/1_6_Strings", $"{trId}_Name", out string? directTrinket) && !string.IsNullOrEmpty(directTrinket))
            {
                _cache[key] = directTrinket;
                return directTrinket;
            }
        }

        if (itemId.StartsWith("(B)", StringComparison.OrdinalIgnoreCase))
        {
            string bId = itemId.Substring(3);
            if (_contentProvider.TryGetString("Data/Boots", bId, out string? bootsEntry) && !string.IsNullOrEmpty(bootsEntry))
            {
                string[] fields = bootsEntry.Split('/');
                string? name = LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) ? ArgUtility.Get(fields, 0) : ArgUtility.Get(fields, 6);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    _cache[key] = name;
                    return name;
                }
            }
        }

        if (itemId.StartsWith("(H)", StringComparison.OrdinalIgnoreCase))
        {
            string hId = itemId.Substring(3);
            if (_contentProvider.TryGetString("Data/hats", hId, out string? hatsEntry) && !string.IsNullOrEmpty(hatsEntry))
            {
                string[] fields = hatsEntry.Split('/');
                string? name = LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) ? ArgUtility.Get(fields, 0) : ArgUtility.Get(fields, 5);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    _cache[key] = name;
                    return name;
                }
            }
        }

        if (itemId.StartsWith("(WP)", StringComparison.OrdinalIgnoreCase))
        {
            if (_contentProvider.TryGetString("Strings/StringsFromCSFiles", "Wallpaper.cs.13204", out string? wpName) && !string.IsNullOrWhiteSpace(wpName))
            {
                _cache[key] = wpName;
                return wpName;
            }
            if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage))
            {
                _cache[key] = "Wallpaper";
                return "Wallpaper";
            }
            return null;
        }

        if (itemId.StartsWith("(FL)", StringComparison.OrdinalIgnoreCase))
        {
            if (_contentProvider.TryGetString("Strings/StringsFromCSFiles", "Wallpaper.cs.13203", out string? flName) && !string.IsNullOrWhiteSpace(flName))
            {
                _cache[key] = flName;
                return flName;
            }
            if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage))
            {
                _cache[key] = "Flooring";
                return "Flooring";
            }
            return null;
        }

        return null;
    }

    /// <summary>
    /// Resolves the secondary display name for a special power from Data/Powers or known sources.
    /// </summary>
    public string? GetSecondaryPowerName(string? powerKey)
    {
        if (string.IsNullOrWhiteSpace(powerKey) || powerKey == "???")
            return null;

        string trimmed = powerKey.Trim();

        // 0. External registered translations (Content Patcher or C# mods) have highest priority
        string? external = _externalRegistry.GetTranslation("Power", trimmed, EffectiveLanguage);
        if (!string.IsNullOrWhiteSpace(external))
            return external;

        if (!_contentProvider.IsSecondaryAvailable)
            return null;

        string? patchedTitle = GetPatchedDisplayName("Data/Powers", trimmed);
        if (patchedTitle != null)
            return patchedTitle;

        // 1. Check data resolver or IPowerDataProvider
        StardewValley.GameData.Powers.PowersData? powerData = _powerDataResolver != null
            ? _powerDataResolver(trimmed)
            : (_contentProvider is IPowerDataProvider powerProvider && powerProvider.TryGetPowerData(trimmed, out var data) ? data : null);

        if (powerData != null && !string.IsNullOrWhiteSpace(powerData.DisplayName))
        {
            string? fromToken = ResolveLocalizedTokens(powerData.DisplayName);
            if (!string.IsNullOrWhiteSpace(fromToken) && fromToken != powerData.DisplayName)
                return fromToken;

            if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) && IsUsableLiteralName(powerData.DisplayName))
                return powerData.DisplayName;
        }

        // 2. Known vanilla power sources fallback
        if (SkillProfessionData.TryGetVanillaPowerSource(trimmed, out var sourceInfo))
        {
            if (_contentProvider.TryGetString(sourceInfo.Asset, sourceInfo.Key, out string? val) && !string.IsNullOrWhiteSpace(val))
                return val;
        }

        return null;
    }

    /// <summary>
    /// Resolves the secondary profession title by its integer number (0..29).
    /// </summary>
    public string? GetSecondaryProfessionTitle(int whichProfession)
    {
        string? profName = SkillProfessionData.GetProfessionInternalName(whichProfession);
        if (profName == null)
            return null;

        string? external = _externalRegistry.GetTranslation("Skill", profName, EffectiveLanguage);
        if (!string.IsNullOrWhiteSpace(external))
            return external;

        if (!_contentProvider.IsSecondaryAvailable)
            return null;

        if (_contentProvider.TryGetString("Strings/UI", $"LevelUp_ProfessionName_{profName}", out string? localizedTitle) && !string.IsNullOrWhiteSpace(localizedTitle))
            return localizedTitle;

        if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage))
            return profName;

        return null;
    }

    /// <summary>
    /// Resolves the secondary skill name by skill index (0..5).
    /// </summary>
    public string? GetSecondarySkillName(int skillIndex)
    {
        string? skillDefault = SkillProfessionData.GetSkillDefaultName(skillIndex);
        if (skillDefault != null)
        {
            string? external = _externalRegistry.GetTranslation("Skill", skillDefault, EffectiveLanguage);
            if (!string.IsNullOrWhiteSpace(external))
                return external;
        }

        if (!_contentProvider.IsSecondaryAvailable)
            return null;

        string? key = SkillProfessionData.GetSkillStringKey(skillIndex);
        if (key != null && _contentProvider.TryGetString("Strings/StringsFromCSFiles", key, out string? localizedSkill) && !string.IsNullOrWhiteSpace(localizedSkill))
            return localizedSkill;

        if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage))
            return skillDefault;

        return null;
    }

    /// <summary>
    /// Resolves the secondary achievement title by achievement ID.
    /// </summary>
    public string? GetSecondaryAchievementTitle(string? achievementId)
    {
        if (string.IsNullOrWhiteSpace(achievementId) || achievementId == "???")
            return null;

        string trimmed = achievementId.Trim();

        // 0. External registered translations (Content Patcher or C# mods) have highest priority
        string? external = _externalRegistry.GetTranslation("Achievement", trimmed, EffectiveLanguage);
        if (!string.IsNullOrWhiteSpace(external))
            return external;

        if (!_contentProvider.IsSecondaryAvailable)
            return null;

        if (_contentProvider.TryGetString("Data/Achievements", trimmed, out string? raw) && !string.IsNullOrWhiteSpace(raw))
        {
            string[] parts = raw.Split('^');
            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                return parts[0].Trim();
        }

        return null;
    }

    /// <summary>
    /// Resolves the secondary title for a letter by its mail ID.
    /// Uses actual Data/mail dictionary title suffix ([#]) without guessing from body or replacing NPC names.
    /// </summary>
    public string? GetSecondaryMailTitle(string? mailId)
    {
        if (string.IsNullOrWhiteSpace(mailId) || mailId == "???")
            return null;

        string trimmed = mailId.Trim();

        // 0. External registered translations (Content Patcher or C# mods) have highest priority
        string? external = _externalRegistry.GetTranslation("Mail", trimmed, EffectiveLanguage);
        if (!string.IsNullOrWhiteSpace(external))
            return external;

        if (!_contentProvider.IsSecondaryAvailable)
            return null;

        // 1. Live Data/mail from isolated secondary content provider
        if (_contentProvider.TryGetString("Data/mail", trimmed, out string? rawMail) && !string.IsNullOrEmpty(rawMail))
        {
            int delimIdx = rawMail.IndexOf("[#]", StringComparison.Ordinal);
            if (delimIdx >= 0 && delimIdx + 3 < rawMail.Length)
            {
                string title = rawMail.Substring(delimIdx + 3).Trim();
                if (!string.IsNullOrWhiteSpace(title) && title != "???")
                    return title;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the secondary title for a secret note or journal scrap.
    /// Handles note index and journal fragment indexing (>= 1000).
    /// </summary>
    public string? GetSecondarySecretNoteTitle(int noteIndex)
    {
        // 0. External registered translations (Content Patcher or C# mods) have highest priority
        string? external = _externalRegistry.GetTranslation("SecretNote", noteIndex.ToString(), EffectiveLanguage);
        if (!string.IsNullOrWhiteSpace(external))
            return external;

        if (!_contentProvider.IsSecondaryAvailable)
            return null;

        const int journalIndex = 1000;
        if (noteIndex >= journalIndex)
        {
            int journalNum = noteIndex - journalIndex;
            if (_contentProvider.TryGetString("Strings/Locations", "Journal_Name", out string? journalPrefix) && !string.IsNullOrWhiteSpace(journalPrefix))
                return $"{journalPrefix.Trim()} #{journalNum}";

            if (LanguageHelper.IsBaseOrEnglish(EffectiveLanguage))
                return $"Journal Scrap #{journalNum}";
        }
        else
        {
            if (_contentProvider.TryGetString("Strings/Locations", "Secret_Note_Name", out string? notePrefix) && !string.IsNullOrWhiteSpace(notePrefix))
                return $"{notePrefix.Trim()} #{noteIndex}";

            if (LanguageHelper.IsBaseOrEnglish(EffectiveLanguage))
                return $"Secret Note #{noteIndex}";
        }

        return null;
    }

    /// <summary>
    /// Resolves the secondary hover title for a CollectionsPage side tab (0..7).
    /// </summary>
    public string? GetSecondarySideTabName(int tabId)
    {
        // 0. External registered translations (Content Patcher or C# mods) have highest priority
        string? external = _externalRegistry.GetTranslation("SideTab", tabId.ToString(), EffectiveLanguage);
        if (!string.IsNullOrWhiteSpace(external))
            return external;

        if (!_contentProvider.IsSecondaryAvailable) return null;
        string? key = tabId switch
        {
            0 => "Collections_Shipped",
            1 => "Collections_Fish",
            2 => "Collections_Artifacts",
            3 => "Collections_Minerals",
            4 => "Collections_Cooking",
            5 => "Collections_Achievements",
            6 => "Collections_SecretNotes",
            7 => "Collections_Letters",
            _ => null
        };

        if (key != null)
        {
            if (_contentProvider.TryGetString("Strings/UI", key, out string? tabName) && !string.IsNullOrWhiteSpace(tabName))
                return tabName.Trim();

            if (LanguageHelper.IsBaseOrEnglish(EffectiveLanguage))
            {
                return tabId switch
                {
                    0 => "Items Shipped",
                    1 => "Fish",
                    2 => "Artifacts",
                    3 => "Minerals",
                    4 => "Cooking",
                    5 => "Achievements",
                    6 => "Secret Notes",
                    7 => "Letters",
                    _ => null
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the secondary title for a collections tab item.
    /// </summary>
    public string? GetSecondaryCollectionItemTitle(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || itemId == "???")
            return null;

        string trimmed = itemId.Trim();
        string unqualified = trimmed.StartsWith("(O)", StringComparison.OrdinalIgnoreCase) ? trimmed.Substring(3) : trimmed;

        if (_contentProvider.TryGetString("Strings/Objects", $"{unqualified}_CollectionsTabName", out string? colName) && !string.IsNullOrWhiteSpace(colName))
            return colName.Trim();

        string? fromRegistry = GetSecondaryNameById(trimmed);
        if (!string.IsNullOrWhiteSpace(fromRegistry))
            return fromRegistry;

        if (!_contentProvider.IsSecondaryAvailable) return null;

        // Fallback using Data/Objects directly when ItemRegistry is not initialized (e.g. offline tests or raw data)
        if (_contentProvider is RawContentProvider rawProvider && rawProvider.TryGetObjectData(unqualified, out StardewValley.GameData.Objects.ObjectData? objData) && objData != null)
        {
            if (TryParseLocalizedToken(objData.DisplayName, out string asset, out string key))
            {
                if (_contentProvider.TryGetString(asset, key, out string? val) && !string.IsNullOrWhiteSpace(val))
                    return val;
            }

            if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) && IsUsableLiteralName(objData.DisplayName))
                return objData.DisplayName;
        }

        return null;
    }

    private string? ResolveItem(Item item)
    {
        // 0. Trinket with dynamic variant override (e.g. frog egg colors, perfect ice rod, rapid/heavy quiver)
        if (item is StardewValley.Objects.Trinkets.Trinket trinket && !string.IsNullOrWhiteSpace(trinket.displayNameOverrideTemplate.Value))
        {
            string template = trinket.displayNameOverrideTemplate.Value;
            if (TryParseLocalizedToken(template, out string asset, out string key))
            {
                if (_contentProvider.TryGetString(asset, key, out string? val) && !string.IsNullOrEmpty(val))
                    return val;
            }

            string resolvedTokens = ResolveLocalizedTokens(template);
            if (!string.IsNullOrEmpty(resolvedTokens) && resolvedTokens != template)
                return resolvedTokens;

            if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) && IsUsableLiteralName(template))
                return template;
        }

        // 0b. Wallpaper and Flooring
        if (item is StardewValley.Objects.Wallpaper wp)
        {
            string key = wp.isFloor.Value ? "Wallpaper.cs.13203" : "Wallpaper.cs.13204";
            if (_contentProvider.TryGetString("Strings/StringsFromCSFiles", key, out string? wpName) && !string.IsNullOrWhiteSpace(wpName))
                return wpName;
            return LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) ? (wp.isFloor.Value ? "Flooring" : "Wallpaper") : null;
        }

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
        string? patchedAsset = data.ItemType.Identifier switch
        {
            "(O)" => "Data/Objects",
            "(BC)" => "Data/BigCraftables",
            "(W)" => "Data/Weapons",
            "(T)" => "Data/Tools",
            "(TR)" => "Data/Trinkets",
            "(P)" => "Data/Pants",
            "(S)" => "Data/Shirts",
            _ => null
        };
        if (patchedAsset != null && GetPatchedDisplayName(patchedAsset, data.ItemId) is string patchedTitle)
            return patchedTitle;

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

            if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) && IsUsableLiteralName(objData.DisplayName))
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

            if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) && IsUsableLiteralName(bcData.DisplayName))
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

            if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) && IsUsableLiteralName(weaponData.DisplayName))
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

            if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) && IsUsableLiteralName(toolData.DisplayName))
                return toolData.DisplayName;
        }

        // E. Boots, Hats, Furniture (string[] raw fields)
        if (raw is string[] fields && fields.Length > 0)
        {
            // Boots: Field 0 is English, Field 6 is localized name in Data/Boots
            if (data.ItemType.Identifier == ItemRegistry.type_boots)
            {
                if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage))
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
                if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage))
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

                if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage))
                {
                    return ArgUtility.Get(fields, 0);
                }

                return null;
            }

            // General string[] fallback
            if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage))
            {
                return ArgUtility.Get(fields, 0);
            }
        }

        // F. TrinketData & Vanilla Trinkets (where RawData is null by game design)
        if (data.ItemType.Identifier == ItemRegistry.type_trinket || data.QualifiedItemId.StartsWith("(TR)") || raw is TrinketData)
        {
            TrinketData? trinketData = raw as TrinketData;
            if (trinketData == null && _trinketDataResolver != null)
            {
                trinketData = _trinketDataResolver(data.ItemId);
            }
            if (trinketData == null && _contentProvider is ITrinketDataProvider trinketProvider)
            {
                trinketProvider.TryGetTrinketData(data.ItemId, out trinketData);
            }
            if (trinketData == null && Game1.content != null)
            {
                try
                {
                    DataLoader.Trinkets(Game1.content).TryGetValue(data.ItemId, out trinketData);
                }
                catch
                {
                    // Ignore content exceptions
                }
            }

            if (trinketData != null)
            {
                if (TryParseLocalizedToken(trinketData.DisplayName, out string asset, out string key))
                {
                    if (_contentProvider.TryGetString(asset, key, out string? val) && !string.IsNullOrEmpty(val))
                        return val;
                }

                if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) && IsUsableLiteralName(trinketData.DisplayName))
                    return trinketData.DisplayName;
            }

            // Direct fallback for vanilla and standard trinket keys: Strings/1_6_Strings:<ItemId>_Name
            if (_contentProvider.TryGetString("Strings/1_6_Strings", $"{data.ItemId}_Name", out string? byId) && !string.IsNullOrEmpty(byId))
                return byId;
        }

        // G. PantsData & ShirtData
        if (raw is PantsData pantsData)
        {
            if (TryParseLocalizedToken(pantsData.DisplayName, out string asset, out string key))
            {
                if (_contentProvider.TryGetString(asset, key, out string? val))
                    return val;
            }
            if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) && IsUsableLiteralName(pantsData.DisplayName))
                return pantsData.DisplayName;
        }

        if (raw is ShirtData shirtData)
        {
            if (TryParseLocalizedToken(shirtData.DisplayName, out string asset, out string key))
            {
                if (_contentProvider.TryGetString(asset, key, out string? val))
                    return val;
            }
            if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) && IsUsableLiteralName(shirtData.DisplayName))
                return shirtData.DisplayName;
        }

        // H. Mannequins (where RawData is null by game design)
        if (data.ItemType?.Identifier == "(M)" || data.QualifiedItemId.StartsWith("(M)"))
        {
            if (_contentProvider.TryGetString("Strings/1_6_Strings", $"{data.ItemId}_Name", out string? byId) && !string.IsNullOrEmpty(byId))
                return byId;
        }

        // I. Wallpaper (where RawData is null by vanilla game design)
        if (data.ItemType?.Identifier == ItemRegistry.type_wallpaper || data.QualifiedItemId.StartsWith("(WP)"))
        {
            if (data.RawData != null && TryParseLocalizedToken(data.DisplayName, out string tokenAsset, out string tokenKey))
            {
                if (_contentProvider.TryGetString(tokenAsset, tokenKey, out string? tokenVal) && !string.IsNullOrEmpty(tokenVal))
                    return tokenVal;
            }

            if (_contentProvider.TryGetString("Strings/StringsFromCSFiles", "Wallpaper.cs.13204", out string? wpName) && !string.IsNullOrWhiteSpace(wpName))
                return wpName;

            return LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) ? "Wallpaper" : null;
        }

        // J. Flooring (where RawData is null by vanilla game design)
        if (data.ItemType?.Identifier == ItemRegistry.type_floorpaper || data.QualifiedItemId.StartsWith("(FL)"))
        {
            if (data.RawData != null && TryParseLocalizedToken(data.DisplayName, out string tokenAsset, out string tokenKey))
            {
                if (_contentProvider.TryGetString(tokenAsset, tokenKey, out string? tokenVal) && !string.IsNullOrEmpty(tokenVal))
                    return tokenVal;
            }

            if (_contentProvider.TryGetString("Strings/StringsFromCSFiles", "Wallpaper.cs.13203", out string? flName) && !string.IsNullOrWhiteSpace(flName))
                return flName;

            return LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) ? "Flooring" : null;
        }

        // I. Fallback: InternalName if secondary is English and InternalName is human-readable
        if (LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage) && IsHumanReadableName(data.InternalName, data.ItemId))
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
            && LanguageHelper.IsBaseOrEnglish(_contentProvider.EffectiveLanguage))
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

    private string? GetPatchedDisplayName(string asset, string id)
    {
        if (_contentProvider is not RawContentProvider raw || raw.GetPatchedDisplayName(asset, id) is not string title)
            return null;
        string resolved = ResolveLocalizedTokens(title);
        return string.IsNullOrWhiteSpace(resolved) || LocalizedTokenRegex.IsMatch(resolved) ? null : resolved;
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
