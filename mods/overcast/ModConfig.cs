namespace Overcast;

internal sealed class ModConfig
{
    public const int MinimumSize = 2;
    public const int MaximumSize = 8;

    public bool Enabled { get; set; } = true;
    public bool EnableInSpring { get; set; } = true;
    public bool EnableInFall { get; set; } = true;
    public bool EnableInWinter { get; set; }
    public float SpeedMultiplier { get; set; } = 1f;
    public int MinSize { get; set; } = 4;
    public int MaxSize { get; set; } = 6;

    public bool Normalize()
    {
        var speed = float.IsFinite(SpeedMultiplier) ? Math.Clamp(SpeedMultiplier, 0f, 2f) : 1f;
        var minimum = Math.Clamp(MinSize, MinimumSize, MaximumSize);
        var maximum = Math.Clamp(MaxSize, minimum, MaximumSize);
        var changed = speed != SpeedMultiplier || minimum != MinSize || maximum != MaxSize;
        SpeedMultiplier = speed;
        MinSize = minimum;
        MaxSize = maximum;
        return changed;
    }
}
