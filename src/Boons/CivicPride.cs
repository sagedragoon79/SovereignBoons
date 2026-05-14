// Folded from VC_DesirabilityBuildingsControl by VC (v1.0)
// Original DLL: VC_DesirabilityBuildingsControl_FF.dll
// Original prefs: RadiusMul + BonusMul (range 0.5x..10x)
// SB changes: lower bound stays at 0.5 (some desirability layouts work better
//             with a tighter radius for stacking). Once-per-instance via HashSet
//             follows the source's pattern — DecorativeBuilding.Awake can fire
//             multiple times in edge cases.
//
// Verified targets (decompile_verification.md):
//   - DecorativeBuilding inherits from Building (ff_full.cs:338371).
//   - _strategicPlanningRadius (50f) and _strategicPlanningBonus are on Building base
//     at ff_full.cs:327184/327188. Use FieldRef<Building, float> — NOT <DecorativeBuilding, float>.
//     AccessTools walks the inheritance chain.

using System.Collections.Generic;
using HarmonyLib;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Multiplies every DecorativeBuilding's desirability radius and bonus
    /// magnitude. Pretty buildings get prettier.
    /// </summary>
    internal static class CivicPride
    {
        // Building-base fields, accessed via DecorativeBuilding instance reference
        // by walking the inheritance chain.
        private static readonly AccessTools.FieldRef<Building, float>? _radiusRef =
            AccessTools.FieldRefAccess<Building, float>("_strategicPlanningRadius");
        private static readonly AccessTools.FieldRef<Building, float>? _bonusRef =
            AccessTools.FieldRefAccess<Building, float>("_strategicPlanningBonus");

        // Track which instances we've already scaled so re-entrant Awake calls
        // don't compound the multiplier.
        private static readonly HashSet<int> _scaled = new HashSet<int>();

        public static void Reset() => _scaled.Clear();

        [HarmonyPatch(typeof(DecorativeBuilding), "Awake")]
        internal static class DecorativeBuilding_Awake_Patch
        {
            private static void Postfix(DecorativeBuilding __instance)
            {
                if (!Config.EnableCivicPride.Value) return;
                if (Plugin.IsForeignModLoaded("VC_DesirabilityBuildingsControl")) return;
                if (__instance == null) return;

                int key = __instance.GetInstanceID();
                if (!_scaled.Add(key)) return;

                try
                {
                    float radiusMul = Config.CivicPrideRadiusMul.Value;
                    float bonusMul  = Config.CivicPrideBonusMul.Value;

                    if (_radiusRef != null && radiusMul != 1.0f)
                        _radiusRef(__instance) = _radiusRef(__instance) * radiusMul;

                    if (_bonusRef != null && bonusMul != 1.0f)
                        _bonusRef(__instance) = _bonusRef(__instance) * bonusMul;
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Civic Pride] DecorativeBuilding.Awake postfix failed: {ex.Message}");
                }
            }
        }
    }
}
