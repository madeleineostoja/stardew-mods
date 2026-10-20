using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace ConfigurableRumble;

internal sealed class ModEntry : Mod
{
    internal static ModConfig Config { get; private set; } = new();

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        var harmony = new Harmony(ModManifest.UniqueID);
        try
        {
            RumblePatches.Apply(harmony);
        }
        catch (Exception exception)
        {
            harmony.UnpatchAll(ModManifest.UniqueID);
            Monitor.Log($"Configurable Rumble could not patch the game's vibration calls; vanilla behaviour is unchanged. {exception}", LogLevel.Error);
            return;
        }

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var api = Helper.ModRegistry.GetApi<IConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null)
            return;

        api.Register(ModManifest, () => Config = new ModConfig(), () => Helper.WriteConfig(Config));
        AddOption(RumbleEffect.FishingBites, () => Config.FishingBites, value => Config.FishingBites = value);
        AddOption(RumbleEffect.FishingCasts, () => Config.FishingCasts, value => Config.FishingCasts = value);
        AddOption(RumbleEffect.FishingReeling, () => Config.FishingReeling, value => Config.FishingReeling = value);
        AddOption(RumbleEffect.ToolUse, () => Config.ToolUse, value => Config.ToolUse = value);
        AddOption(RumbleEffect.Combat, () => Config.Combat, value => Config.Combat = value);
        AddOption(RumbleEffect.Explosions, () => Config.Explosions, value => Config.Explosions = value);
        AddOption(RumbleEffect.HorseRiding, () => Config.HorseRiding, value => Config.HorseRiding = value);
        AddOption(RumbleEffect.DoorsAndTransitions, () => Config.DoorsAndTransitions, value => Config.DoorsAndTransitions = value);

        void AddOption(RumbleEffect effect, Func<bool> getValue, Action<bool> setValue)
        {
            api.AddBoolOption(ModManifest, getValue, setValue,
                () => Helper.Translation.Get($"config.{effect}.name"),
                () => Helper.Translation.Get($"config.{effect}.tooltip"),
                fieldId: effect.ToString());
        }
    }
}
