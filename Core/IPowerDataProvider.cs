using StardewValley.GameData.Powers;

namespace UntranslatedStardew.Core;

/// <summary>
/// Provides access to PowersData entries for secondary power name resolution.
/// </summary>
public interface IPowerDataProvider
{
    /// <summary>
    /// Attempts to retrieve PowersData for a given power key.
    /// </summary>
    bool TryGetPowerData(string powerKey, out PowersData? powerData);
}
