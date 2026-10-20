using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley;

namespace ConfigurableRumble;

internal static class RumbleCallRewriter
{
    private static readonly MethodInfo Rumble = AccessTools.Method(typeof(Rumble), nameof(StardewValley.Rumble.rumble),
        new[] { typeof(float), typeof(float) });
    private static readonly MethodInfo Fade = AccessTools.Method(typeof(Rumble), nameof(StardewValley.Rumble.rumbleAndFade));
    private static readonly MethodInfo PlayMethod = AccessTools.Method(typeof(RumbleCallRewriter), nameof(Play));
    private static readonly MethodInfo FadeMethod = AccessTools.Method(typeof(RumbleCallRewriter), nameof(PlayAndFade));

    internal static IEnumerable<CodeInstruction> Rewrite(
        IEnumerable<CodeInstruction> instructions, RumbleEffect effect, int expectedCalls)
    {
        var codes = instructions.Select(code => new CodeInstruction(code)).ToList();
        var calls = codes.Count(code => code.Calls(Rumble) || code.Calls(Fade));
        if (calls != expectedCalls)
            throw new InvalidOperationException($"Expected {expectedCalls} vanilla vibration calls for {effect}, found {calls}.");

        for (var index = 0; index < codes.Count; index++)
        {
            var code = codes[index];
            var fading = code.Calls(Fade);
            if (!fading && !code.Calls(Rumble))
                continue;

            var argument = new CodeInstruction(OpCodes.Ldc_I4, (int)effect).WithLabels(code.ExtractLabels());
            // Branches and exception entry must run the added argument; exception exit stays after the call.
            argument.blocks.AddRange(code.blocks.Where(block => block.blockType != ExceptionBlockType.EndExceptionBlock));
            code.blocks.RemoveAll(block => block.blockType != ExceptionBlockType.EndExceptionBlock);
            codes.Insert(index++, argument);
            code.operand = fading ? FadeMethod : PlayMethod;
        }
        return codes;
    }

    private static void Play(float power, float milliseconds, RumbleEffect effect)
    {
        if (ModEntry.Config.IsEnabled(effect))
            StardewValley.Rumble.rumble(power, milliseconds);
    }

    private static void PlayAndFade(float power, float milliseconds, RumbleEffect effect)
    {
        if (ModEntry.Config.IsEnabled(effect))
            StardewValley.Rumble.rumbleAndFade(power, milliseconds);
    }
}
