using System.Reflection;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;

namespace BetterJournal;

internal sealed record EventTimeWindow(int Start, int End);

internal sealed record EventCandidate(
    string DestinationName,
    GameLocation Destination,
    string EventId,
    string Header,
    EventTimeWindow? TimeWindow,
    IReadOnlyList<string> PresentNpcNames);

internal sealed class EventLookup
{
    // Method identity, not a condition-name allowlist: the game owns canonical names and aliases.
    // Legacy negative methods remain supported with their vanilla semantics.
#pragma warning disable CS0618
    private static readonly HashSet<MethodInfo> AllowedMethods = new EventPreconditionDelegate[]
    {
        Preconditions.SawEvent, Preconditions.NotSawEvent, Preconditions.SawSecretNote,
        Preconditions.ChoseDialogueAnswers, Preconditions.ActiveDialogueEvent, Preconditions.NotActiveDialogueEvent,
        Preconditions.HostMail, Preconditions.NotHostMail, Preconditions.HostOrLocalMail, Preconditions.NotHostOrLocalMail,
        Preconditions.LocalMail, Preconditions.NotLocalMail, Preconditions.WorldState,
        Preconditions.EarnedMoney, Preconditions.HasMoney, Preconditions.FreeInventorySlots,
        Preconditions.CommunityCenterOrWarehouseDone, Preconditions.NotCommunityCenterOrWarehouseDone,
        Preconditions.JojaBundlesDone, Preconditions.Shipped, Preconditions.GoldenWalnuts,
        Preconditions.ReachedMineBottom, Preconditions.HasItem, Preconditions.Skill,
        Preconditions.MissingPet, Preconditions.Dating, Preconditions.Spouse, Preconditions.NotSpouse,
        Preconditions.Roommate, Preconditions.NotRoommate, Preconditions.InUpgradedHouse, Preconditions.SpouseBed,
        Preconditions.Gender, Preconditions.DaysPlayed, Preconditions.FestivalDay, Preconditions.NotFestivalDay,
        Preconditions.Time, Preconditions.Weather, Preconditions.DayOfWeek, Preconditions.NotDayOfWeek,
        Preconditions.Season, Preconditions.NotSeason, Preconditions.Year, Preconditions.DayOfMonth,
        Preconditions.UpcomingFestival, Preconditions.NotUpcomingFestival,
        Preconditions.NpcVisible, Preconditions.NpcVisibleHere, Preconditions.IsHost
    }.Select(handler => handler.Method).ToHashSet();
#pragma warning restore CS0618

    private static readonly MethodInfo HostMethod = ((EventPreconditionDelegate)Preconditions.IsHost).Method;
    private static readonly MethodInfo TimeMethod = ((EventPreconditionDelegate)Preconditions.Time).Method;
    private static readonly MethodInfo PresenceMethod = ((EventPreconditionDelegate)Preconditions.NpcVisibleHere).Method;
    private readonly EventDefinitionCache definitions;
    private readonly IMonitor monitor;

    public EventLookup(EventDefinitionCache definitions, IMonitor monitor)
    {
        this.definitions = definitions;
        this.monitor = monitor;
    }

    public IReadOnlyList<EventCandidate> FindCandidates()
    {
        var results = new List<EventCandidate>();
        if (!Context.IsWorldReady)
            return results;

        var visited = new HashSet<string>(StringComparer.Ordinal);
        Utility.ForEachLocation(destination =>
        {
            try
            {
                if (visited.Add(destination.NameOrUniqueName))
                    FindAtDestination(destination, results);
            }
            catch (Exception exception)
            {
                monitor.Log($"Better Journal could not discover events at '{destination.NameOrUniqueName}': {exception}", LogLevel.Warn);
            }
            return true;
        }, includeInteriors: true, includeGenerated: false);
        return results;
    }

    private void FindAtDestination(GameLocation destination, List<EventCandidate> results)
    {
        if (!destination.TryGetLocationEvents(out _, out var events))
            return;

        var matches = new Dictionary<string, EventCandidate>(StringComparer.Ordinal);
        foreach (var pair in events)
        {
            try
            {
                var definition = definitions.Get(pair.Key);
                if (definition is null
                    || Game1.player.eventsSeen.Contains(definition.EventId)
                    || Game1.eventsSeenSinceLastLocationChange.Contains(definition.EventId)
                    || !GameLocation.IsValidLocationEvent(pair.Key, pair.Value))
                    continue;

                var conditions = EventConditionEvaluation.Evaluate(definition.Conditions, AllowedMethods,
                    name => Event.TryGetPreconditionHandler(name, out var handler) ? handler : null,
                    (handler, args) => handler.Method == HostMethod
                        ? Game1.IsMasterGame
                        : ((EventPreconditionDelegate)handler)(destination, definition.EventId, args));
                if (conditions is null)
                    continue;

                if (!matches.TryGetValue(definition.EventId, out var previous)
                    || StringComparer.Ordinal.Compare(pair.Key, previous.Header) < 0)
                {
                    matches[definition.EventId] = new EventCandidate(destination.NameOrUniqueName, destination,
                        definition.EventId, pair.Key, GetTimeWindow(conditions), GetPresentNpcs(conditions));
                }
            }
            catch (Exception exception)
            {
                monitor.Log($"Better Journal could not evaluate event header '{pair.Key}' at '{destination.NameOrUniqueName}': {exception}", LogLevel.Warn);
            }
        }
        results.AddRange(matches.Values);
    }

    private static EventTimeWindow? GetTimeWindow(IReadOnlyList<ResolvedEventCondition> conditions)
    {
        EventTimeWindow? window = null;
        foreach (var condition in conditions.Where(condition => condition.Handler.Method == TimeMethod))
        {
            // A negated range may be disjoint and can't be represented by a single window.
            if (condition.Negated
                || !ArgUtility.TryGetInt(condition.Arguments, 1, out int start, out _)
                || !ArgUtility.TryGetInt(condition.Arguments, 2, out int end, out _))
                return null;

            window = window is null ? new(start, end) : new(Math.Max(window.Start, start), Math.Min(window.End, end));
        }
        return window;
    }

    private static IReadOnlyList<string> GetPresentNpcs(IReadOnlyList<ResolvedEventCondition> conditions)
    {
        return conditions
            .Where(condition => !condition.Negated && condition.Handler.Method == PresenceMethod)
            .Select(condition => condition.Arguments[1])
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
