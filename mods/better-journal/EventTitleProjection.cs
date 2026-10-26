using System.Globalization;
using StardewModdingAPI;
using StardewValley;

namespace BetterJournal;

internal sealed class EventTitleProjection
{
    private readonly ITranslationHelper translations;

    public EventTitleProjection(ITranslationHelper translations)
    {
        this.translations = translations;
    }

    public IReadOnlyList<EventRow> Project(IEnumerable<EventCandidate> candidates)
    {
        return EventRow.Group(candidates.Select(candidate =>
        {
            string location = EventDestinations.GetDisplayName(candidate.Destination, translations);
            string title = T("events.hint.visit", new { location });
            return new EventTitle(candidate.DestinationName, candidate.EventId, title, GetInstructions(candidate.VisitRequirements));
        }), Describe, GetDisplayComparer());
    }

    private IReadOnlyList<string> Describe(string title, IReadOnlyList<EventTriggerGroup> triggers)
    {
        return triggers.Select((trigger, index) =>
        {
            bool alternatives = trigger.Alternatives.Count > 1;
            var first = trigger.Alternatives[0];
            string schedule = alternatives ? "" : first.Schedule;
            string visit = schedule.Length > 0 ? T(!alternatives && first.HasAside
                ? "events.description.visit.aside" : "events.description.visit", new { title, schedule }) : title;
            var sentences = new List<string>
            {
                T($"events.description.{(index == 0 ? "first" : "next")}.{(trigger.Count == 1 ? "single" : "multiple")}",
                    new { visit, schedule = schedule.Length > 0 ? schedule : T("events.requirement.here"), count = trigger.Count })
            };
            if (!alternatives)
                sentences.AddRange(first.Conditions.Select(condition => trigger.Count == 1 ? condition.Single : condition.Multiple));
            else
            {
                for (int route = 0; route < trigger.Alternatives.Count; route++)
                {
                    var instructions = trigger.Alternatives[route];
                    sentences.Add(T(route == 0 ? "events.description.option" : "events.description.alternative",
                        new { schedule = instructions.Schedule.Length > 0 ? instructions.Schedule : T("events.requirement.there") }));
                    sentences.AddRange(instructions.Conditions.Select(condition => trigger.Count == 1 ? condition.Single : condition.Multiple));
                }
            }
            return string.Join(" ", sentences);
        }).ToArray();
    }

    private StringComparer GetDisplayComparer()
    {
        string locale = translations.Locale;
        try
        {
            return StringComparer.Create(CultureInfo.GetCultureInfo(locale.Length == 0 ? "en" : locale), ignoreCase: false);
        }
        catch (CultureNotFoundException)
        {
            return StringComparer.InvariantCulture;
        }
    }

    private EventInstructions GetInstructions(IReadOnlyList<EventVisitRequirement> requirements)
    {
        var schedule = new List<string>();
        var sentences = new List<EventConditionText>();
        // Calendar sets are already intersected and complemented by the parser, not by prose heuristics.
        foreach (var requirement in requirements.Where(requirement => requirement.Kind is EventVisitKind.Weekday or EventVisitKind.Season)
            .OrderBy(requirement => requirement.Kind))
        {
            bool weekday = requirement.Kind == EventVisitKind.Weekday;
            schedule.Add(FormatCalendar(requirement, weekday ? EventVisitRequirements.Weekdays : EventVisitRequirements.Seasons,
                weekday ? "events.schedule.weekdays" : "events.schedule.seasons",
                value => weekday ? T("events.weekday." + value) : Utility.getSeasonNameFromNumber(int.Parse(value))));
        }
        string calendar = string.Join(" ", schedule);
        schedule.Clear();
        if (calendar.Length > 0)
            schedule.Add(calendar);
        var weather = requirements.Where(requirement => requirement.Kind == EventVisitKind.Weather).ToArray();
        if (weather.Length == 1)
            schedule.Add(FormatWeather(weather[0], calendar.Length > 0));
        var times = requirements.Where(requirement => requirement.Kind == EventVisitKind.Time && !requirement.Negated).ToArray();
        if (times.Length > 0)
            schedule.Add(FormatTime(times.Max(time => int.Parse(time.Values[0])), times.Min(time => int.Parse(time.Values[1])), false));

        foreach (var requirement in requirements
            .Where(requirement => requirement.Kind is not (EventVisitKind.Weekday or EventVisitKind.Season or EventVisitKind.NpcHere or EventVisitKind.NpcAvailable))
            .Where(requirement => requirement.Kind != EventVisitKind.Time || requirement.Negated)
            .Where(requirement => requirement.Kind != EventVisitKind.Weather || weather.Length != 1)
            .OrderBy(requirement => requirement.Kind)
            .ThenBy(requirement => requirement.Negated)
            .ThenBy(requirement => string.Join(" ", requirement.Values), StringComparer.Ordinal))
            sentences.Add(Availability(FormatCondition(requirement)));

        foreach (var people in requirements.Where(requirement => requirement.Kind is EventVisitKind.NpcHere or EventVisitKind.NpcAvailable)
            .GroupBy(requirement => (requirement.Kind, requirement.Negated))
            .OrderBy(group => group.Key.Kind).ThenBy(group => group.Key.Negated))
        {
            var names = people.Select(requirement => requirement.Values[0]).Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal).Select(NpcName).ToArray();
            string number = names.Length == 1 ? ".single" : ".multiple";
            var tokens = new { npcs = FormatList(names, "and") };
            if (people.Key.Kind == EventVisitKind.NpcHere && !people.Key.Negated)
                schedule.Add(T("events.schedule.npc-here" + number, tokens));
            else
                sentences.Add(Availability(T("events.requirement." + (people.Key.Kind == EventVisitKind.NpcHere ? "npc-here" : "npc-available")
                    + number + (people.Key.Negated ? ".not" : ""), tokens)));
        }
        return new EventInstructions(string.Join(T("events.list.separator"), schedule.Where(clause => clause.Length > 0)),
            sentences.DistinctBy(condition => condition.Single, StringComparer.Ordinal).ToArray(), schedule.Count(clause => clause.Length > 0) > 1);
    }

    private EventConditionText Availability(string requirements) => new(
        T("events.availability.single", new { requirements }), T("events.availability.multiple", new { requirements }));

    private string FormatCalendar(EventVisitRequirement requirement, IReadOnlyList<string> universe, string key,
        Func<string, string> display)
    {
        var excluded = universe.Except(requirement.Values, StringComparer.Ordinal).ToArray();
        bool exceptions = excluded.Length < requirement.Values.Length;
        var selected = exceptions ? excluded : requirement.Values;
        return T(key + (exceptions ? ".except" : ""), new { values = FormatList(selected.Select(display).ToArray(), "or") });
    }

    private string FormatWeather(EventVisitRequirement requirement, bool hasCalendar)
    {
        string suffix = requirement.Negated ? ".not" : "";
        if (requirement.Values[0] == "rainy")
            return T("events.schedule.rain" + (hasCalendar ? ".with-calendar" : "") + suffix);
        string key = "events.weather." + requirement.Values[0];
        return T("events.schedule.weather" + suffix,
            new { weather = translations.ContainsKey(key) ? T(key) : requirement.Values[0] });
    }

    private string FormatCondition(EventVisitRequirement requirement)
    {
        string suffix = requirement.Negated ? ".not" : "";
        var values = requirement.Values;
        switch (requirement.Kind)
        {
            case EventVisitKind.Time:
                return FormatTime(int.Parse(values[0]), int.Parse(values[1]), true);
            case EventVisitKind.Weather:
                if (values[0] == "rainy")
                    return T("events.requirement.rain" + suffix);
                string weatherKey = "events.weather." + values[0];
                return T("events.requirement.weather" + suffix,
                    new { weather = translations.ContainsKey(weatherKey) ? T(weatherKey) : values[0] });
            case EventVisitKind.DayOfMonth:
                var excluded = EventVisitRequirements.MonthDays.Except(values, StringComparer.Ordinal).ToArray();
                bool exceptions = excluded.Length < values.Length;
                var days = exceptions ? excluded : values;
                return T("events.requirement.day." + (days.Length == 1 ? "single" : "multiple") + (exceptions ? ".except" : ""),
                    new { days = FormatList(days, "or") });
            case EventVisitKind.Festival:
                return T("events.requirement.festival" + suffix);
            case EventVisitKind.UpcomingFestival:
                int ahead = int.Parse(values[0]) - 1;
                return ahead == 0 ? T("events.requirement.festival" + suffix)
                    : T("events.requirement.upcoming." + (ahead == 1 ? "single" : "multiple") + suffix, new { days = ahead });
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private string FormatTime(int start, int end, bool negated)
    {
        bool hasStart = start > 600;
        bool hasEnd = end < 2600;
        string range = start == end ? "at" : (hasStart, hasEnd, negated) switch
        {
            (true, true, _) => negated ? "outside" : "between",
            (true, false, _) => negated ? "before" : "after",
            (false, true, _) => negated ? "after" : "before",
            _ => negated ? "outside" : ""
        };
        if (range.Length == 0)
            return "";
        if (start == end && negated)
            range = "not-at";
        if (negated && hasStart != hasEnd)
            (start, end) = (end, start);
        return T("events.requirement.time." + range,
            new { start = Game1.getTimeOfDayString(start), end = Game1.getTimeOfDayString(end) });
    }

    private static string NpcName(string name)
    {
        string? displayName = Game1.getCharacterFromName(name, mustBeVillager: false)?.displayName;
        return string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }

    private string FormatList(IReadOnlyList<string> values, string conjunction) => values.Count switch
    {
        0 => "",
        1 => values[0],
        2 => T("events.list.two." + conjunction, new { first = values[0], last = values[1] }),
        _ => T("events.list.many." + conjunction, new
        {
            items = string.Join(T("events.list.separator"), values.Take(values.Count - 1)),
            last = values[^1]
        })
    };

    private string T(string key, object? tokens = null) => translations.Get(key, tokens);
}
