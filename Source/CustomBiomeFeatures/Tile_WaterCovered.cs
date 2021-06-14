using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace CustomBiomeFeatures
{
    [HarmonyPatch(typeof(Tile), "WaterCovered", MethodType.Getter)]
    [HarmonyPriority(100)]
    public class Tile_WaterCovered
    {
        public static bool Prefix(ref Tile __instance, ref bool __result)
        {
            if (Current.ProgramState != ProgramState.MapInitializing || __instance.biome == null ||
                !(__instance.elevation < 0))
            {
                return true;
            }

            if (!CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.TryGetSettings(__instance.biome,
                out var settings))
            {
                return true;
            }

            if (!settings.allowedHilliness.TryGetValue(__instance.hilliness, out var result))
            {
                return true;
            }

            if (!result)
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}