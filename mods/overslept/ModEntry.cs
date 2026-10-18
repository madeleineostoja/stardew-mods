using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Overslept;

internal sealed class ModEntry : Mod
{
    private const string WakeUpKey = "heyitsmads.Overslept/WakeUp";
    internal static ModConfig Config { get; private set; } = new();
    internal static PassOutNight? Night { get; private set; }
    private static Farmer? collapsedFarmer;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        NormalizeConfig();

        var harmony = new Harmony(ModManifest.UniqueID);
        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }
        catch (Exception exception)
        {
            harmony.UnpatchAll(ModManifest.UniqueID);
            Monitor.Log($"Overslept could not patch overnight penalties; vanilla behaviour is unchanged. {exception}", LogLevel.Error);
            return;
        }

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => ClearNight();
    }

    internal static void OnPassOut(Farmer who)
    {
        var night = PassOutNight.Create(Context.IsMultiplayer, who.IsLocalPlayer, Game1.timeOfDay,
            Game1.Date.TotalDays, who.newLevels.Count > 0, Config);
        if (!who.IsLocalPlayer)
            return;

        Night = night;
        collapsedFarmer = night is null ? null : who;
        if (night is not null)
            who.modData[WakeUpKey] = night.SaveWakeUp();
    }

    internal static bool IsAffected(Farmer who)
    {
        return !Context.IsMultiplayer && ReferenceEquals(who, collapsedFarmer) && Night is not null;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        ClearNight();
        if (Context.IsMultiplayer)
            return;

        Game1.player.modData.TryGetValue(WakeUpKey, out var saved);
        var target = PassOutNight.ReadWakeUp(saved, Game1.Date.TotalDays);
        if (Game1.timeOfDay >= target)
            return;

        try
        {
            MorningCatchUp.AdvanceTo(target);
            Helper.Events.GameLoop.UpdateTicked += OnWakeUp;
        }
        catch (Exception exception)
        {
            Monitor.Log($"Overslept could not finish morning catch-up; time remains at {Game1.timeOfDay}. {exception}", LogLevel.Error);
        }
    }

    private void OnWakeUp(object? sender, UpdateTickedEventArgs e)
    {
        // Wait for the morning fade and any menus so the emote is visible.
        if (!Context.CanPlayerMove || Game1.fadeToBlackAlpha > 0f)
            return;

        Helper.Events.GameLoop.UpdateTicked -= OnWakeUp;
        Game1.player.doEmote(Character.angryEmote);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        Game1.player.modData.TryGetValue(WakeUpKey, out var saved);
        if (Context.IsMultiplayer || PassOutNight.ReadWakeUp(saved, Game1.Date.TotalDays) == 600)
            Game1.player.modData.Remove(WakeUpKey);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMultiplayer)
            Monitor.Log("Overslept is single-player only. Multiplayer wakeup times and penalties are unchanged.", LogLevel.Info);
    }

    private void ClearNight()
    {
        Helper.Events.GameLoop.UpdateTicked -= OnWakeUp;
        Night = null;
        collapsedFarmer = null;
    }

    private void NormalizeConfig()
    {
        if (Config.Normalize())
            Monitor.Log("Invalid WakeUpTime; using 1000 (10am). Use HHMM in ten-minute increments from 0600 to 2550.", LogLevel.Warn);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var api = Helper.ModRegistry.GetApi<IConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null)
            return;

        api.Register(ModManifest, () => Config = new ModConfig(), () =>
        {
            NormalizeConfig();
            Helper.WriteConfig(Config);
        });
        // Use minutes in the slider so crossing an hour cannot produce an invalid HHMM value.
        api.AddNumberOption(ModManifest,
            () => Config.WakeUpTime / 100 * 60 + Config.WakeUpTime % 100,
            minutes => Config.WakeUpTime = minutes / 60 * 100 + minutes % 60,
            () => T("config.wake-up-time.name"), () => T("config.wake-up-time.tooltip"),
            360, 1550, 10, minutes => Game1.getTimeOfDayString(minutes / 60 * 100 + minutes % 60));
        api.AddBoolOption(ModManifest, () => Config.DisableEnergyPenalty, value => Config.DisableEnergyPenalty = value,
            () => T("config.disable-energy-penalty.name"), () => T("config.disable-energy-penalty.tooltip"));
        api.AddBoolOption(ModManifest, () => Config.DisableMoneyLoss, value => Config.DisableMoneyLoss = value,
            () => T("config.disable-money-loss.name"), () => T("config.disable-money-loss.tooltip"));
    }

    private string T(string key) => Helper.Translation.Get(key);
}
