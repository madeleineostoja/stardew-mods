using StardewValley;

namespace BetterJournal;

internal sealed record EventDefinition(string EventId, string[][] Conditions);

internal sealed class EventDefinitionCache
{
    private readonly Dictionary<string, EventDefinition?> definitions = new(StringComparer.Ordinal);

    public EventDefinition? Get(string header)
    {
        if (definitions.TryGetValue(header, out var definition))
            return definition;

        definition = Parse(header);
        definitions.Add(header, definition);
        return definition;
    }

    private static EventDefinition? Parse(string header)
    {
        // SplitPreconditions removes empty segments, so don't let a missing ID become a condition.
        if (string.IsNullOrWhiteSpace(header) || header.TrimStart().StartsWith('/'))
            return null;

        var parts = Event.SplitPreconditions(header);
        if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]) || parts[0] == "-1")
            return null;

        return new EventDefinition(parts[0], parts.Skip(1).Select(ArgUtility.SplitBySpaceQuoteAware).ToArray());
    }
}
