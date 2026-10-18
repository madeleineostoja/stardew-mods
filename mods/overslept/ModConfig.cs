namespace Overslept;

internal sealed class ModConfig
{
    public int WakeUpTime { get; set; } = 1000;
    public bool DisableEnergyPenalty { get; set; } = true;
    public bool DisableMoneyLoss { get; set; } = true;

    public static bool IsValidTime(int time)
    {
        return time >= 600 && time <= 2550 && time % 100 < 60 && time % 10 == 0;
    }

    public bool Normalize()
    {
        if (IsValidTime(WakeUpTime))
            return false;

        WakeUpTime = 1000;
        return true;
    }
}
