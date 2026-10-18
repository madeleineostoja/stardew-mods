using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley;

namespace Overslept;

[HarmonyPatch(typeof(Farmer), nameof(Farmer.passOutFromTired))]
internal static class PassOutPatch
{
    private static void Prefix(Farmer who) => ModEntry.OnPassOut(who);
}

[HarmonyPatch(typeof(Farmer), nameof(Farmer.dayupdate))]
internal static class EnergyPenaltyPatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.Select(code => new CodeInstruction(code)).ToList();
        var matches = 0;
        for (var index = 0; index < codes.Count; index++)
        {
            if (!codes[index].LoadsConstant(2400))
                continue;

            // Change only the late-bedtime gate, leaving exhaustion and other recovery rules intact.
            var labels = codes[index].ExtractLabels();
            codes.Insert(index++, new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels));
            codes.Insert(index++, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EnergyPenaltyPatch), nameof(GetPenaltyBedtime))));
            matches++;
        }

        if (matches != 1)
            throw new InvalidOperationException("Overnight energy recovery no longer matches the expected bedtime hook.");
        return codes;
    }

    private static int GetPenaltyBedtime(int bedtime, Farmer who)
    {
        return ModEntry.IsAffected(who) && ModEntry.Night!.DisableEnergyPenalty ? 2400 : bedtime;
    }
}

[HarmonyPatch]
internal static class RescuePenaltyPatch
{
    private static MethodBase TargetMethod()
    {
        return typeof(Farmer).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            .Single(method => method.Name.StartsWith("<performPassoutWarp>g__ContinuePassOut|", StringComparison.Ordinal));
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.Select(code => new CodeInstruction(code)).ToList();
        var safeCheck = AccessTools.Method(typeof(GameLocation), nameof(GameLocation.HasMapPropertyWithValue));
        var matches = 0;
        for (var index = 1; index < codes.Count; index++)
        {
            if (!codes[index].Calls(safeCheck) || !codes[index - 1].LoadsConstant("PassOutSafe"))
                continue;

            codes[index].opcode = OpCodes.Call;
            codes[index].operand = AccessTools.Method(typeof(RescuePenaltyPatch), nameof(IsSafeFromRescue));
            matches++;
        }

        if (matches != 1)
            throw new InvalidOperationException("Pass-out rescue no longer matches the expected safety hook.");
        return codes;
    }

    private static bool IsSafeFromRescue(GameLocation location, string property)
    {
        // Skip the native charge-and-mail branch, not the warp or next-day transition.
        return (ModEntry.IsAffected(Game1.player) && ModEntry.Night!.DisableMoneyLoss)
            || location.HasMapPropertyWithValue(property);
    }
}
