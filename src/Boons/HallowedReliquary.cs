// Folded from VC_ModifyTemple by VC (v1.4)
// Original DLL: VC_ModifyTemple_FF.dll
// Original prefs: RelicAddons (0..15), BonusMul (0.5..3.0), ForceWorkers (bool)
// SB changes (v0.3 PARTIAL FOLD — see _research/IMPLEMENTATION_PLAN.md):
//   - INCLUDED: BonusMul (spirituality bonus per relic) and ForceWorkers (match
//     Temple maxWorkers to relic slot count).
//   - DEFERRED to v0.4: RelicAddons (extra relic slots). The source mod's UI rewire
//     depends on UniverseLib's UIFactory.CreateVerticalGroup, which we don't ship.
//     Without the UI rewire, extra slots exist in game state but can't be assigned
//     through the UI — confusing UX. v0.4 will ship a simpler UI extension.
//
// Verified targets (decompile_verification.md):
//   - ReligionManager._spiritualityBonusPerRelic (private float = 50f) at ff_full.cs:139799
//   - Temple._maxRelicCount (private int) at ff_full.cs:356144 [reserved for v0.4]
//   - Temple.maxWorkers / Resource.maxWorkers — settable via reflection
//     on the property setter (the property is inherited).

using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Spirituality bonus per relic multiplier + optional auto-match of Temple
    /// maxWorkers to relic slot count. Extra relic slots themselves are
    /// reserved for v0.4 once the UI rewire lands.
    /// </summary>
    internal static class HallowedReliquary
    {
        private static readonly AccessTools.FieldRef<ReligionManager, float>? _bonusPerRelicRef =
            AccessTools.FieldRefAccess<ReligionManager, float>("_spiritualityBonusPerRelic");

        // Cache the vanilla value so we can compute (vanilla * multiplier) on re-apply
        // rather than compounding.
        private static float? _vanillaBonusPerRelic;
        private static bool _applied;

        public static void Reset()
        {
            _applied = false;
            // Keep _vanillaBonusPerRelic — it survives across Map reloads since the
            // ReligionManager singleton may be re-used.
        }

        /// <summary>
        /// Called from Plugin.OnUpdate once per second while a game is loaded, until
        /// it successfully captures vanilla and writes the multiplied value.
        /// </summary>
        public static void TryApplyBonusOnce()
        {
            if (_applied) return;
            if (!Config.EnableHallowedReliquary.Value) return;
            if (Plugin.IsForeignModLoaded("VC_ModifyTemple")) return;
            if (_bonusPerRelicRef == null) return;
            if (!GameManager.gameReadyToPlay) return;

            try
            {
                var gm = UnitySingleton<GameManager>.Instance;
                var rm = gm?.religionManager;
                if (rm == null) return;

                if (!_vanillaBonusPerRelic.HasValue)
                    _vanillaBonusPerRelic = _bonusPerRelicRef(rm);

                float mul = Config.HallowedReliquaryBonusMul.Value;
                _bonusPerRelicRef(rm) = _vanillaBonusPerRelic.Value * mul;
                _applied = true;

                Plugin.Log.Msg($"[Hallowed Reliquary] Spirituality bonus per relic: " +
                               $"{_vanillaBonusPerRelic.Value} → {_bonusPerRelicRef(rm)} (×{mul}).");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.Warning($"[Hallowed Reliquary] TryApplyBonusOnce failed: {ex.Message}");
            }
        }

        [HarmonyPatch(typeof(Temple), "Awake")]
        internal static class Temple_Awake_ForceWorkers_Patch
        {
            private static PropertyInfo? _maxWorkersProp;

            private static void Postfix(Temple __instance)
            {
                if (!Config.EnableHallowedReliquary.Value) return;
                if (Plugin.IsForeignModLoaded("VC_ModifyTemple")) return;
                if (!Config.HallowedReliquaryForceWorkers.Value) return;
                if (__instance == null) return;

                try
                {
                    if (_maxWorkersProp == null)
                    {
                        _maxWorkersProp = typeof(Resource).GetProperty("maxWorkers",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                    if (_maxWorkersProp == null || !_maxWorkersProp.CanWrite) return;

                    int currentMax = __instance.maxWorkers;
                    int relicCount = __instance.maxRelicCount;
                    if (currentMax < relicCount)
                        _maxWorkersProp.SetValue(__instance, relicCount, null);
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Hallowed Reliquary] Temple.Awake postfix failed: {ex.Message}");
                }
            }
        }
    }
}
