using Xunit;

namespace Overslept.Tests;

public sealed class PassOutNightTests
{
    [Theory]
    [InlineData(true, true, 2600)]
    [InlineData(false, false, 2600)]
    [InlineData(false, true, 2530)]
    public void OtherPlayersAndEarlierCollapsesKeepVanillaBehaviour(bool multiplayer, bool localPlayer, int time)
    {
        Assert.Null(PassOutNight.Create(multiplayer, localPlayer, time, 10, false, new ModConfig()));
    }

    [Fact]
    public void CollapseSnapshotsIndependentPenaltySettingsAndWakeUpTime()
    {
        var config = new ModConfig { WakeUpTime = 1130, DisableEnergyPenalty = false, DisableMoneyLoss = true };
        var night = PassOutNight.Create(false, true, 2600, 27, false, config)!;
        config.WakeUpTime = 900;
        config.DisableMoneyLoss = false;

        Assert.Equal(1130, PassOutNight.ReadWakeUp(night.SaveWakeUp(), 28));
        Assert.False(night.DisableEnergyPenalty);
        Assert.True(night.DisableMoneyLoss);
    }

    [Fact]
    public void LevelUpKeepsSixAmWithoutRemovingPenaltyProtection()
    {
        var night = PassOutNight.Create(false, true, 2600, 10, true, new ModConfig())!;
        Assert.Equal(600, PassOutNight.ReadWakeUp(night.SaveWakeUp(), 11));
        Assert.True(night.DisableEnergyPenalty);
        Assert.True(night.DisableMoneyLoss);
    }

    [Fact]
    public void ReloadUsesTheSameMorningButDoesNotAffectLaterDays()
    {
        var saved = PassOutNight.Create(false, true, 2600, 10, false, new ModConfig())!.SaveWakeUp();
        Assert.Equal(1000, PassOutNight.ReadWakeUp(saved, 11));
        Assert.Equal(600, PassOutNight.ReadWakeUp(saved, 12));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("11:1260")]
    [InlineData("11:2600")]
    [InlineData("garbage")]
    public void InvalidSavedWakeUpDoesNotAdvanceTime(string? saved)
    {
        Assert.Equal(600, PassOutNight.ReadWakeUp(saved, 11));
    }

    [Theory]
    [InlineData(1260)]
    [InlineData(945)]
    [InlineData(2600)]
    public void InvalidConfigurationFallsBackToTenAm(int time)
    {
        var config = new ModConfig { WakeUpTime = time };
        Assert.True(config.Normalize());
        Assert.Equal(1000, config.WakeUpTime);
    }

    [Fact]
    public void ValidLateWakeUpIsNotClampedToAnEarlierHour()
    {
        var config = new ModConfig { WakeUpTime = 2530 };
        Assert.False(config.Normalize());
        Assert.Equal(2530, config.WakeUpTime);
    }
}
