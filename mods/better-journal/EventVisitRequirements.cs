using System.Globalization;
using System.Reflection;
using StardewValley;
using StardewValley.Delegates;

namespace BetterJournal;

internal enum EventVisitKind { Time, Weather, Weekday, Season, DayOfMonth, Festival, UpcomingFestival, NpcHere, NpcAvailable }

internal sealed record EventVisitRequirement(EventVisitKind Kind, bool Negated, string[] Values);

internal static class EventVisitRequirements
{
#pragma warning disable CS0618
    private static readonly Dictionary<MethodInfo, (EventVisitKind Kind, bool Inverted)> Kinds = new()
    {
        [((EventPreconditionDelegate)Preconditions.Time).Method] = (EventVisitKind.Time, false),
        [((EventPreconditionDelegate)Preconditions.Weather).Method] = (EventVisitKind.Weather, false),
        [((EventPreconditionDelegate)Preconditions.DayOfWeek).Method] = (EventVisitKind.Weekday, false),
        [((EventPreconditionDelegate)Preconditions.NotDayOfWeek).Method] = (EventVisitKind.Weekday, true),
        [((EventPreconditionDelegate)Preconditions.Season).Method] = (EventVisitKind.Season, false),
        [((EventPreconditionDelegate)Preconditions.NotSeason).Method] = (EventVisitKind.Season, true),
        [((EventPreconditionDelegate)Preconditions.DayOfMonth).Method] = (EventVisitKind.DayOfMonth, false),
        [((EventPreconditionDelegate)Preconditions.FestivalDay).Method] = (EventVisitKind.Festival, false),
        [((EventPreconditionDelegate)Preconditions.NotFestivalDay).Method] = (EventVisitKind.Festival, true),
        [((EventPreconditionDelegate)Preconditions.UpcomingFestival).Method] = (EventVisitKind.UpcomingFestival, false),
        [((EventPreconditionDelegate)Preconditions.NotUpcomingFestival).Method] = (EventVisitKind.UpcomingFestival, true),
        [((EventPreconditionDelegate)Preconditions.NpcVisibleHere).Method] = (EventVisitKind.NpcHere, false),
        [((EventPreconditionDelegate)Preconditions.NpcVisible).Method] = (EventVisitKind.NpcAvailable, false)
    };
#pragma warning restore CS0618

    // Seasons gate eligibility, but stay in the parsed instructions to explain the event's schedule.
    public static readonly IReadOnlySet<MethodInfo> DeferredMethods = Kinds
        .Where(pair => pair.Value.Kind != EventVisitKind.Season).Select(pair => pair.Key).ToHashSet();
    public static readonly string[] Weekdays = Enum.GetValues<DayOfWeek>().OrderBy(day => ((int)day + 6) % 7).Select(day => day.ToString()).ToArray();
    public static readonly string[] Seasons = Enum.GetValues<Season>().Select(season => ((int)season).ToString(CultureInfo.InvariantCulture)).ToArray();
    public static readonly string[] MonthDays = Enumerable.Range(1, 28).Select(day => day.ToString(CultureInfo.InvariantCulture)).ToArray();

    public static IReadOnlyList<EventVisitRequirement> Parse(IReadOnlyList<ResolvedEventCondition> conditions)
    {
        var requirements = new List<EventVisitRequirement>();
        foreach (var condition in conditions)
        {
            if (!Kinds.TryGetValue(condition.Handler.Method, out var type))
                continue;
            bool negated = condition.Negated ^ type.Inverted;
            var args = condition.Arguments;
            string Required(int index) => index < args.Length && !string.IsNullOrWhiteSpace(args[index])
                ? args[index] : throw new FormatException("Missing visit requirement argument.");
            string Number(int index) => int.Parse(Required(index), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            string[] values;
            switch (type.Kind)
            {
                case EventVisitKind.Time:
                    values = new[] { Number(1), Number(2) };
                    if (int.Parse(values[0]) > int.Parse(values[1]))
                        throw new FormatException("Inverted event time window.");
                    break;
                case EventVisitKind.Weekday:
                    Required(1);
                    values = args.Skip(1).Select(day => WorldDate.TryGetDayOfWeekFor(day, out var parsed)
                        ? parsed.ToString() : throw new FormatException("Invalid event weekday."))
                        .Distinct().OrderBy(day => day, StringComparer.Ordinal).ToArray();
                    break;
                case EventVisitKind.Season:
                    // Vanilla's Season handler consults the first argument even when more are supplied.
                    if (!Enum.TryParse<Season>(Required(1), true, out var season) || !Enum.IsDefined(typeof(Season), season))
                        throw new FormatException("Invalid event season.");
                    values = new[] { ((int)season).ToString(CultureInfo.InvariantCulture) };
                    break;
                case EventVisitKind.DayOfMonth:
                    Required(1);
                    values = Enumerable.Range(1, args.Length - 1).Select(Number).Distinct()
                        .OrderBy(day => int.Parse(day)).ToArray();
                    break;
                case EventVisitKind.Festival:
                    values = Array.Empty<string>();
                    break;
                case EventVisitKind.UpcomingFestival:
                    values = new[] { Number(1) };
                    if (int.Parse(values[0]) < 1)
                        throw new FormatException("Invalid festival window.");
                    break;
                default:
                    values = new[] { Required(1) };
                    break;
            }
            if (type.Kind == EventVisitKind.Weather && values[0] == "sunny")
            {
                // Vanilla "sunny" means not raining, which can also include snowy weather.
                values[0] = "rainy";
                negated = !negated;
            }
            requirements.Add(new EventVisitRequirement(type.Kind, negated, values));
        }
        var times = requirements.Where(requirement => requirement.Kind == EventVisitKind.Time && !requirement.Negated).ToArray();
        if (times.Length > 0 && times.Max(time => int.Parse(time.Values[0])) > times.Min(time => int.Parse(time.Values[1])))
            throw new FormatException("Conflicting event time windows.");
        NormalizeCalendar(requirements, EventVisitKind.Weekday, Weekdays);
        NormalizeCalendar(requirements, EventVisitKind.Season, Seasons);
        NormalizeCalendar(requirements, EventVisitKind.DayOfMonth, MonthDays);
        return requirements;
    }

    private static void NormalizeCalendar(List<EventVisitRequirement> requirements, EventVisitKind kind, string[] universe)
    {
        var filters = requirements.Where(requirement => requirement.Kind == kind).ToArray();
        if (filters.Length == 0)
            return;
        var allowed = new HashSet<string>(universe, StringComparer.Ordinal);
        foreach (var filter in filters)
        {
            if (filter.Negated)
                allowed.ExceptWith(filter.Values);
            else
                allowed.IntersectWith(filter.Values);
        }
        if (allowed.Count == 0)
            throw new FormatException("Conflicting event calendar requirements.");
        requirements.RemoveAll(requirement => requirement.Kind == kind);
        if (allowed.Count < universe.Length)
            requirements.Add(new EventVisitRequirement(kind, false, universe.Where(allowed.Contains).ToArray()));
    }
}
