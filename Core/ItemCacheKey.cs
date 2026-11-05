using StardewValley;

namespace UntranslatedStardew.Core;

/// <summary>
/// A composite cache key for resolved item secondary names that accounts for preserve variants,
/// display format overrides, and tool upgrade levels.
/// </summary>
public readonly record struct ItemCacheKey(
    string QualifiedItemId,
    StardewValley.Object.PreserveType? PreserveType,
    string? PreservedId,
    string? DisplayNameFormat,
    int? UpgradeLevel
)
{
    public static ItemCacheKey FromItem(Item item)
    {
        if (item is StardewValley.Objects.Trinkets.Trinket trinket)
        {
            return new ItemCacheKey(
                item.QualifiedItemId,
                null,
                null,
                trinket.displayNameOverrideTemplate.Value,
                null
            );
        }

        if (item is StardewValley.Object obj)
        {
            return new ItemCacheKey(
                item.QualifiedItemId,
                obj.preserve.Value,
                obj.preservedParentSheetIndex.Value,
                obj.displayNameFormat,
                null
            );
        }

        if (item is StardewValley.Tool tool)
        {
            return new ItemCacheKey(
                item.QualifiedItemId,
                null,
                null,
                null,
                tool.UpgradeLevel
            );
        }

        return new ItemCacheKey(
            item.QualifiedItemId,
            null,
            null,
            null,
            null
        );
    }
}
