// Folded from FFEnableAchievements by idontcare (v1.0.0)
// Original DLL: FFEnableAchievements_FF.dll
// Verified targets:
//   - SettingsManager.allowCustomSettingsForAchievements (public static bool = false)
//   - SettingsManager.AreCustomGameOptionsSet(bool allowAchievementsWithCheats = false) (public static)
//
// Why the field alone is NOT enough (fix added 2026-05-29):
//   The game gates achievements in two INCONSISTENT ways:
//     1. Unlock/stat paths (SteamAchievement.UnlockAchievement, SteamStat.SetInt/FloatStat,
//        UIAchievements.Initialize) use:  allowCustomSettingsForAchievements || !AreCustomGameOptionsSet(cheats:true)
//        → setting the field TRUE makes these pass, so achievements DO unlock Steam-side.
//     2. UIAchievements.OnOpened uses:    if (AreCustomGameOptionsSet(cheats:true)) { disable window }
//        → it NEVER reads the field, so with custom settings the in-game achievements WINDOW
//        greys out even though unlocking works. That is the user-visible "doesn't work with
//        custom settings" bug.
//   Fix: also patch AreCustomGameOptionsSet so the ACHIEVEMENT-context calls (they all pass
//   allowAchievementsWithCheats:true) return false when the boon is on. That uniformly
//   neutralizes the custom-options gate across BOTH the unlock paths and the UI window, while
//   leaving the start-screen "Custom map" display calls (which use the false default) untouched.

using System;
using HarmonyLib;
using MelonLoader;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Allows achievements to unlock — and the in-game achievements window to stay usable —
    /// even when non-default game settings or mods are in use. Sets the vanilla opt-out field
    /// AND patches the custom-options gate for achievement-context calls (the field alone
    /// doesn't cover UIAchievements.OnOpened).
    /// </summary>
    internal static class SteadfastResolve
    {
        public static void Apply()
        {
            if (!Config.EnableSteadfastResolve.Value) return;
            if (Plugin.IsForeignModLoaded("FFEnableAchievements")) return;

            try
            {
                SettingsManager.allowCustomSettingsForAchievements = true;
                Plugin.Log.Msg("[Achieve Cheese] Achievements unlocked for custom settings.");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.Warning($"[Achieve Cheese] Apply failed: {ex.Message}");
            }
        }

        // Neutralize the custom-options gate for achievement-context calls only. Every
        // achievement-related caller passes allowAchievementsWithCheats:true; the start-screen
        // map-type display calls use the false default and are left alone (so the map still
        // reads "Custom" correctly).
        [HarmonyPatch(typeof(SettingsManager), nameof(SettingsManager.AreCustomGameOptionsSet), new Type[] { typeof(bool) })]
        internal static class AreCustomGameOptionsSet_Patch
        {
            private static bool Prefix(bool allowAchievementsWithCheats, ref bool __result)
            {
                if (!Config.EnableSteadfastResolve.Value) return true;
                if (Plugin.IsForeignModLoaded("FFEnableAchievements")) return true;
                if (!allowAchievementsWithCheats) return true; // start-screen display path — don't touch

                __result = false; // achievement context: pretend no custom options are set
                return false;     // skip original
            }
        }
    }
}
