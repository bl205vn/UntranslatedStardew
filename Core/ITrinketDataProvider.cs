using StardewValley.GameData;

namespace UntranslatedStardew.Core;

/// <summary>
/// Optional interface for content providers that can supply TrinketData.
/// Decouples trinket resolution from specific asset loaders.
/// </summary>
public interface ITrinketDataProvider
{
    /// <summary>
    /// Attempts to retrieve TrinketData for a given unqualified trinket ID (e.g. "FrogEgg").
    /// </summary>
    /// <param name="itemId">The unqualified item ID.</param>
    /// <param name="trinketData">The resolved TrinketData if found, else null.</param>
    /// <returns>True if found, false otherwise.</returns>
    bool TryGetTrinketData(string itemId, out TrinketData? trinketData);
}
