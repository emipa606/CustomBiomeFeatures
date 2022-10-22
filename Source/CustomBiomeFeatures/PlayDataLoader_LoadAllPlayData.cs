using HarmonyLib;
using Verse;

namespace CustomBiomeFeatures;

[HarmonyPatch(typeof(UIRoot_Entry), "Init")]
[HarmonyPriority(100)]
public class PlayDataLoader_LoadAllPlayData
{
    private static void Postfix()
    {
        CustomBiomeFeaturesSettingsManager.SaveVanilla();
        CustomBiomeFeaturesSettingsManager.RecacheData();
    }
}