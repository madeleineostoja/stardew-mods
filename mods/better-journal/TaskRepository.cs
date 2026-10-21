using System.Text.Json;
using StardewModdingAPI;
using StardewValley;

namespace BetterJournal;

internal sealed class TaskRepository
{
    private readonly Farmer player;
    public TaskList? Tasks { get; }

    public TaskRepository(Farmer player, IMonitor monitor)
    {
        this.player = player;
        player.modData.TryGetValue(ModEntry.TasksKey, out var saved);
        try
        {
            Tasks = TaskList.Read(saved);
        }
        catch (JsonException exception)
        {
            monitor.Log($"Better Journal could not read {player.Name}'s tasks. The saved data is unchanged; task editing is disabled. {exception.Message}", LogLevel.Error);
        }
    }

    public void Save()
    {
        if (Tasks is not null)
            player.modData[ModEntry.TasksKey] = Tasks.Write();
    }

    public void FinishCompleted()
    {
        if (Tasks?.ClearCompleted() == true)
            Save();
    }
}
