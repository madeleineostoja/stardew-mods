namespace ConfigurableRumble;

internal sealed class ModConfig
{
    public bool FishingBites { get; set; } = true;
    public bool FishingCasts { get; set; } = true;
    public bool FishingReeling { get; set; }
    public bool ToolUse { get; set; } = true;
    public bool Combat { get; set; } = true;
    public bool Explosions { get; set; } = true;
    public bool HorseRiding { get; set; }
    public bool DoorsAndTransitions { get; set; } = true;

    public bool IsEnabled(RumbleEffect effect) => effect switch
    {
        RumbleEffect.FishingBites => FishingBites,
        RumbleEffect.FishingCasts => FishingCasts,
        RumbleEffect.FishingReeling => FishingReeling,
        RumbleEffect.ToolUse => ToolUse,
        RumbleEffect.Combat => Combat,
        RumbleEffect.Explosions => Explosions,
        RumbleEffect.HorseRiding => HorseRiding,
        RumbleEffect.DoorsAndTransitions => DoorsAndTransitions,
        _ => true
    };
}
