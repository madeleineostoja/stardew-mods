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
                new EventTitle(candidate.DestinationName, candidate.EventId, GetTitle(candidate))),
            (title, count) => translations.Get("events.hint.count", new { title, count }), GetDisplayComparer());
    }

    private StringComparer GetDisplayComparer()
    {
        // The game thread uses invariant culture independently of the display language.
        string locale = translations.Locale;
        try
        {
            return StringComparer.Create(CultureInfo.GetCultureInfo(locale.Length == 0 ? "en" : locale), ignoreCase: false);
        }
        catch (CultureNotFoundException)
        {
            // Custom language packs may use locale codes which .NET doesn't recognize.
            return StringComparer.InvariantCulture;
        }
    }

    private string GetTitle(EventCandidate candidate)
    {
        var window = candidate.TimeWindow;
        bool hasStart = window is not null && window.Start > 600;
        bool hasEnd = window is not null && window.End < 2600;
        string range = (hasStart, hasEnd) switch
        {
            (true, true) => "between",
            (true, false) => "after",
            (false, true) => "before",
            _ => "enter"
        };

        var names = candidate.PresentNpcNames
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name =>
            {
                string? displayName = Game1.getCharacterFromName(name, mustBeVillager: false)?.displayName;
                return string.IsNullOrWhiteSpace(displayName) ? name : displayName;
            })
            .ToArray();

        // Complete templates let translations reorder every clause, rather than joining English fragments.
        return translations.Get($"events.hint.{range}{(names.Length > 0 ? ".npcs" : "")}", new
        {
            location = candidate.Destination.DisplayName,
            start = hasStart ? Game1.getTimeOfDayString(window!.Start) : "",
            end = hasEnd ? Game1.getTimeOfDayString(window!.End) : "",
            npcs = FormatNames(names)
        });
    }

    private string FormatNames(string[] names)
    {
        return names.Length switch
        {
            0 => "",
            1 => names[0],
            2 => translations.Get("events.npcs.two", new { first = names[0], last = names[1] }),
            _ => translations.Get("events.npcs.many", new
            {
                names = string.Join(translations.Get("events.npcs.separator").ToString(), names[..^1]),
                last = names[^1]
            })
        };
    }
}
