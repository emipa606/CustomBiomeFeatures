using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CustomBiomeFeatures
{
    public class CustomBiomeFeaturesSettingsManager : ModSettings
    {
        private static readonly int hillinessCount = Enum.GetValues(typeof(Hilliness)).Length;

        private static List<BiomeDef> allowsRoads;

        private static List<BiomeDef> allowsRivers;
        private List<BiomeFeatureSettings> biomeSettings;

        private Dictionary<BiomeDef, BiomeFeatureSettings> biomeSettingsMap;

        private Vector2 biomeSettingsScroll = Vector2.zero;

        private BiomeFeatureSettings biomeToRemoveFromSettings;

        public IEnumerable<BiomeFeatureSettings> BiomeSettings => biomeSettings == null
            ? Enumerable.Empty<BiomeFeatureSettings>()
            : biomeSettings.AsEnumerable();

        public void DoSettingsWindowContents(Rect inRect)
        {
            var buttonRect = new Rect(0, inRect.y, inRect.width, 30);
            if (Widgets.ButtonText(buttonRect, "CBF.SelectBiome".Translate()))
            {
                var options = new List<FloatMenuOption>();
                foreach (var biomeDef in DefDatabase<BiomeDef>.AllDefs.Where(x =>
                    biomeSettings == null || !biomeSettings.Any(x2 => x2.BiomeDef == x)).OrderBy(def => def.label))
                {
                    options.Add(new FloatMenuOption(biomeDef.LabelCap, () =>
                    {
                        if (biomeSettings == null)
                        {
                            biomeSettings = new List<BiomeFeatureSettings>();
                        }

                        biomeSettings?.Add(new BiomeFeatureSettings(biomeDef));
                    }));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            buttonRect.y += 30;
            if (Widgets.ButtonText(buttonRect, "CBF.Recache".Translate()))
            {
                RecacheData();
            }

            Widgets.DrawLineHorizontal(0, buttonRect.y + 35, inRect.width);

            if (biomeToRemoveFromSettings != null)
            {
                biomeSettings?.Remove(biomeToRemoveFromSettings);
                biomeSettingsMap?.Remove(biomeToRemoveFromSettings.BiomeDef);
                biomeToRemoveFromSettings = null;
            }

            if (biomeSettings == null || !biomeSettings.Any())
            {
                return;
            }

            var biomeSettingsRect = new Rect(0, inRect.y + 70, inRect.width, inRect.height - 65);
            GUI.BeginGroup(biomeSettingsRect);

            var biomeHeight = 220;
            var biomeSettingsScrollRect = new Rect(0, 0, biomeSettingsRect.width, biomeSettingsRect.height);
            var biomeSettingsVertScrollRect = new Rect(0, 0, biomeSettingsRect.x, biomeSettings.Count * biomeHeight);
            Widgets.BeginScrollView(biomeSettingsScrollRect, ref biomeSettingsScroll, biomeSettingsVertScrollRect);
            var y = 10;
            foreach (var biomeFeaturesSettings in biomeSettings)
            {
                var biomeGenerateSettings = biomeFeaturesSettings;

                var biomeRect = new Rect(0, y, biomeSettingsRect.width - 30, biomeHeight);
                DrawBiomeSettings(biomeRect, biomeGenerateSettings);

                y += biomeHeight;
                Widgets.DrawLineHorizontal(0, y, biomeSettingsRect.width);
            }

            Widgets.EndScrollView();

            GUI.EndGroup();
        }

        public static void SaveVanilla()
        {
            allowsRivers = new List<BiomeDef>();
            allowsRoads = new List<BiomeDef>();
            foreach (var biomeDef in DefDatabase<BiomeDef>.AllDefsListForReading)
            {
                if (biomeDef.allowRoads)
                {
                    allowsRoads.Add(biomeDef);
                }

                if (biomeDef.allowRivers)
                {
                    allowsRivers.Add(biomeDef);
                }
            }
        }

        public static void RecacheData()
        {
            foreach (var biomeDef in DefDatabase<BiomeDef>.AllDefsListForReading)
            {
                if (CustomBiomeFeaturesMod.CustomBiomeFeaturesSettings.TryGetSettings(biomeDef, out var settings))
                {
                    biomeDef.allowRoads = settings.allowRoads;
                    biomeDef.allowRivers = settings.allowRivers;
                }
                else
                {
                    biomeDef.allowRivers = allowsRivers.Contains(biomeDef);
                    biomeDef.allowRoads = allowsRoads.Contains(biomeDef);
                }
            }

            DefDatabase<BiomeDef>.ResolveAllReferences();
        }

        public bool TryGetSettings(BiomeDef biomeDef, out BiomeFeatureSettings settings)
        {
            settings = null;
            if (biomeSettings == null)
            {
                return false;
            }

            biomeSettings = biomeSettings.Where(featureSettings => featureSettings.BiomeDef != null).ToList();

            if (biomeSettingsMap == null || !biomeSettingsMap.ContainsKey(biomeDef))
            {
                biomeSettingsMap = biomeSettings.ToDictionary(k => k.BiomeDef, v => v);
            }

            return biomeSettingsMap.TryGetValue(biomeDef, out settings);
        }

        private void DrawBiomeSettings(Rect rect, BiomeFeatureSettings biomeFeatureSettings)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width - 25, 35), biomeFeatureSettings.BiomeDef.LabelCap);
            var buttonRect = new Rect(rect.width - 25, rect.y + 5, 20, 20);
            if (Widgets.ButtonText(buttonRect, "X"))
            {
                //biomeSettings.Remove(biomeFeatureSettings); Cant do this as it would modify the collection during a loop
                biomeToRemoveFromSettings = biomeFeatureSettings;
            }

            Text.Font = GameFont.Small;

            var settingsRect = new Rect(rect.x, rect.y + 35, rect.width, rect.height - 40);
            GUI.BeginGroup(settingsRect);

            var hillinessSettings = new Rect(0, 0, (rect.width / 2) - 5, hillinessCount * 25);
            //GUI.BeginGroup(hillinessSettings);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0, 0, hillinessSettings.width, 25),
                "CBF.Hilliness_Settings".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            var y = 40;
            foreach (Hilliness hilliness in Enum.GetValues(typeof(Hilliness)))
            {
                if (hilliness == Hilliness.Undefined)
                {
                    continue;
                }

                biomeFeatureSettings.allowedHilliness[hilliness] ^= Widgets.RadioButtonLabeled(
                    new Rect(0, y, hillinessSettings.width, 25), hilliness.GetLabelCap(),
                    biomeFeatureSettings.allowedHilliness[hilliness]);

                y += 25;
            }

            //GUI.EndGroup();

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect((rect.width / 2) + 5, 0, (rect.width / 2) - 5, 25),
                "CBF.Other_Settings".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            var otherSettingsRect = new Rect((rect.width / 2) + 5, 40, (rect.width / 2) - 5, 120);

            var listing_Standard = new Listing_Standard();
            listing_Standard.Begin(otherSettingsRect);
            listing_Standard.CheckboxLabeled("CBF.AllowRoads".Translate(),
                ref biomeFeatureSettings.allowRoads, "CBF.AllowRoads".Translate());
            listing_Standard.CheckboxLabeled("CBF.AllowRivers".Translate(),
                ref biomeFeatureSettings.allowRivers, "CBF.AllowRivers".Translate());
            listing_Standard.CheckboxLabeled("CBF.AllowCaves".Translate(),
                ref biomeFeatureSettings.allowCaves, "CBF.AllowCaves".Translate());
            listing_Standard.CheckboxLabeled("CBF.AllowGeysers".Translate(),
                ref biomeFeatureSettings.allowGeysers, "CBF.AllowGeysers".Translate());
            listing_Standard.CheckboxLabeled("CBF.AllowRuins".Translate(),
                ref biomeFeatureSettings.allowRuins, "CBF.AllowRuins".Translate());
            listing_Standard.End();

            GUI.EndGroup();
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref biomeSettings, "biomeSettings", LookMode.Deep);

            biomeSettings?.RemoveAll(x => x.ShouldRemove());
        }
    }
}