// Folded from FFEnableAchievements by idontcare (v1.0.0)
// Original DLL: FFEnableAchievements_FF.dll
// Verified target: SettingsManager.allowCustomSettingsForAchievements (public static bool = false)
//   at Assembly-CSharp ff_full.cs:100431

using MelonLoader;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Allows achievements to unlock even when non-default game settings or mods
    /// are in use. Vanilla FF gates achievement awards on a custom-settings check;
    /// this boon flips the static bool that disables that gate.
    ///
    /// Implementation note: the source mod patches nothing — it writes the field
    /// directly at OnInitializeMelon. We do the same, gated on the boon toggle.
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
    }
}
