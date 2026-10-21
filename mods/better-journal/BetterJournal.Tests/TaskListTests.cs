using System.Text.Json;
using Xunit;

namespace BetterJournal.Tests;

public sealed class TaskListTests
{
    [Fact]
    public void SavingAndReloadingPreservesOrderIdentityAndCompletion()
    {
        var tasks = TaskList.Read(null);
        var first = tasks.Add("Upgrade the watering can");
        var second = tasks.Add("Buy strawberry seeds");
        tasks.Toggle(first.Id, 12);
        tasks.Rename(second.Id, "Bring hardwood to Robin");

        var loaded = TaskList.Read(tasks.Write());
        Assert.Equal(new[] { first.Id, second.Id }, loaded.Entries.Select(task => task.Id));
        Assert.Equal(12, loaded.Entries[0].CompletedOnDay);
        Assert.Equal("Bring hardwood to Robin", loaded.Entries[1].Text);
        Assert.Null(loaded.Entries[1].CompletedOnDay);
    }

    [Fact]
    public void CompletionStaysInPlaceBrieflyThenRemovesOnlyThatTask()
    {
        var tasks = TaskList.Read(null);
        var first = tasks.Add("First");
        var second = tasks.Add("Second");
        tasks.Toggle(first.Id, 8);

        Assert.False(tasks.AdvanceCompletions(TimeSpan.FromMilliseconds(600)));
        Assert.Equal(first.Id, tasks.Entries[0].Id);
        Assert.True(tasks.Entries[0].CompletedOnDay.HasValue);
        Assert.True(tasks.AdvanceCompletions(TimeSpan.FromMilliseconds(50)));
        Assert.Equal(second.Id, Assert.Single(tasks.Entries).Id);
    }

    [Fact]
    public void UncheckingDuringThePauseCancelsRemoval()
    {
        var tasks = TaskList.Read(null);
        var task = tasks.Add("Keep me");
        tasks.Toggle(task.Id, 8);
        tasks.AdvanceCompletions(TimeSpan.FromMilliseconds(400));
        tasks.Toggle(task.Id, 8);

        Assert.False(tasks.AdvanceCompletions(TimeSpan.FromSeconds(1)));
        Assert.Null(Assert.Single(tasks.Entries).CompletedOnDay);
    }

    [Fact]
    public void CheckingAgainRestartsThePause()
    {
        var tasks = TaskList.Read(null);
        var task = tasks.Add("Finish me");
        tasks.Toggle(task.Id, 8);
        tasks.AdvanceCompletions(TimeSpan.FromMilliseconds(400));
        tasks.Toggle(task.Id, 8);
        tasks.Toggle(task.Id, 8);

        Assert.False(tasks.AdvanceCompletions(TimeSpan.FromMilliseconds(400)));
        Assert.Single(tasks.Entries);
        Assert.True(tasks.AdvanceCompletions(TimeSpan.FromMilliseconds(250)));
        Assert.Empty(tasks.Entries);
    }

    [Fact]
    public void RemovingAnEarlierTaskDoesNotShortenAnotherTasksPause()
    {
        var tasks = TaskList.Read(null);
        var first = tasks.Add("First");
        var second = tasks.Add("Second");
        tasks.Toggle(first.Id, 8);
        tasks.AdvanceCompletions(TimeSpan.FromMilliseconds(400));
        tasks.Toggle(second.Id, 8);

        tasks.AdvanceCompletions(TimeSpan.FromMilliseconds(250));
        Assert.Equal(second.Id, Assert.Single(tasks.Entries).Id);
        Assert.False(tasks.AdvanceCompletions(TimeSpan.FromMilliseconds(399)));
        Assert.True(tasks.AdvanceCompletions(TimeSpan.FromMilliseconds(1)));
        Assert.Empty(tasks.Entries);
    }

    [Fact]
    public void ClosingOrReloadingClearsCheckedTasksWithoutWaitingForTheClock()
    {
        var tasks = TaskList.Read(null);
        tasks.Toggle(tasks.Add("Finished").Id, 20);
        var unfinished = tasks.Add("Still needed");
        var loaded = TaskList.Read(tasks.Write());

        Assert.True(loaded.ClearCompleted());
        Assert.Equal(unfinished.Id, Assert.Single(loaded.Entries).Id);
        Assert.True(tasks.ClearCompleted());
        Assert.False(tasks.AdvanceCompletions(TimeSpan.FromSeconds(1)));
        Assert.Equal(unfinished.Id, Assert.Single(tasks.Entries).Id);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{\"Id\":\"same\",\"Text\":\"One\"},{\"Id\":\"same\",\"Text\":\"Two\"}]")]
    [InlineData("[{\"Id\":\"one\",\"Text\":\" \"}]")]
    [InlineData("not json")]
    public void InvalidSavedDataIsRejectedInsteadOfSilentlyDiscarded(string json)
    {
        Assert.Throws<JsonException>(() => TaskList.Read(json));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("First\nSecond")]
    public void InvalidEditsLeaveTheExistingTaskIntact(string text)
    {
        var tasks = TaskList.Read(null);
        var task = tasks.Add("Keep me");
        Assert.Throws<ArgumentException>(() => tasks.Rename(task.Id, text));
        Assert.Equal("Keep me", Assert.Single(tasks.Entries).Text);
    }

    [Fact]
    public void RemovingOneTaskDoesNotChangeTheOthers()
    {
        var tasks = TaskList.Read(null);
        var first = tasks.Add("First");
        var second = tasks.Add("Second");
        var third = tasks.Add("Third");
        Assert.True(tasks.Remove(second.Id));
        Assert.Equal(new[] { first.Id, third.Id }, tasks.Entries.Select(task => task.Id));
    }
}
