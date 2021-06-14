using HarmonyLib;
using RimWorld;
using Verse;

namespace CustomBiomeFeatures
{
    [HarmonyPatch(typeof(GenStep_ScatterThings), "Generate", typeof(Map), typeof(GenStepParams))]
    [HarmonyPriority(100)]
    public class GenStep_ScatterThings_Generate
    {
        private static bool Prefix(GenStep_ScatterThings __instance, ref Map map)
        {
            if (__instance.thingDef != ThingDefOf.SteamGeyser ||
                !CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.TryGetSettings(map.Biome, out var settings))
            {
                return true;
            }

            return settings.allowGeysers;
        }
    }
}