using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace CustomBiomeFeatures
{
    [HarmonyPatch(typeof(World), "HasCaves", typeof(int))]
    [HarmonyPriority(100)]
    public class World_HasCaves
    {
        public static bool Prefix(int tile, ref bool __result)
        {
            var tile2 = Find.WorldGrid[tile];

            if (!CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.TryGetSettings(tile2.biome, out var settings))
            {
                return true;
            }

            if (settings.allowCaves)
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}