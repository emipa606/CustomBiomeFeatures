using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CustomBiomeFeatures
{
    public class CustomBiomeFeaturesMod : Mod
    {
        public static CustomBiomeFeaturesSettingsManager CustomBiomeFeaturesSettings;

        public CustomBiomeFeaturesMod(ModContentPack content) : base(content)
        {
            var harmonyInstance = new Harmony("Mlie.CustomBiomeFeatures");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            CustomBiomeFeaturesSettings = GetSettings<CustomBiomeFeaturesSettingsManager>();
        }

        public override string SettingsCategory()
        {
            return "Custom Biome Features";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            CustomBiomeFeaturesSettings.DoSettingsWindowContents(inRect);
        }
    }
}