using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace BetterJournal;

internal sealed class ModEntry : Mod
{
    internal const string TasksKey = "heyitsmads.BetterJournal/Tasks";
    private ModConfig config = new();
    private readonly PerScreen<EventDefinitionCache> eventDefinitions = new(() => new EventDefinitionCache());

    internal IReadOnlyList<EventCandidate> FindEventCandidates() => new EventLookup(eventDefinitions.Value, Monitor).FindCandidates();

    internal IReadOnlyList<EventRow> FindEventRows() => new EventTitleProjection(Helper.Translation).Project(FindEventCandidates());

    public override void Entry(IModHelper helper)
    {
        config = helper.ReadConfig<ModConfig>();
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
        helper.Events.Content.LocaleChanged += OnLocaleChanged;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
    }

    private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (e.NamesWithoutLocale.Any(name => name.IsEquivalentTo("Data/Locations") || name.StartsWith("Data/Events/")))
            eventDefinitions.ResetAllScreens();
    }

    private void OnLocaleChanged(object? sender, LocaleChangedEventArgs e) => eventDefinitions.ResetAllScreens();

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e) => eventDefinitions.ResetAllScreens();

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var api = Helper.ModRegistry.GetApi<IConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null)
            return;

        api.Register(ModManifest, () => config = new ModConfig(), () => Helper.WriteConfig(config));
        api.AddBoolOption(ModManifest, () => config.ShowMonsterGoals, value => config.ShowMonsterGoals = value,
            () => Helper.Translation.Get("config.monster-goals.name"),
            () => Helper.Translation.Get("config.monster-goals.tooltip"));
        api.AddBoolOption(ModManifest, () => config.ShowEvents, value => config.ShowEvents = value,
            () => Helper.Translation.Get("config.events.name"),
            () => Helper.Translation.Get("config.events.tooltip"));
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        // Leave other mods' quest-menu subclasses and replacements in control.
        if (!Context.IsWorldReady || e.NewMenu?.GetType() != typeof(QuestLog)
            || !ReferenceEquals(Game1.activeClickableMenu, e.NewMenu)
            || e.NewMenu.GetChildMenu() is not null || e.NewMenu.GetParentMenu() is not null)
            return;

        var tasks = new TaskRepository(Game1.player, Monitor);
        tasks.FinishCompleted();
        Game1.activeClickableMenu = new JournalMenu((QuestLog)e.NewMenu, tasks, Helper.Translation,
            config.ShowMonsterGoals, config.ShowEvents ? FindEventRows : null);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Game1.activeClickableMenu is JournalMenu journal)
            journal.AdvanceCompletions(Game1.currentGameTime.ElapsedGameTime);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        new TaskRepository(Game1.player, Monitor).FinishCompleted();
    }
}
