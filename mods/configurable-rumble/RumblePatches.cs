using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Tools;

namespace ConfigurableRumble;

internal static class RumblePatches
{
    private static readonly Dictionary<MethodBase, (RumbleEffect Effect, int Calls)> Targets = new();

    internal static void Apply(Harmony harmony)
    {
        foreach (var target in GetTargets())
        {
            Targets.Add(target.Method, (target.Effect, target.Calls));
            harmony.Patch(target.Method, transpiler: new HarmonyMethod(typeof(RumblePatches), nameof(Transpiler)));
        }
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var target = Targets[__originalMethod];
        return RumbleCallRewriter.Rewrite(instructions, target.Effect, target.Calls);
    }

    internal static IEnumerable<(MethodInfo Method, RumbleEffect Effect, int Calls)> GetTargets()
    {
        yield return Target(typeof(Farmer), nameof(Farmer.takeDamage), RumbleEffect.Combat, 2,
            typeof(int), typeof(bool), typeof(Monster));
        yield return Target(typeof(Farmer), nameof(Farmer.Update), RumbleEffect.Combat, 1,
            typeof(GameTime), typeof(GameLocation));
        yield return Target(typeof(GameLocation), nameof(GameLocation.damageMonster), RumbleEffect.Combat, 1,
            typeof(Rectangle), typeof(int), typeof(int), typeof(bool), typeof(float), typeof(int),
            typeof(float), typeof(float), typeof(bool), typeof(Farmer), typeof(bool));
        yield return Target(typeof(GameLocation), nameof(GameLocation.performAction), RumbleEffect.DoorsAndTransitions, 3,
            typeof(string[]), typeof(Farmer), typeof(xTile.Dimensions.Location));
        yield return Target(typeof(GameLocation), nameof(GameLocation.lockedDoorWarp), RumbleEffect.DoorsAndTransitions, 1,
            typeof(Point), typeof(string), typeof(int), typeof(int), typeof(string), typeof(int));
        yield return Target(typeof(GameLocation), nameof(GameLocation.answerDialogueAction), RumbleEffect.DoorsAndTransitions, 2,
            typeof(string), typeof(string[]));
        yield return Target(typeof(GameLocation), "performRumbleAndFade", RumbleEffect.Explosions, 1, typeof(int));
        yield return Target(typeof(Tool), nameof(Tool.DoFunction), RumbleEffect.ToolUse, 1,
            typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer));
        yield return Target(typeof(FishingRod), nameof(FishingRod.tickUpdate), RumbleEffect.FishingBites, 2,
            typeof(GameTime), typeof(Farmer));
        yield return Target(typeof(FishingRod), "doStartCasting", RumbleEffect.FishingCasts, 1);
        yield return Target(typeof(BobberBar), nameof(BobberBar.update), RumbleEffect.FishingReeling, 1, typeof(GameTime));
        yield return Target(typeof(MovieTheater), "OnStartMovieEvent", RumbleEffect.DoorsAndTransitions, 1, typeof(StartMovieEvent));
        yield return Target(typeof(Town), nameof(Town.checkAction), RumbleEffect.DoorsAndTransitions, 1,
            typeof(xTile.Dimensions.Location), typeof(xTile.Dimensions.Rectangle), typeof(Farmer));
        yield return Target(typeof(Horse), nameof(Horse.PerformDefaultHorseFootstep), RumbleEffect.HorseRiding, 3, typeof(string));
    }

    private static (MethodInfo Method, RumbleEffect Effect, int Calls) Target(
        Type type, string name, RumbleEffect effect, int calls, params Type[] parameters)
    {
        var method = AccessTools.DeclaredMethod(type, name, parameters)
            ?? throw new MissingMethodException(type.FullName, name);
        return (method, effect, calls);
    }
}
