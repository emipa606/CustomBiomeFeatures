using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CustomBiomeFeatures
{
    [HarmonyPatch(typeof(GenStep_RockChunks), "Generate", typeof(Map), typeof(GenStepParams))]
    [HarmonyPriority(100)]
    public class GenStep_RockChunks_Generate
    {
        private static bool Prefix(ref Map map)
        {
            if (!CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.TryGetSettings(map.Biome, out var settings))
            {
                return true;
            }

            return settings.allowedHilliness.Any(x => x.Value);
        }
    }
}