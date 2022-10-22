using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace CustomBiomeFeatures;

[HarmonyPatch(typeof(WorldGenStep_Terrain), "GenerateTileFor", typeof(int))]
[HarmonyPriority(100)]
public class WorldGenStep_Terrain_GenerateTileFor
{
    private static readonly FieldInfo noiseElevationField = typeof(WorldGenStep_Terrain).GetField("noiseElevation",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo noiseMountainLinesField =
        typeof(WorldGenStep_Terrain).GetField("noiseMountainLines",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo noiseHillsPatchesMicroField =
        typeof(WorldGenStep_Terrain).GetField("noiseHillsPatchesMicro",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo noiseTemperatureOffsetField =
        typeof(WorldGenStep_Terrain).GetField("noiseTemperatureOffset",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo noiseRainfallField = typeof(WorldGenStep_Terrain).GetField("noiseRainfall",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo noiseSwampinessField =
        typeof(WorldGenStep_Terrain).GetField("noiseSwampiness",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly MethodInfo BaseTemperatureAtLatitudeMethod =
        typeof(WorldGenStep_Terrain).GetMethod("BaseTemperatureAtLatitude",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

    private static readonly MethodInfo TemperatureReductionAtElevationMethod =
        typeof(WorldGenStep_Terrain).GetMethod("TemperatureReductionAtElevation",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

    private static readonly MethodInfo BiomeFromMethod = typeof(WorldGenStep_Terrain).GetMethod("BiomeFrom",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

    private static bool Prefix(ref WorldGenStep_Terrain __instance, ref int tileID, ref Tile __result)
    {
        var tile = new Tile();
        var tileCenter = Find.WorldGrid.GetTileCenter(tileID);
        tile.elevation = ((ModuleBase)noiseElevationField.GetValue(__instance)).GetValue(tileCenter);
        HillinessFor(__instance, tile, tileCenter);
        var num = (float)BaseTemperatureAtLatitudeMethod.Invoke(__instance,
            new object[] { Find.WorldGrid.LongLatOf(tileID).y });
        num -= (float)TemperatureReductionAtElevationMethod.Invoke(__instance, new object[] { tile.elevation });
        num += ((ModuleBase)noiseTemperatureOffsetField.GetValue(__instance)).GetValue(tileCenter);
        var temperatureCurve = Find.World.info.overallTemperature.GetTemperatureCurve();
        if (temperatureCurve != null)
        {
            num = temperatureCurve.Evaluate(num);
        }

        tile.temperature = num;
        tile.rainfall = ((ModuleBase)noiseRainfallField.GetValue(__instance)).GetValue(tileCenter);
        if (float.IsNaN(tile.rainfall))
        {
            Log.ErrorOnce(
                ((ModuleBase)noiseRainfallField.GetValue(__instance)).GetValue(tileCenter) + " rain bad at " +
                tileID, 694822);
        }

        if (tile.hilliness == Hilliness.Flat || tile.hilliness == Hilliness.SmallHills)
        {
            tile.swampiness = ((ModuleBase)noiseSwampinessField.GetValue(__instance)).GetValue(tileCenter);
        }

        tile.biome = (BiomeDef)BiomeFromMethod.Invoke(__instance, new object[] { tile, tileID });

        if (CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.TryGetSettings(tile.biome, out _))
        {
            HillinessWithoutElevation(__instance, tile, tileCenter);
        }

        __result = tile;

        return false;
    }

    private static void HillinessWithoutElevation(WorldGenStep_Terrain __instance, Tile tile, Vector3 tileCenter)
    {
        var value = ((ModuleBase)noiseMountainLinesField.GetValue(__instance)).GetValue(tileCenter);
        if (value > 0.235f)
        {
            if (((ModuleBase)noiseHillsPatchesMicroField.GetValue(__instance)).GetValue(tileCenter) > 0.46f &&
                ((ModuleBase)noiseHillsPatchesMicroField.GetValue(__instance)).GetValue(tileCenter) > -0.3f)
            {
                tile.hilliness = Rand.Bool ? Hilliness.SmallHills : Hilliness.LargeHills;
            }
            else
            {
                tile.hilliness = Hilliness.Flat;
            }
        }
        else if (value > 0.12f)
        {
            switch (Rand.Range(0, 4))
            {
                case 0:
                    tile.hilliness = Hilliness.Flat;
                    break;
                case 1:
                    tile.hilliness = Hilliness.SmallHills;
                    break;
                case 2:
                    tile.hilliness = Hilliness.LargeHills;
                    break;
                case 3:
                    tile.hilliness = Hilliness.Mountainous;
                    break;
            }
        }
        else if (value > 0.0363f)
        {
            tile.hilliness = Hilliness.Mountainous;
        }
        else
        {
            tile.hilliness = Hilliness.Impassable;
        }
    }

    private static void HillinessFor(WorldGenStep_Terrain __instance, Tile tile, Vector3 tileCenter)
    {
        var value = ((ModuleBase)noiseMountainLinesField.GetValue(__instance)).GetValue(tileCenter);
        if (value > 0.235f || tile.elevation <= 0f)
        {
            if (tile.elevation > 0f &&
                ((ModuleBase)noiseHillsPatchesMicroField.GetValue(__instance)).GetValue(tileCenter) > 0.46f &&
                ((ModuleBase)noiseHillsPatchesMicroField.GetValue(__instance)).GetValue(tileCenter) > -0.3f)
            {
                tile.hilliness = Rand.Bool ? Hilliness.SmallHills : Hilliness.LargeHills;
            }
            else
            {
                tile.hilliness = Hilliness.Flat;
            }
        }
        else if (value > 0.12f)
        {
            switch (Rand.Range(0, 4))
            {
                case 0:
                    tile.hilliness = Hilliness.Flat;
                    break;
                case 1:
                    tile.hilliness = Hilliness.SmallHills;
                    break;
                case 2:
                    tile.hilliness = Hilliness.LargeHills;
                    break;
                case 3:
                    tile.hilliness = Hilliness.Mountainous;
                    break;
            }
        }
        else if (value > 0.0363f)
        {
            tile.hilliness = Hilliness.Mountainous;
        }
        else
        {
            tile.hilliness = Hilliness.Impassable;
        }
    }
}