// Folded from VC_NoBlizzardAndDrought by VC (v1.1)
// Original DLL: VC_NoBlizzardAndDrought.dll
// Original prefs: Blizzard Active, Heatwave Active, Extreme weather Active, Drought Active
//                 (all default FALSE — i.e. removed by default — aggressive defaults)
// SB changes: Inverted polarity. Prefs are now "Disable<X>" (default FALSE = vanilla
//             behavior). The source mod's confusing "Active=false means remove" reads
//             backwards. Same behavior, clearer naming.
//
// Verified targets (decompile_verification.md):
//   - Weather.Initialize (public void) at ff_full.cs:398366
//   - Weather.RollForWeatherEvents (private void) at ff_full.cs:398491
//   - WeatherManager.Update (public void) at ff_full.cs:398732
//   - Weather.extremeWeatherOptions (private List<ExtremeWeatherSettings>) at ff_full.cs:398283
//   - BlizzardSettings : ExtremeWeatherSettings at ff_full.cs:397630
//   - HeatWaveSettings : ExtremeWeatherSettings at ff_full.cs:397647
//   - WeatherProfile.chanceOfDrought (AnimationCurve) at ff_full.cs:398748

using HarmonyLib;
using UnityEngine;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Independently suppresses Blizzard / Heatwave / Drought / all-extreme
    /// weather events. Every toggle defaults OFF (vanilla weather). Flip
    /// individual toggles ON to remove the corresponding hazard.
    /// </summary>
    internal static class TemperateSkies
    {
        private static readonly AccessTools.FieldRef<Weather, System.Collections.Generic.List<ExtremeWeatherSettings>>? _extremeOptionsRef =
            AccessTools.FieldRefAccess<Weather, System.Collections.Generic.List<ExtremeWeatherSettings>>("extremeWeatherOptions");

        private static bool AnyExtremeSuppressed
            => Config.TemperateSkiesDisableBlizzard.Value
            || Config.TemperateSkiesDisableHeatwave.Value
            || Config.TemperateSkiesDisableAllExtreme.Value;

        [HarmonyPatch(typeof(Weather), "Initialize")]
        internal static class Weather_Initialize_Patch
        {
            private static void Postfix(Weather __instance)
            {
                if (!Config.EnableTemperateSkies.Value) return;
                if (Plugin.IsForeignModLoaded("VC_NoBlizzardAndDrought")) return;
                if (_extremeOptionsRef == null) return;

                try
                {
                    var opts = _extremeOptionsRef(__instance);
                    if (opts == null) return;

                    if (Config.TemperateSkiesDisableBlizzard.Value || Config.TemperateSkiesDisableAllExtreme.Value)
                        opts.RemoveAll(x => x is BlizzardSettings);
                    if (Config.TemperateSkiesDisableHeatwave.Value || Config.TemperateSkiesDisableAllExtreme.Value)
                        opts.RemoveAll(x => x is HeatWaveSettings);
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Temperate Skies] Weather.Initialize postfix failed: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(Weather), "RollForWeatherEvents")]
        internal static class Weather_RollForWeatherEvents_Patch
        {
            private static void Prefix(Weather __instance)
            {
                if (!Config.EnableTemperateSkies.Value) return;
                if (Plugin.IsForeignModLoaded("VC_NoBlizzardAndDrought")) return;
                if (!AnyExtremeSuppressed) return;

                try
                {
                    // currentWeatherProfile is a STRUCT field — assigning through it
                    // modifies the underlying storage (compiler addresses inner field
                    // directly). Don't copy to a local: that would mutate a copy.
                    if (Config.TemperateSkiesDisableAllExtreme.Value
                        && __instance.currentWeatherProfile.chanceOfExtremeWeather > 0f)
                    {
                        __instance.currentWeatherProfile.chanceOfExtremeWeather = 0f;
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Temperate Skies] RollForWeatherEvents prefix failed: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(WeatherManager), "Update")]
        internal static class WeatherManager_Update_Patch
        {
            // Reused flat-zero curve so we don't allocate every frame.
            private static AnimationCurve? _flatZero;

            private static void Prefix(WeatherManager __instance)
            {
                if (!Config.EnableTemperateSkies.Value) return;
                if (Plugin.IsForeignModLoaded("VC_NoBlizzardAndDrought")) return;
                if (!Config.TemperateSkiesDisableDrought.Value) return;

                try
                {
                    if (_flatZero == null)
                    {
                        _flatZero = new AnimationCurve(
                            new Keyframe(0f, 0f),
                            new Keyframe(0.5f, 0f),
                            new Keyframe(1f, 0f));
                    }
                    // currentWeatherProfile is a struct field — direct field-on-field
                    // assignment is OK.
                    if (__instance.currentWeatherProfile.chanceOfDrought != _flatZero)
                        __instance.currentWeatherProfile.chanceOfDrought = _flatZero;
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Temperate Skies] WeatherManager.Update prefix failed: {ex.Message}");
                }
            }
        }
    }
}
