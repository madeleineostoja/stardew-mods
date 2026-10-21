using Xunit;

namespace BetterJournal.Tests;

public sealed class ListPositionTests
{
    [Fact]
    public void SelectionRevealsWholePagesRatherThanSlidingTheList()
    {
        var list = new ListPosition();
        list.SetItems(30, 6);
        list.Select(6);
        Assert.Equal(6, list.FirstVisible);
        list.Select(29);
        Assert.Equal(24, list.FirstVisible);
        list.Select(23);
        Assert.Equal(18, list.FirstVisible);
    }

    [Fact]
    public void PageTurnsDoNotOverlapAndStopAtAPartialLastPage()
    {
        var list = new ListPosition();
        list.SetItems(13, 5);
        Assert.False(list.HasPrevious);
        Assert.True(list.GoToPage(1));
        Assert.Equal(5, list.FirstVisible);
        Assert.Equal(5, list.VisibleCount);
        Assert.True(list.HasPrevious);
        Assert.True(list.HasNext);
        Assert.True(list.GoToPage(2));
        Assert.Equal(10, list.FirstVisible);
        Assert.Equal(3, list.VisibleCount);
        Assert.False(list.HasNext);
        Assert.False(list.GoToPage(3));
        Assert.Equal(10, list.Selected);
    }

    [Fact]
    public void DeletingTheOnlyItemOnTheLastPageReturnsToThePreviousPage()
    {
        var list = new ListPosition();
        list.SetItems(7, 6, 6);
        list.SetItems(6, 6);
        Assert.Equal(5, list.Selected);
        Assert.Equal(0, list.FirstVisible);
        Assert.False(list.HasNext);
        list.SetItems(0, 6);
        Assert.Equal(-1, list.Selected);
        Assert.Equal(0, list.VisibleCount);
        Assert.False(list.HasPrevious);
        Assert.False(list.HasNext);
        Assert.False(list.GoToPage(1));
    }

    [Fact]
    public void ResizingKeepsTheSelectedItemOnAnAlignedPage()
    {
        var list = new ListPosition();
        list.SetItems(30, 6, 15);
        list.SetItems(30, 4);
        Assert.Equal(15, list.Selected);
        Assert.Equal(12, list.FirstVisible);
        list.GoToPage(-1);
        Assert.Equal(0, list.Selected);
        list.GoToPage(100);
        Assert.Equal(28, list.FirstVisible);
        Assert.Equal(2, list.VisibleCount);
    }
}
