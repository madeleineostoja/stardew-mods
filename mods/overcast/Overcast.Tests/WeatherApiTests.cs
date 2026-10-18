using System.Reflection;
using System.Reflection.Emit;
using Nanoray.Pintail;
using Xunit;

namespace Overcast.Tests;

public sealed class WeatherApiTests
{
    [Fact]
    public void Readonly_weather_projection_preserves_identity_and_explicit_cloud_veto()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Overcast.WeatherProxyTests"), AssemblyBuilderAccess.Run);
        var manager = new ProxyManager<string>(assembly.DefineDynamicModule("WeatherProxyTests"));
        var api = manager.ObtainProxy<string, ICloudySkiesApi>(new NativeWeatherApi(), "weather.framework", "overcast");

        var weather = Assert.Single(api.GetAllCustomWeather());

        Assert.Equal("CustomWeather", weather.Id);
        Assert.Equal(false, weather.SpawnClouds);
    }

    public interface IMutableWeather
    {
        string Id { get; set; }
        bool? SpawnClouds { get; set; }
        string DisplayName { get; set; }
    }

    public sealed class NativeWeather : IMutableWeather
    {
        public string Id { get; set; } = "CustomWeather";
        public bool? SpawnClouds { get; set; } = false;
        public string DisplayName { get; set; } = "Weather with additional mutable metadata";
    }

    public sealed class NativeWeatherApi
    {
        public IEnumerable<IMutableWeather> GetAllCustomWeather()
        {
            yield return new NativeWeather();
        }
    }
}
