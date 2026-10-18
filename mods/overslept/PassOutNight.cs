using System.Globalization;

namespace Overslept;

internal sealed record PassOutNight(int WakeUpDay, int WakeUpTime, bool DisableEnergyPenalty, bool DisableMoneyLoss)
{
    public static PassOutNight? Create(bool multiplayer, bool localPlayer, int time, int day, bool levelledUp, ModConfig config)
    {
        if (multiplayer || !localPlayer || time < 2600)
            return null;

        return new PassOutNight(day + 1, levelledUp ? 600 : config.WakeUpTime,
            config.DisableEnergyPenalty, config.DisableMoneyLoss);
    }

    public string SaveWakeUp()
    {
        return FormattableString.Invariant($"{WakeUpDay}:{WakeUpTime}");
    }

    public static int ReadWakeUp(string? saved, int day)
    {
        var parts = saved?.Split(':');
        if (parts?.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var savedDay)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var time)
            || savedDay != day || !ModConfig.IsValidTime(time))
            return 600;

        return time;
    }
}
