using System.Linq;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace CustomBiomeFeatures
{
    [HarmonyPatch(typeof(WorldGenerator), "GenerateWorld")]
    [HarmonyPriority(100)]
    public class WorldGenerator_GenerateWorld
    {
        private static void Postfix()
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (!CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.BiomeSettings.Any())
                {
                    return;
                }

                ClearRoads();
                ClearRivers();

                CheckTilesHilliness();

                EnsureElevation();
            });
        }

        private static void EnsureElevation()
        {
            var biomes = CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.BiomeSettings
                .Where(x => x.allowRoads || x.allowRoads)
                .Select(x => x.BiomeDef).ToList();

            for (var tileID = 0; tileID < Find.WorldGrid.TilesCount; tileID++)
            {
                var tile = Find.WorldGrid[tileID];

                if (tile.potentialRoads == null && tile.potentialRivers == null || !biomes.Contains(tile.biome))
                {
                    continue;
                }

                if (tile.elevation <= 0)
                {
                    tile.elevation = 1;
                }
            }
        }

        private static void CheckTilesHilliness()
        {
            var hillinessMap =
                CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.BiomeSettings.ToDictionary(k => k.BiomeDef,
                    v => v.allowedHilliness);

            if (!hillinessMap.Any())
            {
                return;
            }

            for (var tileID = 0; tileID < Find.WorldGrid.TilesCount; tileID++)
            {
                var tile = Find.WorldGrid[tileID];

                if (!hillinessMap.TryGetValue(tile.biome, out var hillAllow))
                {
                    continue;
                }

                if (!hillAllow.TryGetValue(tile.hilliness, out var value))
                {
                    continue;
                }

                if (!value)
                {
                    tile.hilliness = Hilliness.Flat;
                }
            }
        }

        private static void ClearRoads()
        {
            var biomes = CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.BiomeSettings.Where(x => !x.allowRoads)
                .Select(x => x.BiomeDef)
                .ToList();

            if (biomes.Count == 0)
            {
                return;
            }

            for (var tileID = 0; tileID < Find.WorldGrid.TilesCount; tileID++)
            {
                var tile = Find.WorldGrid[tileID];

                if (biomes.Contains(tile.biome))
                {
                    tile.potentialRoads = null;
                }
            }
        }

        private static void ClearRivers()
        {
            var biomes = CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.BiomeSettings.Where(x => !x.allowRivers)
                .Select(x => x.BiomeDef)
                .ToList();

            if (biomes.Count == 0)
            {
                return;
            }

            for (var tileID = 0; tileID < Find.WorldGrid.TilesCount; tileID++)
            {
                var tile = Find.WorldGrid[tileID];

                if (biomes.Contains(tile.biome))
                {
                    tile.potentialRivers = null;
                }
            }
        }
    }
}