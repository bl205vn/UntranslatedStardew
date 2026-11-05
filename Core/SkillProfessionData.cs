using System;
using System.Collections.Generic;

namespace UntranslatedStardew.Core;

/// <summary>
/// Contains stable metadata mappings and key helpers for skills, professions, and special powers.
/// Avoids reverse substring matching of localized strings.
/// </summary>
public static class SkillProfessionData
{
    private static readonly Dictionary<string, (string Asset, string Key)> VanillaPowerSources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SkullKey"] = ("Strings/StringsFromCSFiles", "SkillsPage.cs.11591"),
        ["RustyKey"] = ("Strings/StringsFromCSFiles", "SkillsPage.cs.11588"),
        ["ClubCard"] = ("Strings/StringsFromCSFiles", "SkillsPage.cs.11589"),
        ["SpecialCharm"] = ("Strings/StringsFromCSFiles", "SkillsPage.cs.11590"),
        ["MagnifyingGlass"] = ("Strings/StringsFromCSFiles", "SkillsPage.cs.magnifyingglass"),
        ["DarkTalisman"] = ("Strings/Objects", "DarkTalisman"),
        ["MagicInk"] = ("Strings/Objects", "MagicInk"),
        ["BearPaw"] = ("Strings/Objects", "BearPaw"),
        ["DwarvishTranslationGuide"] = ("Strings/Objects", "DwarvishTranslationGuide_Name"),
        ["ForestMagic"] = ("Strings/1_6_Strings", "ForestMagic"),
        ["TownKey"] = ("Strings/1_6_Strings", "TownKey"),
        ["Book_Stars"] = ("Strings/1_6_Strings", "Book_Stars"),
        ["Book_Defense"] = ("Strings/1_6_Strings", "Book_Defense"),
        ["Book_Friendship"] = ("Strings/1_6_Strings", "Book_Friendship"),
        ["Book_WildSeeds"] = ("Strings/1_6_Strings", "Book_WildSeeds"),
        ["Book_Woodcutting"] = ("Strings/1_6_Strings", "Book_Woodcutting"),
        ["Book_Speed"] = ("Strings/1_6_Strings", "Book_Speed"),
        ["Book_Bombs"] = ("Strings/1_6_Strings", "Book_Bombs"),
        ["Book_Crab"] = ("Strings/1_6_Strings", "Book_Crab"),
        ["Book_Mystery"] = ("Strings/1_6_Strings", "Book_Mystery"),
        ["Book_Diamonds"] = ("Strings/1_6_Strings", "Book_Diamonds"),
        ["Book_Roe"] = ("Strings/1_6_Strings", "Book_Roe"),
        ["Book_Trash"] = ("Strings/1_6_Strings", "Book_Trash"),
        ["Book_PriceCatalogue"] = ("Strings/1_6_Strings", "Book_PriceCatalogue"),
        ["Book_Grass"] = ("Strings/1_6_Strings", "Book_Grass"),
        ["Book_Animal"] = ("Strings/1_6_Strings", "Book_Animal"),
        ["Book_Monsters"] = ("Strings/1_6_Strings", "Book_Monsters"),
        ["Book_Artifact"] = ("Strings/1_6_Strings", "Book_Artifact"),
        ["Book_Horse"] = ("Strings/1_6_Strings", "Book_Horse")
    };

    /// <summary>
    /// Gets the vanilla internal profession identifier from its integer index (0..29).
    /// </summary>
    public static string? GetProfessionInternalName(int whichProfession)
    {
        return whichProfession switch
        {
            0 => "Rancher",
            1 => "Tiller",
            2 => "Coopmaster",
            3 => "Shepherd",
            4 => "Artisan",
            5 => "Agriculturist",
            6 => "Fisher",
            7 => "Trapper",
            8 => "Angler",
            9 => "Pirate",
            10 => "Mariner",
            11 => "Luremaster",
            12 => "Forester",
            13 => "Gatherer",
            14 => "Lumberjack",
            15 => "Tapper",
            16 => "Botanist",
            17 => "Tracker",
            18 => "Miner",
            19 => "Geologist",
            20 => "Blacksmith",
            21 => "Prospector",
            22 => "Excavator",
            23 => "Gemologist",
            24 => "Fighter",
            25 => "Scout",
            26 => "Brute",
            27 => "Defender",
            28 => "Acrobat",
            29 => "Desperado",
            _ => null
        };
    }

    /// <summary>
    /// Gets the localization key in Strings/StringsFromCSFiles for the given skill index.
    /// </summary>
    public static string? GetSkillStringKey(int skillIndex)
    {
        return skillIndex switch
        {
            0 => "Farmer.cs.1991",
            1 => "Farmer.cs.1993",
            2 => "Farmer.cs.1994",
            3 => "Farmer.cs.1992",
            4 => "Farmer.cs.1996",
            5 => "Farmer.cs.1995",
            _ => null
        };
    }

    /// <summary>
    /// Gets the default English skill name for the given skill index.
    /// </summary>
    public static string? GetSkillDefaultName(int skillIndex)
    {
        return skillIndex switch
        {
            0 => "Farming",
            1 => "Fishing",
            2 => "Foraging",
            3 => "Mining",
            4 => "Combat",
            5 => "Luck",
            _ => null
        };
    }

    /// <summary>
    /// Attempts to look up the known vanilla asset and key for a special power entry.
    /// </summary>
    public static bool TryGetVanillaPowerSource(string powerKey, out (string Asset, string Key) source)
    {
        return VanillaPowerSources.TryGetValue(powerKey, out source);
    }
}
