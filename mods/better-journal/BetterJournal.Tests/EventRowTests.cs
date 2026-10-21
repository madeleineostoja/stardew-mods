using System.Globalization;
using Xunit;

namespace BetterJournal.Tests;

public sealed class EventRowTests
{
    [Fact]
    public void GroupsFullNormalizedTitlesAndCountsDistinctDestinationEventPairs()
    {
        string prefix = $"Enter {new string('x', 180)} ";
        var rows = EventRow.Group(new[]
        {
            new EventTitle("Town", "1", prefix + "with Abigail\r\npresent"),
            new EventTitle("Town", "1", prefix + "with Abigail^present"),
            new EventTitle("Town", "2", prefix + "with Abigail present"),
            new EventTitle("OtherTown", "1", prefix + "with Abigail present"),
            new EventTitle("Town", "3", prefix + "with Alex present")
        }, (title, count) => $"{title} ({count})", StringComparer.InvariantCulture);

        Assert.Equal(new[]
        {
            new EventRow(prefix + "with Abigail present (3)", 3),
            new EventRow(prefix + "with Alex present", 1)
        }, rows);
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
                new EventTitle("Town", "1", "Enter Ä"),
                new EventTitle("Town", "2", "Enter Z"),
                new EventTitle("Town", "3", "Enter é"),
                new EventTitle("Town", "4", "Enter e\u0301")
            };

            string[] Project(string locale) => EventRow.Group(titles, (title, count) => $"{title} ({count})",
                    StringComparer.Create(CultureInfo.GetCultureInfo(locale), ignoreCase: false))
                .Select(row => row.Title).ToArray();

            Assert.Equal(new[] { "Enter e\u0301", "Enter é", "Enter Z", "Enter Ä" }, Project("sv-SE"));
            Assert.Equal(new[] { "Enter Ä", "Enter e\u0301", "Enter é", "Enter Z" }, Project("de-DE"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
