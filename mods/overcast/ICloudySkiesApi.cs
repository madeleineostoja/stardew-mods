namespace Overcast;

public interface ICloudySkiesApi
{
    // Output-only data supports a read-only projection; out parameters require the full mutable schema.
    IEnumerable<ICloudyWeather> GetAllCustomWeather();
}

public interface ICloudyWeather
{
    string Id { get; }
    bool? SpawnClouds { get; }
}
