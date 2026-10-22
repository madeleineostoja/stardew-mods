using System.Globalization;
using Xunit;

namespace BetterJournal.Tests;

public sealed class EventRowTests
{
    private static EventInstructions At(string schedule) => new(schedule, Array.Empty<EventConditionText>(), false);
    [Fact]
    public void KeepsAlternativeRoutesAndCountsEventsWithoutAddingCountsToRows()
    {
        var rows = EventRow.Group(new[]
        {
            new EventTitle("Town", "1", "Visit Pelican Town", At("before noon")),
            new EventTitle("Town", "1", "Visit Pelican Town", At("after 8pm")),
            new EventTitle("Town", "2", "Visit Pelican Town", At("before noon")),
            new EventTitle("Town", "2", "Visit Pelican Town", At("after 8pm")),
            new EventTitle("Town", "3", "Visit Pelican Town", At("while Caroline\r\nis there")),
            new EventTitle("Town", "3", "Visit Pelican Town", At("while Caroline^is there")),
            new EventTitle("OtherTown", "1", "Visit another town", At("before noon"))
        }, (_, triggers) => triggers.Select(trigger => $"{trigger.Count}: {string.Join(" OR ", trigger.Alternatives.Select(route => route.Schedule))}").ToArray(),
            StringComparer.InvariantCulture);

        Assert.Equal(2, rows.Count);
        var town = Assert.Single(rows, row => row.Title == "Visit Pelican Town");
        Assert.Equal(3, town.Count);
        Assert.Equal(new[] { "2: after 8pm OR before noon", "1: while Caroline is there" }, town.Details);
        var other = Assert.Single(rows, row => row.Title == "Visit another town");
        Assert.Equal(1, other.Count);
        Assert.Equal("1: before noon", Assert.Single(other.Details));
    }

    [Fact]
    public void SortsWithDisplayLocaleInsteadOfThreadCultureAndBreaksCollationTiesOrdinally()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var titles = new[]
            {
                new EventTitle("A", "1", "Visit Ä", At("")),
                new EventTitle("Z", "2", "Visit Z", At("")),
                new EventTitle("e", "3", "Visit é", At("")),
                new EventTitle("accent", "4", "Visit e\u0301", At(""))
            };

            string[] Project(string locale) => EventRow.Group(titles, (_, _) => Array.Empty<string>(),
                    StringComparer.Create(CultureInfo.GetCultureInfo(locale), ignoreCase: false))
                .Select(row => row.Title).ToArray();

            Assert.Equal(new[] { "Visit e\u0301", "Visit é", "Visit Z", "Visit Ä" }, Project("sv-SE"));
            Assert.Equal(new[] { "Visit Ä", "Visit e\u0301", "Visit é", "Visit Z" }, Project("de-DE"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
