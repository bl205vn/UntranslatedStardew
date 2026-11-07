using System;
using HarmonyLib;
using StardewValley;
using StardewValley.GameData.Movies;
using StardewValley.Locations;
using StardewValley.Menus;
using xTile.Dimensions;

namespace UntranslatedStardew.Patches;

[HarmonyPatch]
internal static class MoviePosterPatch
{
    private sealed class Context
    {
        internal readonly string Action;
        internal MovieData? Movie;
        internal Context(string action) { Action = action; }
    }
    [ThreadStatic] private static Context? _active;

    [HarmonyPatch(typeof(MovieTheater), nameof(MovieTheater.GetMovieToday))]
    [HarmonyPostfix]
    private static void PostfixGetMovieToday(MovieData? __result)
    {
        if (_active is { Action: "Theater_Poster" } context)
            context.Movie = __result;
    }

    [HarmonyPatch(typeof(MovieTheater), nameof(MovieTheater.GetUpcomingMovie))]
    [HarmonyPostfix]
    private static void PostfixGetUpcomingMovie(MovieData? __result)
    {
        if (_active is { Action: "Theater_PosterComingSoon" } context)
            context.Movie = __result;
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performAction), new[] { typeof(string[]), typeof(Farmer), typeof(Location) })]
    [HarmonyPrefix]
    private static void PrefixPerformAction(string[] action, out Context? __state)
    {
        __state = _active;
        _active = action is { Length: > 0 } && action[0] is "Theater_Poster" or "Theater_PosterComingSoon"
            ? new Context(action[0]) : null;
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performAction), new[] { typeof(string[]), typeof(Farmer), typeof(Location) })]
    [HarmonyPostfix]
    private static void PostfixPerformAction(string[] action)
    {
        try
        {
            if (_active is { Movie: { } movie } context)
            {
                if (Game1.activeClickableMenu is DialogueBox box)
                {
                    ModEntry.Instance?.TryAttachMoviePoster(box, movie, context.Action == "Theater_PosterComingSoon");
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.Instance?.Monitor.Log($"Failed to attach bilingual movie poster: {ex.Message}", StardewModdingAPI.LogLevel.Warn);
        }

    }
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performAction), new[] { typeof(string[]), typeof(Farmer), typeof(Location) })]
    [HarmonyFinalizer]
    private static Exception? FinalizerPerformAction(Exception? __exception, Context? __state)
    {
        // Restore outer action scope even if vanilla or another patch throws.
        _active = __state;
        return __exception;
    }

}
