using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using NativeCloud = StardewValley.BellsAndWhistles.Cloud;

namespace Overcast;

[HarmonyPatch(typeof(GameLocation), nameof(GameLocation.addClouds))]
internal static class CloudSpawnPatch
{
    private static Func<GameLocation, bool> frameworkSpawnCheck = null!;

    [HarmonyAfter("leclair.cloudyskies")]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.Select(code => new CodeInstruction(code)).ToList();
        var seasonCheck = AccessTools.Method(typeof(GameLocation), nameof(GameLocation.IsSummerHere));
        var constructor = AccessTools.Constructor(typeof(NativeCloud), new[] { typeof(Vector2) });
        var seasonChecks = 0;
        var frameworkChecks = 0;
        var constructors = 0;
        for (var index = 0; index < codes.Count; index++)
        {
            var code = codes[index];
            if (code.Calls(seasonCheck))
            {
                code.opcode = OpCodes.Call;
                code.operand = AccessTools.Method(typeof(CloudSpawnPatch), nameof(IsCloudSeason));
                seasonChecks++;
            }
            else if (code.operand is System.Reflection.MethodInfo method
                && method.DeclaringType?.FullName == "Leclair.Stardew.CloudySkies.Patches.GameLocation_Patches"
                && method.Name == "ShouldNotSpawnClouds")
            {
                frameworkSpawnCheck = method.CreateDelegate<Func<GameLocation, bool>>();
                code.operand = AccessTools.Method(typeof(CloudSpawnPatch), nameof(ShouldNotSpawnClouds));
                frameworkChecks++;
            }
            else if (code.opcode == OpCodes.Newobj && Equals(code.operand, constructor))
            {
                // Set zoom before vanilla performs its overlap checks.
                code.opcode = OpCodes.Ldarg_0;
                code.operand = null;
                codes.Insert(++index, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CloudSpawnPatch), nameof(CreateCloud))));
                constructors++;
            }
        }

        if (seasonChecks + frameworkChecks != 1 || constructors != 1)
            throw new InvalidOperationException("Native cloud spawning no longer matches the expected season and constructor hooks.");
        return codes;
    }

    private static bool IsCloudSeason(GameLocation location)
    {
        return CloudRules.CanSpawn(ModEntry.Config, location.GetSeasonKey(), location.GetWeather().Weather);
    }

    private static bool ShouldNotSpawnClouds(GameLocation location)
    {
        var blocked = frameworkSpawnCheck(location);
        var config = ModEntry.Config;
        var season = location.GetSeasonKey();
        var api = ModEntry.WeatherApi;
        if (api is null || !CloudRules.ShouldTune(config, season))
            return blocked;

        var weatherId = location.GetWeather().Weather;
        var spawnClouds = api.GetAllCustomWeather().FirstOrDefault(weather => weather.Id == weatherId)?.SpawnClouds;
        if (!CloudRules.CanSpawn(config, season, weatherId, spawnClouds))
            return true;

        // Extend only the framework's default seasonal check, not an explicit weather veto.
        return blocked && (location.IsRainingHere() || Game1.weatherIcon == 4);
    }

    private static NativeCloud CreateCloud(Vector2 position, GameLocation location)
    {
        var cloud = new NativeCloud(position);
        var config = ModEntry.Config;
        if (CloudRules.ShouldTune(config, location.GetSeasonKey()) && (config.MinSize != 4 || config.MaxSize != 6))
            cloud.zoom = Game1.random.Next(config.MinSize, config.MaxSize + 1);
        return cloud;
    }
}

[HarmonyPatch(typeof(NativeCloud), nameof(NativeCloud.update))]
internal static class CloudMovementPatch
{
    private const float NativeMovementRate = 0.02f;

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.Select(code => new CodeInstruction(code)).ToList();
        var replacements = 0;
        for (var index = 0; index < codes.Count; index++)
        {
            var code = codes[index];
            if (code.opcode != OpCodes.Ldc_R4 || code.operand is not float value || value != NativeMovementRate)
                continue;

            code.opcode = OpCodes.Ldarg_2;
            code.operand = null;
            codes.Insert(++index, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CloudMovementPatch), nameof(GetMovementRate))));
            replacements++;
        }

        if (replacements != 2)
            throw new InvalidOperationException("Native cloud movement no longer matches the expected drift hooks.");
        return codes;
    }

    private static float GetMovementRate(GameLocation location)
    {
        var config = ModEntry.Config;
        return NativeMovementRate * (CloudRules.ShouldTune(config, location.GetSeasonKey()) ? config.SpeedMultiplier : 1f);
    }
}

[HarmonyPatch(typeof(NativeCloud), nameof(NativeCloud.drawAboveFrontLayer))]
internal static class CloudOpacityPatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.Select(code => new CodeInstruction(code)).ToList();
        var white = AccessTools.PropertyGetter(typeof(Color), nameof(Color.White));
        var replacements = 0;
        foreach (var code in codes)
        {
            if (!code.Calls(white))
                continue;

            code.opcode = OpCodes.Call;
            code.operand = AccessTools.Method(typeof(CloudOpacityPatch), nameof(GetDrawColor));
            replacements++;
        }

        if (replacements != 1)
            throw new InvalidOperationException("Native cloud drawing no longer matches the expected colour hook.");
        return codes;
    }

    private static Color GetDrawColor()
    {
        var location = Game1.currentLocation;
        return location is null
            ? Color.White
            : Color.White * CloudRules.GetOpacity(ModEntry.Config, location.GetSeasonKey(), location.GetWeather().Weather);
    }
}
