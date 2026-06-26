// Folded from Forced Child Labor by Krasipeace (v1.0.3)
// Original DLL: ForcedChildLabor_FF.dll
// Original prefs: AgeCutoffChild (def 7), AgeCutoffAdolescent (def 15), SchoolMinAge (def 5), SchoolMaxAge (def 50)
// SB changes: VillagerHealth fields are STATIC — single write at scene init replaces
//             the source mod's redundant per-instance Awake patch. Defaults are less
//             aggressive than source: child=12 (was 7), adolescent=18 (was 15), school 5-10 (vanilla).
//
// Verified targets (decompile_verification.md):
//   - VillagerHealth.ageCutoffChild (public static int = 15) at ff_full.cs:386726
//   - VillagerHealth.ageCutoffAdolescent (public static int = 25) at ff_full.cs:386728
//   - School.minEnrollmentAge, School.maxEnrollmentAge (public int, per-instance)
//     at ff_full.cs:352047-352049

using HarmonyLib;
using MelonLoader;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Lower the age at which villagers join the labor pool, plus reshape School
    /// enrollment range. VillagerHealth cutoffs are static — applied once per
    /// map load. School fields are per-instance and patched via Awake postfix.
    /// </summary>
    internal static class EagerHands
    {
        // VillagerHealth age cutoffs are process-global statics that the game does NOT
        // reset between maps. Capture the true vanilla baseline on the first call so we
        // can restore it when the boon is later disabled (otherwise the lowered cutoffs
        // would persist process-wide until FF restarts).
        private static int? _vanillaChild;
        private static int? _vanillaAdolescent;

        /// <summary>
        /// Called from Plugin.OnSceneWasInitialized — writes the static VillagerHealth
        /// age cutoffs when the boon is enabled, and restores vanilla when it's disabled.
        /// Idempotent.
        /// </summary>
        public static void ApplyStatics()
        {
            if (Plugin.IsForeignModLoaded("ForcedChildLabor")) return;

            try
            {
                // Capture vanilla once (first call, before we ever overwrite).
                if (!_vanillaChild.HasValue)
                {
                    _vanillaChild      = VillagerHealth.ageCutoffChild;
                    _vanillaAdolescent = VillagerHealth.ageCutoffAdolescent;
                }

                // Always restore vanilla first, so disabling the boon and loading another
                // map reverts the cutoffs instead of leaving the lowered values stuck.
                VillagerHealth.ageCutoffChild      = _vanillaChild      ?? VillagerHealth.ageCutoffChild;
                VillagerHealth.ageCutoffAdolescent = _vanillaAdolescent ?? VillagerHealth.ageCutoffAdolescent;

                if (!Config.EnableEagerHands.Value) return;

                VillagerHealth.ageCutoffChild      = Config.EagerHandsChildAge.Value;
                VillagerHealth.ageCutoffAdolescent = Config.EagerHandsAdolescentAge.Value;
                Plugin.Log.Msg($"[Eager Hands] Age cutoffs set: child={VillagerHealth.ageCutoffChild}, " +
                               $"adolescent={VillagerHealth.ageCutoffAdolescent}");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.Warning($"[Eager Hands] ApplyStatics failed: {ex.Message}");
            }
        }

        [HarmonyPatch(typeof(School), "Awake")]
        internal static class School_Awake_Patch
        {
            private static void Postfix(School __instance)
            {
                if (!Config.EnableEagerHands.Value) return;
                if (Plugin.IsForeignModLoaded("ForcedChildLabor")) return;

                try
                {
                    __instance.minEnrollmentAge = Config.EagerHandsSchoolMinAge.Value;
                    __instance.maxEnrollmentAge = Config.EagerHandsSchoolMaxAge.Value;
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Eager Hands] School.Awake postfix failed: {ex.Message}");
                }
            }
        }
    }
}
