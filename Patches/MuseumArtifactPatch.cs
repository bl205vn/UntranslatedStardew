using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using xTile.Dimensions;

namespace UntranslatedStardew.Patches;

/// <summary>Retains the donated item ID selected by the museum's examination action.</summary>
[HarmonyPatch(typeof(LibraryMuseum), nameof(LibraryMuseum.checkAction))]
internal static class MuseumArtifactPatch
{
    private readonly record struct Examination(string? ItemId, IClickableMenu? PreviousMenu);

    private static void Prefix(LibraryMuseum __instance, Location tileLocation, out Examination __state)
    {
        __instance.museumPieces.TryGetValue(new Vector2(tileLocation.X, tileLocation.Y), out string? itemId);
        if (itemId == null)
            __instance.museumPieces.TryGetValue(new Vector2(tileLocation.X, tileLocation.Y - 1), out itemId);
        __state = new(itemId, Game1.activeClickableMenu);
    }

    private static void Postfix(bool __result, Examination __state)
    {
        if (!__result || __state.ItemId == null || Game1.activeClickableMenu is not DialogueBox box
            || ReferenceEquals(box, __state.PreviousMenu)) return;
        try { ModEntry.Instance?.TryAttachMuseumArtifact(box, __state.ItemId); }
        catch (Exception ex)
        {
            ModEntry.Instance?.Monitor.Log($"Museum translation fell back: {ex.Message}", StardewModdingAPI.LogLevel.Trace);
        }
    }
}
