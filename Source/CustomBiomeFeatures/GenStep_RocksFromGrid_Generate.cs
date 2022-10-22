using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CustomBiomeFeatures;

[HarmonyPatch(typeof(GenStep_RocksFromGrid), "Generate", typeof(Map), typeof(GenStepParams))]
[HarmonyPriority(100)]
public class GenStep_RocksFromGrid_Generate
{
    private static bool Prefix(ref Map map)
    {
        return !CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.TryGetSettings(map.Biome, out var settings) ||
               settings.allowedHilliness.Any(x => x.Value);
    }
}