using Overcast.Models;
using Xunit;

namespace Overcast.Tests;

public sealed class WorldBoundsTests
{
    [Fact]
    public void Exposed_band_uses_accumulated_camera_movement()
    {
        var previous = new WorldBounds(100, 200, 1280, 720);
        var current = previous with { X = 106, Y = 203 };

        Assert.Equal(new WorldBounds(1380, 203, 6, 720), current.GetExposedBand(previous));
    }

    [Fact]
    public void Growing_viewport_exposes_its_new_edge_without_camera_movement()
    {
        var previous = new WorldBounds(100, 200, 1280, 720);
        var current = previous with { Width = 1920 };

        Assert.Equal(new WorldBounds(1380, 200, 640, 720), current.GetExposedBand(previous));
    }

    [Fact]
    public void Moving_and_shrinking_viewport_does_not_create_a_negative_exposed_band()
    {
        var previous = new WorldBounds(100, 200, 1280, 720);
        var current = new WorldBounds(110, 205, 1200, 700);

        Assert.Null(current.GetExposedBand(previous));
    }
}
