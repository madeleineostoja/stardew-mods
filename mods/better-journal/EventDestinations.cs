using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace BetterJournal;

internal static class EventDestinations
{
    public static bool IsAccessible(GameLocation destination)
    {
        string name = destination.Name;
        bool island = name is "IslandSouth" or "IslandEast" or "IslandHut" or "IslandNorth"
            or "IslandWest" or "IslandSouthEast" or "IslandSouthEastCave" or "IslandWestCave1"
            or "IslandFarmCave" or "IslandFarmHouse" or "IslandFieldOffice" or "IslandNorthCave1"
            or "CaptainRoom" or "QiNutRoom" or "Caldera";
        if (island && !Game1.MasterPlayer.mailReceived.Contains("willyBoatFixed"))
            return false;

        // These are progression gates, not opening hours or a search for traversable warp paths.
        return name switch
        {
            "BoatTunnel" => Game1.player.mailReceived.Contains("willyBackRoomInvitation"),
            "Sunroom" => Game1.player.getFriendshipHeartLevelForNPC("Caroline") >= 2,
            "Mine" => Game1.stats.DaysPlayed >= 5,
            "QiNutRoom" => IslandWest.IsQiWalnutRoomDoorUnlocked(out _),
            "IslandFarmHouse" => Game1.locations.OfType<IslandWest>().Any(west => west.farmhouseRestored.Value),
            "Railroad" => Game1.stats.DaysPlayed >= 31,
            _ when name.StartsWith("BathHouse", StringComparison.Ordinal) => Game1.stats.DaysPlayed >= 31,
            "Desert" or "SandyHouse" => Game1.isLocationAccessible("Desert"),
            "SkullCave" => Game1.isLocationAccessible("Desert") && Game1.player.hasSkullKey,
            "Club" => Game1.isLocationAccessible("Desert") && Game1.player.hasClubCard,
            "Sewer" => Game1.player.hasRustyKey || Game1.player.mailReceived.Contains("OpenedSewer"),
            "WitchSwamp" => CanReachWitchSwamp(),
            "WitchHut" => CanReachWitchSwamp() && Game1.MasterPlayer.mailReceived.Contains("henchmanGone"),
            _ => true
        };
    }

    private static bool CanReachWitchSwamp() => Game1.stats.DaysPlayed >= 31
        && (Game1.player.hasDarkTalisman || Game1.MasterPlayer.mailReceived.Contains("witchStatueGone")
            || Game1.locations.OfType<Railroad>().Any(railroad => railroad.witchStatueGone.Value));

    public static string GetDisplayName(GameLocation destination, ITranslationHelper translations)
    {
        if (destination.Name == "Town")
            return translations.Get("events.location.Town");
        string name = destination.DisplayName;
        if (!string.IsNullOrWhiteSpace(name) && name != destination.Name && name != destination.NameOrUniqueName)
            return name;

        string key = "events.location." + destination.Name;
        if (translations.ContainsKey(key))
            return translations.Get(key);

        // Unknown mod locations can still expose a readable fallback without an ID mapping.
        return Regex.Replace(destination.Name.Replace('_', ' '), @"(?<=[a-z0-9])(?=[A-Z])", " ");
    }
}
