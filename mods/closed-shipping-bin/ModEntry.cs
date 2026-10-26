using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Buildings;

namespace ClosedShippingBin;

internal sealed class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        var harmony = new Harmony(ModManifest.UniqueID);
        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }
        catch (Exception exception)
        {
            harmony.UnpatchAll(ModManifest.UniqueID);
            Monitor.Log($"Closed Shipping Bin could not patch shipping bin lids; vanilla behaviour is unchanged. {exception}", LogLevel.Error);
        }
    }
}

[HarmonyPatch(typeof(ShippingBin))]
internal static class ShippingBinLidPatch
{
    private static readonly ConditionalWeakTable<ShippingBin, object> InteractedBins = new();

    [HarmonyPrefix]
    [HarmonyPatch("openShippingBinLid")]
    private static bool OpenLid(ShippingBin __instance) => InteractedBins.TryGetValue(__instance, out _);

    [HarmonyPrefix]
    [HarmonyPatch("closeShippingBinLid")]
    private static void CloseLid(ShippingBin __instance)
    {
        // Vanilla requests closing only when no farmers remain in the bin's proximity.
        InteractedBins.Remove(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShippingBin.doAction))]
    private static void Interact(ShippingBin __instance, bool __result)
    {
        if (__result)
            InteractedBins.GetOrCreateValue(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShippingBin.showShipment))]
    private static void ShipItem(ShippingBin __instance)
    {
        InteractedBins.GetOrCreateValue(__instance);
    }
}
