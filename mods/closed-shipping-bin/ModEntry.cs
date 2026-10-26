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

[HarmonyPatch(typeof(ShippingBin), "openShippingBinLid")]
internal static class ShippingBinLidPatch
{
    private static bool Prefix() => false;
}
