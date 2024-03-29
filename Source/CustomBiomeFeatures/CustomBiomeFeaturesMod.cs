﻿using System.Reflection;
using HarmonyLib;
using Mlie;
using UnityEngine;
using Verse;

namespace CustomBiomeFeatures;

public class CustomBiomeFeaturesMod : Mod
{
    public static CustomBiomeFeaturesSettingsManager CustomBiomeFeaturesSettings;

    public static string currentVersion;

    public CustomBiomeFeaturesMod(ModContentPack content) : base(content)
    {
        var harmonyInstance = new Harmony("Mlie.CustomBiomeFeatures");
        harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

        CustomBiomeFeaturesSettings = GetSettings<CustomBiomeFeaturesSettingsManager>();
        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
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