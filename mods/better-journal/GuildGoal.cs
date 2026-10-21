using StardewValley;
using StardewValley.Locations;
using StardewValley.TokenizableStrings;

namespace BetterJournal;

internal sealed record GuildGoal(string Id, string Name, int Count, int Target, bool Complete)
{
    public static bool HasVisitedGuild => Game1.player.mailReceived.Contains("guildMember");

    public static List<GuildGoal> Read()
    {
        if (!HasVisitedGuild)
            return new List<GuildGoal>();

        return DataLoader.MonsterSlayerQuests(Game1.content).Select(pair =>
        {
            var goal = pair.Value;
            int count = goal.Targets?.Sum(target => Game1.stats.getMonstersKilled(target)) ?? 0;
            return new GuildGoal(pair.Key, count == 0 ? "????" : TokenParser.ParseText(goal.DisplayName),
                count, goal.Count, AdventureGuild.IsComplete(goal));
        }).ToList();
    }
}
