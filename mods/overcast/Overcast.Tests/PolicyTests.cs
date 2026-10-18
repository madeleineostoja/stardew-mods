using Xunit;

namespace Overcast.Tests;

public sealed class PolicyTests
{
    [Fact]
    public void Summer_always_uses_settings_unless_the_mod_is_disabled()
    {
        var config = new ModConfig { SpeedMultiplier = 0f, MinSize = 2, MaxSize = 8 };
        const string storm = "Kana.WeatherWonders_DryLightning";

        Assert.True(CloudRules.CanSpawn(config, "summer", "Sun"));
        Assert.False(CloudRules.CanSpawn(config, "summer", storm));
        Assert.True(CloudRules.ShouldTune(config, "summer"));
        Assert.Equal(0.5f, CloudRules.GetOpacity(config, "summer", "Kana.WeatherWonders_Cloudy"));

        config.Enabled = false;
        Assert.True(CloudRules.CanSpawn(config, "summer", storm));
        Assert.True(CloudRules.CanSpawn(config, "summer", "Sun", spawnClouds: false));
        Assert.False(CloudRules.ShouldTune(config, "summer"));
        Assert.Equal(1f, CloudRules.GetOpacity(config, "summer", "Sun"));
    }

    [Fact]
    public void Extension_respects_season_switches_with_winter_opt_in()
    {
        var config = new ModConfig();

        Assert.True(CloudRules.CanSpawn(config, "spring", "Sun"));
        Assert.True(CloudRules.CanSpawn(config, "fall", "Sun"));
        Assert.False(CloudRules.CanSpawn(config, "winter", "Sun"));

        config.EnableInSpring = false;
        Assert.False(CloudRules.CanSpawn(config, "spring", "Sun"));
        Assert.True(CloudRules.CanSpawn(config, "fall", "Sun"));

        config.EnableInWinter = true;
        Assert.True(CloudRules.CanSpawn(config, "winter", "Sun"));
        Assert.Equal(0.5f, CloudRules.GetOpacity(config, "winter", "Kana.WeatherWonders_Cloudy"));
        Assert.False(CloudRules.CanSpawn(config, "winter", "OtherMod.CustomWeather", spawnClouds: false));

        config.Enabled = false;
        Assert.False(CloudRules.CanSpawn(config, "fall", "Sun"));
        Assert.False(CloudRules.CanSpawn(config, "winter", "Sun"));
        Assert.Equal(1f, CloudRules.GetOpacity(config, "fall", null));
    }

    [Fact]
    public void Weather_wonders_obscuring_weather_is_excluded_without_blocking_unknown_weather()
    {
        var config = new ModConfig();

        Assert.False(CloudRules.CanSpawn(config, "spring", "Kana.WeatherWonders_Mist"));
        Assert.False(CloudRules.CanSpawn(config, "fall", "Kana.WeatherWonders_DryLightning"));
        Assert.False(CloudRules.CanSpawn(config, "spring", "Kana.WeatherWonders_Heatwave", spawnClouds: false));
        Assert.True(CloudRules.CanSpawn(config, "fall", "OtherMod.CustomWeather"));
        Assert.True(CloudRules.CanSpawn(config, "spring", null));
        Assert.False(CloudRules.CanSpawn(config, "fall", "OtherMod.CustomWeather", spawnClouds: false));
        Assert.True(CloudRules.CanSpawn(config, "fall", "OtherMod.CustomWeather", spawnClouds: true));
        Assert.False(CloudRules.CanSpawn(config, "summer", "OtherMod.CustomWeather", spawnClouds: false));
    }

    [Fact]
    public void Cloudy_weather_softens_shadows_in_every_enabled_season()
    {
        var config = new ModConfig();

        Assert.Equal(1f, CloudRules.GetOpacity(config, "spring", "Sun"));
        Assert.Equal(0.5f, CloudRules.GetOpacity(config, "spring", "Kana.WeatherWonders_Cloudy"));
        Assert.Equal(0.5f, CloudRules.GetOpacity(config, "summer", "Kana.WeatherWonders_Cloudy"));
        Assert.Equal(1f, CloudRules.GetOpacity(config, "winter", "Kana.WeatherWonders_Cloudy"));
        Assert.False(CloudRules.CanSpawn(config, "summer", "Kana.WeatherWonders_Sandstorm"));
    }

    [Fact]
    public void Normalize_rejects_unsafe_values_and_repairs_an_inverted_size_range()
    {
        var config = new ModConfig
        {
            SpeedMultiplier = float.PositiveInfinity,
            MinSize = 99,
            MaxSize = -4,
        };

        Assert.True(config.Normalize());
        Assert.Equal(1f, config.SpeedMultiplier);
        Assert.Equal(8, config.MinSize);
        Assert.Equal(8, config.MaxSize);
        Assert.False(config.Normalize());

        config.SpeedMultiplier = 99f;
        Assert.True(config.Normalize());
        Assert.Equal(2f, config.SpeedMultiplier);
    }
}
