using System.Text.RegularExpressions;

namespace BetterJournal;

internal sealed record EventConditionText(string Single, string Multiple);

internal sealed record EventInstructions(string Schedule, IReadOnlyList<EventConditionText> Conditions, bool HasAside)
{
    public string Key => string.Join("\n", new[] { Schedule }.Concat(Conditions.Select(condition => condition.Single)));
    public bool IsUnrestricted => Schedule.Length == 0 && Conditions.Count == 0;
}

internal sealed record EventTitle(string DestinationName, string EventId, string Title, EventInstructions Requirements);
internal sealed record EventTriggerGroup(IReadOnlyList<EventInstructions> Alternatives, int Count);

internal sealed record EventRow(string Title, int Count, IReadOnlyList<string> Details)
{
    public static IReadOnlyList<EventRow> Group(IEnumerable<EventTitle> titles,
        Func<string, IReadOnlyList<EventTriggerGroup>, IReadOnlyList<string>> describe, StringComparer displayComparer)
    {
        return titles
            .GroupBy(title => title.DestinationName, StringComparer.Ordinal)
            .Select(group =>
            {
                var events = group.GroupBy(entry => entry.EventId, StringComparer.Ordinal)
                    .Select(headers =>
                    {
                        var alternatives = headers.Select(header => new EventInstructions(Normalize(header.Requirements.Schedule),
                                header.Requirements.Conditions.Select(condition => new EventConditionText(
                                    Normalize(condition.Single), Normalize(condition.Multiple))).ToArray(), header.Requirements.HasAside))
                            .DistinctBy(instructions => instructions.Key, StringComparer.Ordinal)
                            .OrderBy(instructions => instructions.Key, StringComparer.Ordinal).ToArray();
                        // An unlocked unconditional header makes other routes to that same event redundant.
                        var unrestricted = alternatives.FirstOrDefault(instructions => instructions.IsUnrestricted);
                        return unrestricted is not null ? new[] { unrestricted } : alternatives;
                    }).ToArray();
                var triggers = events.GroupBy(alternatives => string.Join("\n\n", alternatives.Select(instructions => instructions.Key)), StringComparer.Ordinal)
                    .OrderBy(trigger => trigger.Key, displayComparer)
                    .ThenBy(trigger => trigger.Key, StringComparer.Ordinal)
                    .Select(trigger => new EventTriggerGroup(trigger.First(), trigger.Count()))
                    .ToArray();
                string title = Normalize(group.First().Title);
                return new EventRow(title, events.Length, describe(title, triggers).Select(Normalize).ToArray());
            })
            .OrderBy(row => row.Title, displayComparer)
            .ThenBy(row => row.Title, StringComparer.Ordinal)
            .ToArray();
    }

    // Both the journal font and description text treat carets as line breaks.
    private static string Normalize(string title) => Regex.Replace(title, @"[\s^]+", " ").Trim();
}
