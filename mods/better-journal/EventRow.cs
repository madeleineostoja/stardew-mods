using System.Text.RegularExpressions;

namespace BetterJournal;

internal sealed record EventTitle(string DestinationName, string EventId, string Title);

internal sealed record EventRow(string Title, int Count)
{
    public static IReadOnlyList<EventRow> Group(IEnumerable<EventTitle> titles, Func<string, int, string> formatCount,
        StringComparer displayComparer)
    {
        return titles
            .GroupBy(title => Normalize(title.Title), StringComparer.Ordinal)
            .OrderBy(group => group.Key, displayComparer)
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                int count = group.Select(title => (title.DestinationName, title.EventId)).Distinct().Count();
                return new EventRow(count > 1 ? Normalize(formatCount(group.Key, count)) : group.Key, count);
            })
            .ToArray();
    }

    // Both the journal font and hover text treat carets as line breaks.
    private static string Normalize(string title) => Regex.Replace(title, @"[\s^]+", " ").Trim();
}
