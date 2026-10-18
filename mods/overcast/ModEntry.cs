using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Overcast;

internal sealed class ModEntry : Mod
{
    internal static ModConfig Config { get; private set; } = new();
    internal static ICloudySkiesApi? WeatherApi { get; private set; }

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
            Monitor.Log($"Overcast could not patch native cloud shadows; vanilla behaviour is unchanged. {exception}", LogLevel.Error);
            return;
        }

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        WeatherApi = Helper.ModRegistry.GetApi<ICloudySkiesApi>("leclair.cloudyskies");
        var api = Helper.ModRegistry.GetApi<IConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null)
            return;

        api.Register(ModManifest, () => Config = new ModConfig(), () =>
        {
            NormalizeConfig();
            Helper.WriteConfig(Config);
        });

        AddBool(api, "enabled", () => Config.Enabled, value => Config.Enabled = value);
        AddBool(api, "spring", () => Config.EnableInSpring, value => Config.EnableInSpring = value);
        AddBool(api, "fall", () => Config.EnableInFall, value => Config.EnableInFall = value);
        AddBool(api, "winter", () => Config.EnableInWinter, value => Config.EnableInWinter = value);

        api.AddSectionTitle(ModManifest, () => T("config.appearance.title"));
        AddNumber(api, "speed", () => Config.SpeedMultiplier, value => Config.SpeedMultiplier = value, 0f, 2f);
        AddSize(api, "min-size", () => Config.MinSize, value => Config.MinSize = value);
        AddSize(api, "max-size", () => Config.MaxSize, value => Config.MaxSize = value);
    }

    private void NormalizeConfig()
    {
        if (Config.Normalize())
            Monitor.Log("Unsafe Overcast configuration values were clamped to supported ranges.", LogLevel.Warn);
    }

    private void AddBool(IConfigMenuApi api, string key, Func<bool> getValue, Action<bool> setValue)
    {
        api.AddBoolOption(ModManifest, getValue, setValue, () => T($"config.{key}.name"), () => T($"config.{key}.tooltip"), key);
    }

    private void AddNumber(IConfigMenuApi api, string key, Func<float> getValue, Action<float> setValue, float minimum, float maximum)
    {
        api.AddNumberOption(ModManifest, getValue, setValue, () => T($"config.{key}.name"), () => T($"config.{key}.tooltip"),
            minimum, maximum, 0.05f, null, key);
    }

    private void AddSize(IConfigMenuApi api, string key, Func<int> getValue, Action<int> setValue)
    {
        api.AddNumberOption(ModManifest, getValue, setValue, () => T($"config.{key}.name"), () => T($"config.{key}.tooltip"),
            ModConfig.MinimumSize, ModConfig.MaximumSize, 1, null, key);
    }

    private string T(string key) => Helper.Translation.Get(key);
}
