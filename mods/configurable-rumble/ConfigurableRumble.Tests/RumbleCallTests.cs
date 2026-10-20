using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley;
using Xunit;

namespace ConfigurableRumble.Tests;

public sealed class RumbleCallTests
{
    public RumbleCallTests()
    {
        ModEntry.Config = new ModConfig();
        Rumble.Requests.Clear();
    }

    [Fact]
    public void DefaultsSilenceReelingAndHoofbeatsButKeepFishingAlertsAndStops()
    {
        var reeling = Call(nameof(Rumble.rumble), 0.1f, 1000f)
            .Append(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Rumble), nameof(Rumble.stopRumbling))))
            .Append(new CodeInstruction(OpCodes.Ret));
        var reel = Compile(reeling, RumbleEffect.FishingReeling, 1);
        var ride = Compile(Call(nameof(Rumble.rumble), 0.3f, 50f).Append(new CodeInstruction(OpCodes.Ret)),
            RumbleEffect.HorseRiding, 1);
        reel();
        ride();

        Compile(Call(nameof(Rumble.rumble), 0.75f, 250f).Append(new CodeInstruction(OpCodes.Ret)),
            RumbleEffect.FishingBites, 1)();
        Compile(Call(nameof(Rumble.rumble), 0.5f, 150f).Append(new CodeInstruction(OpCodes.Ret)),
            RumbleEffect.FishingCasts, 1)();

        Assert.Equal(new[] { ("Stop", 0f, 0f), ("Rumble", 0.75f, 250f), ("Rumble", 0.5f, 150f) }, Rumble.Requests);

        Rumble.Requests.Clear();
        ModEntry.Config.FishingReeling = true;
        ModEntry.Config.HorseRiding = true;
        reel();
        ride();

        Assert.Equal(new[] { ("Rumble", 0.1f, 1000f), ("Stop", 0f, 0f), ("Rumble", 0.3f, 50f) }, Rumble.Requests);
    }

    [Fact]
    public void EnabledEffectsKeepVanillaPowerDurationAndFade()
    {
        var codes = Call(nameof(Rumble.rumble), 0.75f, 150f)
            .Concat(Call(nameof(Rumble.rumbleAndFade), 0.75f, 1500f))
            .Append(new CodeInstruction(OpCodes.Ret));

        Compile(codes, RumbleEffect.Combat, 2)();

        Assert.Equal(new[] { ("Rumble", 0.75f, 150f), ("Fade", 0.75f, 1500f) }, Rumble.Requests);
    }

    [Fact]
    public void MutingCombatAlsoSuppressesTheKnockoutFade()
    {
        ModEntry.Config.Combat = false;
        var codes = Call(nameof(Rumble.rumble), 0.75f, 150f)
            .Concat(Call(nameof(Rumble.rumbleAndFade), 0.75f, 1500f))
            .Append(new CodeInstruction(OpCodes.Ret));

        Compile(codes, RumbleEffect.Combat, 2)();

        Assert.Empty(Rumble.Requests);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void ChangedVanillaCallCountsAreRejected(int count)
    {
        var codes = Enumerable.Range(0, count).SelectMany(_ => Call(nameof(Rumble.rumble), 0.1f, 50f));
        Assert.Throws<InvalidOperationException>(() => RumbleCallRewriter.Rewrite(codes, RumbleEffect.HorseRiding, 1));
    }

    [Fact]
    public void BranchingToACallStillAppliesItsToggle()
    {
        ModEntry.Config.ToolUse = false;
        var method = NewMethod();
        var label = method.GetILGenerator().DefineLabel();
        var codes = new[]
        {
            new CodeInstruction(OpCodes.Ldc_R4, 0.2f),
            new CodeInstruction(OpCodes.Ldc_R4, 100f),
            new CodeInstruction(OpCodes.Br, label),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Rumble), nameof(Rumble.rumble))).WithLabels(label),
            new CodeInstruction(OpCodes.Ret)
        };

        Compile(codes, RumbleEffect.ToolUse, 1, method)();

        Assert.Empty(Rumble.Requests);
    }

    private static IEnumerable<CodeInstruction> Call(string name, float power, float duration)
    {
        yield return new CodeInstruction(OpCodes.Ldc_R4, power);
        yield return new CodeInstruction(OpCodes.Ldc_R4, duration);
        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Rumble), name));
    }

    private static DynamicMethod NewMethod() => new("RumbleTest", typeof(void), Type.EmptyTypes, typeof(RumbleCallTests).Module, true);

    private static Action Compile(IEnumerable<CodeInstruction> codes, RumbleEffect effect, int expectedCalls, DynamicMethod? method = null)
    {
        method ??= NewMethod();
        var generator = method.GetILGenerator();
        foreach (var code in RumbleCallRewriter.Rewrite(codes, effect, expectedCalls))
        {
            foreach (var label in code.labels)
                generator.MarkLabel(label);

            switch (code.operand)
            {
                case null: generator.Emit(code.opcode); break;
                case int value: generator.Emit(code.opcode, value); break;
                case float value: generator.Emit(code.opcode, value); break;
                case MethodInfo called: generator.Emit(code.opcode, called); break;
                case Label label: generator.Emit(code.opcode, label); break;
                default: throw new InvalidOperationException($"Unexpected test operand: {code.operand}");
            }
        }
        return method.CreateDelegate<Action>();
    }
}
