namespace Overcast;

internal static class CloudRules
{
    private static readonly HashSet<string> SuppressedWeather = new(StringComparer.Ordinal)
    {
        "Kana.WeatherWonders_Drizzle",
        "Kana.WeatherWonders_Deluge",
        "Kana.WeatherWonders_AcidRain",
        "Kana.WeatherWonders_MuddyRain",
        "Kana.WeatherWonders_Mist",
        "Kana.WeatherWonders_Blizzard",
        "Kana.WeatherWonders_RainSnowMix",
        "Kana.WeatherWonders_DryLightning",
        "Kana.WeatherWonders_Hailstorm",
        "Kana.WeatherWonders_Sandstorm",
    };

    public static bool CanSpawn(ModConfig config, string season, string? weatherId, bool? spawnClouds = null)
    {
        if (season == "summer" && !ShouldTune(config, season))
            return true;

        return ShouldTune(config, season) && spawnClouds != false && (weatherId is null || !SuppressedWeather.Contains(weatherId));
    }

    public static bool ShouldTune(ModConfig config, string season)
    {
        return config.Enabled && season switch
        {
            "spring" => config.EnableInSpring,
            "fall" => config.EnableInFall,
            "summer" => true,
            "winter" => config.EnableInWinter,
            _ => false,
        };
    }

    public static float GetOpacity(ModConfig config, string season, string? weatherId)
    {
        if (!ShouldTune(config, season))
            return 1f;

        return weatherId == "Kana.WeatherWonders_Cloudy" ? 0.5f : 1f;
    }
}
