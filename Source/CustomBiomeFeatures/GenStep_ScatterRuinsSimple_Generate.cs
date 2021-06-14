using HarmonyLib;
using RimWorld;
using Verse;

namespace CustomBiomeFeatures
{
    [HarmonyPatch(typeof(GenStep_ScatterRuinsSimple), "CanScatterAt", typeof(IntVec3), typeof(Map))]
    [HarmonyPriority(100)]
    public class GenStep_ScatterRuinsSimple_Generate
    {
        private static bool Prefix(ref Map map, ref bool __result)
        {
            if (!CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.TryGetSettings(map.Biome, out var settings))
            {
                return true;
            }

            if (settings.allowRuins)
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}