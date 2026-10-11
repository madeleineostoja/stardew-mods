namespace Overcast.Models;

internal sealed class PopulationMaintenance
{
    private float scale;
    private float density;
    private bool includeSecondary;
    private bool cloudsMoved;

    public WorldBounds? Viewport { get; private set; }

    public bool IsNeeded(WorldBounds viewport, float globalScale, float populationDensity, bool secondary)
    {
        return Viewport != viewport || cloudsMoved || scale != globalScale
            || density != populationDensity || includeSecondary != secondary;
    }

    public void MarkMoved()
    {
        // Field membership can change before a cloud reaches the retirement boundary.
        cloudsMoved = true;
    }

    public void Record(WorldBounds viewport, float globalScale, float populationDensity, bool secondary)
    {
        Viewport = viewport;
        scale = globalScale;
        density = populationDensity;
        includeSecondary = secondary;
        cloudsMoved = false;
    }

    public void Clear()
    {
        Viewport = null;
        cloudsMoved = false;
    }
}
