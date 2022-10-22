using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CustomBiomeFeatures;

[HarmonyPatch(typeof(WorldGenStep_Rivers), "GenerateRivers")]
[HarmonyPriority(100)]
public class WorldGenStep_Rivers_GenerateRivers
{
    private static readonly SimpleCurve ElevationChangeCost = new SimpleCurve
    {
        new CurvePoint(-1000f, 50f),
        new CurvePoint(-100f, 100f),
        new CurvePoint(0f, 400f),
        new CurvePoint(0f, 5000f),
        new CurvePoint(100f, 50000f),
        new CurvePoint(1000f, 50000f)
    };

    public static bool Prefix(ref WorldGenStep_Rivers __instance)
    {
        Find.WorldPathGrid.RecalculateAllPerceivedPathCosts();
        var coastalWaterTiles = GetCoastalWaterTiles();
        if (!coastalWaterTiles.Any())
        {
            return false;
        }

        var neighbors = new List<int>();
        var array = Find.WorldPathFinder.FloodPathsWithCostForTree(coastalWaterTiles, delegate(int st, int ed)
        {
            var tile = Find.WorldGrid[ed];
            var tile2 = Find.WorldGrid[st];
            Find.WorldGrid.GetTileNeighbors(ed, neighbors);
            var num = neighbors[0];
            foreach (var tileId in neighbors)
            {
                if (GetImpliedElevation(Find.WorldGrid[tileId]) < GetImpliedElevation(Find.WorldGrid[num]))
                {
                    num = tileId;
                }
            }

            var num2 = 1f;
            if (num != st)
            {
                num2 = 2f;
            }

            return Mathf.RoundToInt(num2 *
                                    ElevationChangeCost.Evaluate(GetImpliedElevation(tile2) -
                                                                 GetImpliedElevation(tile)));
        }, tid =>
        {
            if (!CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.TryGetSettings(Find.WorldGrid[tid].biome,
                    out var settings))
            {
                return Find.WorldGrid[tid].WaterCovered;
            }

            if (settings.allowRoads || settings.allowRivers)
            {
                return false;
            }

            return Find.WorldGrid[tid].WaterCovered;
        });
        var flow = new float[array.Length];
        foreach (var index in coastalWaterTiles)
        {
            AccumulateFlow(flow, array, index);
            CreateRivers(flow, array, index);
        }

        return false;
    }

    private static List<int> GetCoastalWaterTiles()
    {
        var list = new List<int>();
        var list2 = new List<int>();
        for (var i = 0; i < Find.WorldGrid.TilesCount; i++)
        {
            if (Find.WorldGrid[i].biome != BiomeDefOf.Ocean)
            {
                continue;
            }

            Find.WorldGrid.GetTileNeighbors(i, list2);
            var b = false;
            foreach (var tileId in list2)
            {
                if (Find.WorldGrid[tileId].biome == BiomeDefOf.Ocean)
                {
                    continue;
                }

                b = true;
                break;
            }

            if (b)
            {
                list.Add(i);
            }
        }

        return list;
    }

    private static float GetImpliedElevation(Tile tile)
    {
        var num = 0f;
        if (CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.TryGetSettings(tile.biome, out var settings))
        {
            if (settings.allowRoads || settings.allowRivers)
            {
                num = 1000;
            }
        }

        switch (tile.hilliness)
        {
            case Hilliness.SmallHills:
                num = 15f;
                break;
            case Hilliness.LargeHills:
                num = 250f;
                break;
            case Hilliness.Mountainous:
                num = 500f;
                break;
            case Hilliness.Impassable:
                num = 1000f;
                break;
        }

        return tile.elevation + num;
    }

    private static void AccumulateFlow(float[] flow, List<int>[] riverPaths, int index)
    {
        var tile = Find.WorldGrid[index];

        var rainFall = tile.rainfall;
        if (CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.TryGetSettings(tile.biome, out var settings))
        {
            if (settings.allowRivers && rainFall < 200 && tile.temperature > 25)
            {
                rainFall = Mathf.Min(rainFall + 1000, 1200);
            }
        }

        flow[index] += rainFall;
        if (riverPaths[index] != null)
        {
            for (var i = 0; i < riverPaths[index].Count; i++)
            {
                AccumulateFlow(flow, riverPaths, riverPaths[index][i]);
                flow[index] += flow[riverPaths[index][i]];
            }
        }

        flow[index] = Mathf.Max(0f, flow[index] - CalculateTotalEvaporation(flow[index], tile.temperature));
    }

    private static void CreateRivers(float[] flow, List<int>[] riverPaths, int index)
    {
        var list = new List<int>();
        Find.WorldGrid.GetTileNeighbors(index, list);
        foreach (var toTile in list)
        {
            var targetFlow = flow[toTile];
            var riverDef = DefDatabase<RiverDef>.AllDefs
                .Where(rd => rd.spawnFlowThreshold > 0 && rd.spawnFlowThreshold <= targetFlow)
                .MaxByWithFallback(rd => rd.spawnFlowThreshold);
            if (riverDef == null || !(Rand.Value < riverDef.spawnChance))
            {
                continue;
            }

            Find.WorldGrid.OverlayRiver(index, toTile, riverDef);
            ExtendRiver(flow, riverPaths, toTile, riverDef);
        }
    }

    private static void ExtendRiver(float[] flow, List<int>[] riverPaths, int index, RiverDef incomingRiver)
    {
        if (riverPaths[index] == null)
        {
            return;
        }

        var bestOutput = riverPaths[index].MaxBy(ni => flow[ni]);
        var riverDef = incomingRiver;
        while (riverDef != null && riverDef.degradeThreshold > flow[bestOutput])
        {
            riverDef = riverDef.degradeChild;
        }

        if (riverDef != null)
        {
            Find.WorldGrid.OverlayRiver(index, bestOutput, riverDef);
            ExtendRiver(flow, riverPaths, bestOutput, riverDef);
        }

        if (incomingRiver.branches == null)
        {
            return;
        }

        foreach (var alternateRiver in riverPaths[index].Where(ni => ni != bestOutput))
        {
            var branch2 = incomingRiver.branches.Where(branch => branch.minFlow <= flow[alternateRiver])
                .MaxByWithFallback(branch => branch.minFlow);
            if (branch2 == null || !(Rand.Value < branch2.chance))
            {
                continue;
            }

            Find.WorldGrid.OverlayRiver(index, alternateRiver, branch2.child);
            ExtendRiver(flow, riverPaths, alternateRiver, branch2.child);
        }
    }

    private static float CalculateEvaporationConstant(float temperature)
    {
        return 0.61121f * Mathf.Exp((18.678f - (temperature / 234.5f)) * (temperature / (257.14f + temperature))) /
               (temperature + 273f);
    }

    private static float CalculateRiverSurfaceArea(float flow)
    {
        return Mathf.Pow(flow, 0.5f);
    }

    private static float CalculateEvaporativeArea(float flow)
    {
        return CalculateRiverSurfaceArea(flow) + 0f;
    }

    private static float CalculateTotalEvaporation(float flow, float temperature)
    {
        return CalculateEvaporationConstant(temperature) * CalculateEvaporativeArea(flow) * 250f;
    }
}