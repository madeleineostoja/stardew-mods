using System.Text.Json;

namespace BetterJournal;

internal sealed class TaskList
{
    public const int TextLimit = 160;
    private readonly List<JournalTask> entries;
    private readonly Dictionary<string, TimeSpan> completionDelays = new();
    private static readonly TimeSpan CompletionPause = TimeSpan.FromMilliseconds(650);

    public IReadOnlyList<JournalTask> Entries => entries;

    private TaskList(List<JournalTask> entries) => this.entries = entries;

    public static TaskList Read(string? json)
    {
        if (json is null)
            return new TaskList(new List<JournalTask>());

        var entries = JsonSerializer.Deserialize<List<JournalTask>>(json)
            ?? throw new JsonException("The task list is null.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var task in entries)
        {
            if (task is null || string.IsNullOrWhiteSpace(task.Id) || !ids.Add(task.Id)
                || !IsValidText(task.Text) || task.CompletedOnDay < 0)
                throw new JsonException("The task list contains an invalid entry.");
        }
        return new TaskList(entries);
    }

    public string Write() => JsonSerializer.Serialize(entries);

    public JournalTask Add(string text)
    {
        var task = new JournalTask { Id = Guid.NewGuid().ToString("N"), Text = ValidateText(text) };
        entries.Add(task);
        return task;
    }

    public bool Rename(string id, string text)
    {
        var task = entries.Find(entry => entry.Id == id);
        if (task is null)
            return false;
        task.Text = ValidateText(text);
        return true;
    }

    public bool Toggle(string id, int day)
    {
        var task = entries.Find(entry => entry.Id == id);
        if (task is null)
            return false;
        task.CompletedOnDay = task.CompletedOnDay.HasValue ? null : day;
        if (task.CompletedOnDay.HasValue)
            completionDelays[id] = CompletionPause;
        else
            completionDelays.Remove(id);
        return true;
    }

    public bool Remove(string id)
    {
        completionDelays.Remove(id);
        return entries.RemoveAll(entry => entry.Id == id) > 0;
    }

    public bool AdvanceCompletions(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
            return false;
        bool changed = false;
        foreach (string id in completionDelays.Keys.ToArray())
        {
            var remaining = completionDelays[id] - elapsed;
            if (remaining <= TimeSpan.Zero)
                changed |= Remove(id);
            else
                completionDelays[id] = remaining;
        }
        return changed;
    }

    public bool ClearCompleted()
    {
        completionDelays.Clear();
        return entries.RemoveAll(entry => entry.CompletedOnDay.HasValue) > 0;
    }

    private static bool IsValidText(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text.Length <= TextLimit && !text.Contains('\n') && !text.Contains('\r');

    private static string ValidateText(string text)
    {
        text = text.Trim();
        if (!IsValidText(text))
            throw new ArgumentException("Tasks need a single non-empty line of at most 160 characters.", nameof(text));
        return text;
    }
}
