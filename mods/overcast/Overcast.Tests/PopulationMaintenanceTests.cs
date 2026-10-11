using Overcast.Models;
using Xunit;

namespace Overcast.Tests;

public sealed class PopulationMaintenanceTests
{
    private static readonly WorldBounds Viewport = new(100, 200, 1280, 720);

    [Fact]
    public void Initial_and_cleared_populations_require_maintenance_but_unchanged_static_populations_do_not()
    {
        var maintenance = new PopulationMaintenance();

        Assert.True(maintenance.IsNeeded(Viewport, 1f, 1f, true));
        maintenance.Record(Viewport, 1f, 1f, true);
        Assert.False(maintenance.IsNeeded(Viewport, 1f, 1f, true));

        maintenance.Clear();
        Assert.True(maintenance.IsNeeded(Viewport, 1f, 1f, true));
    }

    [Fact]
    public void Movement_requires_maintenance_even_without_a_viewport_or_settings_change()
    {
        var maintenance = new PopulationMaintenance();
        maintenance.Record(Viewport, 1f, 1f, true);

        maintenance.MarkMoved();
        Assert.True(maintenance.IsNeeded(Viewport, 1f, 1f, true));
        Assert.True(maintenance.IsNeeded(Viewport, 1f, 1f, true));

        maintenance.Record(Viewport, 1f, 1f, true);
        Assert.False(maintenance.IsNeeded(Viewport, 1f, 1f, true));
    }

    [Fact]
    public void Checking_small_camera_moves_does_not_discard_the_last_maintained_viewport()
    {
        var maintenance = new PopulationMaintenance();
        maintenance.Record(Viewport, 1f, 1f, true);

        Assert.True(maintenance.IsNeeded(Viewport with { X = 101 }, 1f, 1f, true));
        var next = Viewport with { X = 102 };
        Assert.True(maintenance.IsNeeded(next, 1f, 1f, true));
        Assert.Equal(Viewport, maintenance.Viewport);

        maintenance.Record(next, 1f, 1f, true);
        Assert.Equal(next, maintenance.Viewport);
        Assert.False(maintenance.IsNeeded(next, 1f, 1f, true));
    }

    [Fact]
    public void Resize_scale_density_and_layer_changes_each_require_maintenance()
    {
        var maintenance = new PopulationMaintenance();
        maintenance.Record(Viewport, 1f, 1f, true);

        Assert.True(maintenance.IsNeeded(Viewport with { Width = 1920 }, 1f, 1f, true));
        Assert.True(maintenance.IsNeeded(Viewport, 1.5f, 1f, true));
        Assert.True(maintenance.IsNeeded(Viewport, 1f, 1.3f, true));
        Assert.True(maintenance.IsNeeded(Viewport, 1f, 1f, false));
    }
}
