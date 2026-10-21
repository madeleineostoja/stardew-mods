namespace BetterJournal;

internal sealed class JournalTask
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public int? CompletedOnDay { get; set; }
}
