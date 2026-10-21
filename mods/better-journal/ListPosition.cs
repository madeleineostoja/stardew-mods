namespace BetterJournal;

internal sealed class ListPosition
{
    private int count;
    private int capacity = 1;
    public int Selected { get; private set; } = -1;
    public int Page => Selected < 0 ? 0 : Selected / capacity;
    public int FirstVisible => Page * capacity;
    public int VisibleCount => Math.Min(capacity, count - FirstVisible);
    public bool HasPrevious => Page > 0;
    public bool HasNext => FirstVisible + VisibleCount < count;

    public void SetItems(int count, int capacity, int? selected = null)
    {
        this.count = Math.Max(0, count);
        this.capacity = Math.Max(1, capacity);
        Select(selected ?? Selected);
    }

    public void Select(int index) => Selected = count == 0 ? -1 : Math.Clamp(index, 0, count - 1);

    public bool GoToPage(int page)
    {
        int target = Math.Clamp(page, 0, Math.Max(0, (count - 1) / capacity));
        if (target == Page)
            return false;
        Selected = target * capacity;
        return true;
    }
}
