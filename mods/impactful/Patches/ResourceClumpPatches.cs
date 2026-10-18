using HarmonyLib;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace Impactful.Patches;

[HarmonyPatch(typeof(ResourceClump), nameof(ResourceClump.performToolAction), new[]
{
    typeof(Tool), typeof(int), typeof(Microsoft.Xna.Framework.Vector2)
})]
internal static class ResourceClumpPatches
{
    private readonly record struct State(float Health, int FacingDirection, float Strength);

    private static void Prefix(ResourceClump __instance, Tool? t, ref State? __state)
    {
        var mod = ModEntry.Instance;
        if (!mod.CanShake)
            return;

        var strength = t switch
        {
            Axe when mod.Config.Trees && __instance.parentSheetIndex.Value is ResourceClump.stumpIndex or ResourceClump.hollowLogIndex => ImpactTuning.HardwoodBreak,
            Pickaxe when mod.Config.Mining => ImpactTuning.LargeRockBreak,
            _ => 0f
        };
        if (strength <= 0)
            return;

        var who = t!.getLastFarmerToUse();
        if (who?.IsLocalPlayer == true)
            __state = new State(__instance.health.Value, who.FacingDirection, strength);
    }

    private static void Postfix(ResourceClump __instance, bool __result, State? __state)
    {
        if (__state is not { Health: > 0 } state || !__result || __instance.health.Value > 0)
            return;

        ModEntry.Instance.Emit(state.Strength, ModEntry.DirectionFromFacing(state.FacingDirection));
    }
}
