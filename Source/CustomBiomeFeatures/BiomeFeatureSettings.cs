using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CustomBiomeFeatures
{
    public class BiomeFeatureSettings : IExposable
    {
        public bool allowCaves;

        public Dictionary<Hilliness, bool> allowedHilliness;

        public bool allowGeysers;

        public bool allowRivers;

        public bool allowRoads;

        public bool allowRuins;
        private BiomeDef biomeDef;
        private int biomeDefHash;

        //for expose
        public BiomeFeatureSettings()
        {
        }

        public BiomeFeatureSettings(BiomeDef biomeDef)
        {
            biomeDefHash = biomeDef.shortHash;
            var notWater = !biomeDef.workerClass.Name.Contains("Ocean");
            var hasHills = !biomeDef.workerClass.Name.Contains("Ice") && notWater;
            allowedHilliness = new Dictionary<Hilliness, bool>();
            foreach (Hilliness hilliness in Enum.GetValues(typeof(Hilliness)))
            {
                if (hilliness == Hilliness.Undefined)
                {
                    continue;
                }

                allowedHilliness.Add(hilliness, hasHills);
            }

            allowRivers = biomeDef.allowRivers;
            allowRoads = biomeDef.allowRoads;
            allowCaves = hasHills;
            allowGeysers = notWater;
            allowRuins = notWater;
        }

        public BiomeDef BiomeDef
        {
            get
            {
                if (biomeDef == null)
                {
                    biomeDef = DefDatabase<BiomeDef>.AllDefsListForReading.Find(bDef => bDef.shortHash == biomeDefHash);
                }

                return biomeDef;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref biomeDefHash, "biomeDefHash");
            Scribe_Collections.Look(ref allowedHilliness, "allowedHilliness", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref allowRoads, "allowRoads");
            Scribe_Values.Look(ref allowRivers, "allowRivers");
            Scribe_Values.Look(ref allowCaves, "allowCaves");
            Scribe_Values.Look(ref allowGeysers, "allowGeysers");
            Scribe_Values.Look(ref allowRuins, "allowRuins");
        }

        public bool ShouldRemove()
        {
            return biomeDefHash == 0;
        }
    }
}