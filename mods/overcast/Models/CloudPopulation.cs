using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Overcast.Models;

internal sealed class CloudPopulation
{
    // This exceeds the largest mask at the maximum supported scale, so new edge clouds stay offscreen.
    private const float Padding = 2800f;
    private const float AssetResolutionScale = 0.75f;
    private const float BaseScale = 0.85f / AssetResolutionScale;
    private static readonly Vector2 BaseWind = new Vector2(3f, 1f) * 3.3f;

    private readonly List<Texture2D> textures;
    private readonly List<Cloud> clouds = new();
    private Random random = new(0);
    private readonly PopulationMaintenance maintenance = new();

    public CloudPopulation(IReadOnlyList<Texture2D> textures)
    {
        this.textures = textures.ToList();
    }

    public int Count => clouds.Count;

    public Cloud this[int index] => clouds[index];

    public void Reset(int seed, WorldBounds viewport, float globalScale, float density, bool includeSecondary)
    {
        random = new Random(seed);
        clouds.Clear();
        Populate(GetSimulationRegion(viewport), globalScale, density, includeSecondary, true, null, viewport);
        maintenance.Record(viewport, globalScale, density, includeSecondary);
    }

    public bool NeedsReset(WorldBounds viewport)
    {
        if (maintenance.Viewport is not { } previous)
            return true;

        var dx = Math.Abs(viewport.X - previous.X);
        var dy = Math.Abs(viewport.Y - previous.Y);
        return dx > Padding * 0.65f || dy > Padding * 0.65f;
    }

    public bool NeedsMaintenance(WorldBounds viewport, float globalScale, float density, bool includeSecondary)
    {
        return maintenance.IsNeeded(viewport, globalScale, density, includeSecondary);
    }

    public void Maintain(WorldBounds viewport, float globalScale, float density, bool includeSecondary)
    {
        var field = GetSimulationRegion(viewport);
        var retirementArea = field.Inflate(Padding * 0.75f);
        for (var index = clouds.Count - 1; index >= 0; index--)
        {
            var cloud = clouds[index];
            if ((!includeSecondary && cloud.IsSecondary) || !cloud.GetBounds(globalScale).Intersects(retirementArea))
                clouds.RemoveAt(index);
        }

        Populate(field, globalScale, density, includeSecondary, false, GetExposedBand(viewport), null);
        maintenance.Record(viewport, globalScale, density, includeSecondary);
    }

    public void Advance(float elapsedSeconds, float speed)
    {
        if (elapsedSeconds <= 0f || speed == 0f || clouds.Count == 0)
            return;

        foreach (var cloud in clouds)
            cloud.Move(elapsedSeconds, speed);
        maintenance.MarkMoved();
    }

    public void Clear()
    {
        clouds.Clear();
        maintenance.Clear();
    }

    private void Populate(WorldBounds field, float globalScale, float density, bool includeSecondary, bool initial, WorldBounds? exposedBand, WorldBounds? visibleArea)
    {
        if (textures.Count == 0)
            return;

        var primaryTarget = PopulationRules.GetTarget(field, globalScale, density, false);
        var secondaryTarget = includeSecondary ? PopulationRules.GetTarget(field, globalScale, density, true) : 0;
        var primaryCount = 0;
        var secondaryCount = 0;
        var primaryTotal = 0;
        var secondaryTotal = 0;
        foreach (var cloud in clouds)
        {
            var inField = cloud.GetBounds(globalScale).Intersects(field);
            if (cloud.IsSecondary)
            {
                secondaryTotal++;
                if (inField)
                    secondaryCount++;
            }
            else
            {
                primaryTotal++;
                if (inField)
                    primaryCount++;
            }
        }

        while (primaryCount < primaryTarget && primaryTotal < PopulationRules.PrimaryCap)
        {
            clouds.Add(CreateCloud(field, globalScale, false, initial, exposedBand, primaryCount == 0 ? visibleArea : null));
            primaryCount++;
            primaryTotal++;
        }

        while (secondaryCount < secondaryTarget && secondaryTotal < PopulationRules.SecondaryCap)
        {
            clouds.Add(CreateCloud(field, globalScale, true, initial, exposedBand, null));
            secondaryCount++;
            secondaryTotal++;
        }
    }

    private Cloud CreateCloud(WorldBounds field, float globalScale, bool secondary, bool initial, WorldBounds? exposedBand, WorldBounds? visibleArea)
    {
        var variation = BaseScale * (0.8f + (float)random.NextDouble() * 0.45f);
        if (secondary)
            variation *= 0.9f;

        var texture = textures[random.Next(textures.Count)];
        var width = texture.Width * variation * globalScale;
        var height = texture.Height * variation * globalScale;
        Vector2 position;
        if (visibleArea is { } visible)
        {
            position = FindSpacedPosition(visible, width, height, globalScale, secondary);
        }
        else if (initial)
        {
            position = FindSpacedPosition(field, width, height, globalScale, secondary);
        }
        else if (exposedBand is { } band)
        {
            position = FindSpacedPosition(band, width, height, globalScale, secondary);
        }
        else
        {
            // Steady-state replacements start beyond the padded upwind edge.
            var crossAxis = (float)random.NextDouble();
            if (Math.Abs(BaseWind.X) >= Math.Abs(BaseWind.Y))
            {
                var x = BaseWind.X >= 0 ? field.X - width : field.Right;
                position = new Vector2(x, field.Y + crossAxis * Math.Max(1f, field.Height - height));
            }
            else
            {
                var y = BaseWind.Y >= 0 ? field.Y - height : field.Bottom;
                position = new Vector2(field.X + crossAxis * Math.Max(1f, field.Width - width), y);
            }
        }

        var speedVariation = 0.85f + (float)random.NextDouble() * 0.3f;
        var layerVelocity = secondary ? 1.18f : 1f;
        return new Cloud(texture, position, variation, random.Next(3) == 0, BaseWind * speedVariation * layerVelocity, secondary);
    }

    private Vector2 FindSpacedPosition(WorldBounds area, float width, float height, float globalScale, bool secondary)
    {
        var bestPosition = GetRandomPosition(area, width, height);
        var fewestOverlaps = int.MaxValue;
        var allowedOverlaps = secondary ? 1 : 0;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var position = attempt == 0 ? bestPosition : GetRandomPosition(area, width, height);
            var candidate = new WorldBounds(position.X, position.Y, width, height);
            var overlaps = 0;
            foreach (var cloud in clouds)
            {
                if (!secondary && cloud.IsSecondary)
                    continue;
                if (candidate.Intersects(cloud.GetBounds(globalScale)))
                    overlaps++;
            }
            if (overlaps <= allowedOverlaps)
                return position;
            if (overlaps < fewestOverlaps)
            {
                bestPosition = position;
                fewestOverlaps = overlaps;
            }
        }
        return bestPosition;
    }

    private Vector2 GetRandomPosition(WorldBounds area, float width, float height)
    {
        return new Vector2(
            area.X + (float)random.NextDouble() * Math.Max(1f, area.Width - width),
            area.Y + (float)random.NextDouble() * Math.Max(1f, area.Height - height));
    }

    private WorldBounds? GetExposedBand(WorldBounds viewport)
    {
        if (maintenance.Viewport is not { } lastViewport)
            return null;

        var field = GetSimulationRegion(viewport);
        var previous = GetSimulationRegion(lastViewport);
        return field.GetExposedBand(previous);
    }

    private static WorldBounds GetSimulationRegion(WorldBounds viewport)
    {
        return viewport.Inflate(Padding);
    }
}
