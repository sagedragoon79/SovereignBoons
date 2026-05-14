// Folded from VC_ModifyTemple by VC (v1.4)
// Original DLL: VC_ModifyTemple_FF.dll
// Original prefs: RelicAddons (0..15), BonusMul (0.5..3.0), ForceWorkers (bool)
// SB changes:
//   - REPLACED RelicAddons + ForceWorkers with a simpler decoupling boon:
//     "Unchain Relics" prevents Temple.AdjustRelicsBasedOnPriestCount from
//     deactivating relics when worker count is lower than slot count. With
//     this on, a single priest activates ALL relics in the Temple — solves the
//     same "I want my Temple stronger" need without needing the UniverseLib UI
//     rewire the source mod did to add extra slots.
//   - KEPT BonusMul (spirituality bonus per relic multiplier) unchanged.
//
// Verified targets:
//   - ReligionManager._spiritualityBonusPerRelic (private float = 50f) at ff_full.cs:139799
//   - Temple.AdjustRelicsBasedOnPriestCount (private void) at ff_full.cs:356722
//     Vanilla logic: if relicSlots.Count > workersRO.Count → demote relics into
//     disabledSlots. If <, promote back from disabled. We hijack to NEVER demote.
//   - Temple.ActivateRelic / DeactivateRelic (private) at ff_full.cs:356586/356597
//   - Temple's private List<Relic> relicSlots / disabledSlots
//     at ff_full.cs:356148 / 356150

using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Spirituality bonus per relic multiplier + "Unchain Relics" (one priest
    /// activates every assigned relic, regardless of worker count).
    /// </summary>
    internal static class HallowedReliquary
    {
        // ---------- ReligionManager bonus multiplier ----------

        private static readonly AccessTools.FieldRef<ReligionManager, float>? _bonusPerRelicRef =
            AccessTools.FieldRefAccess<ReligionManager, float>("_spiritualityBonusPerRelic");

        private static float? _vanillaBonusPerRelic;
        private static bool _applied;

        public static void Reset() => _applied = false;

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

        // ---------- Unchain Relics ----------

        private static readonly AccessTools.FieldRef<Temple, List<Relic>>? _relicSlotsRef =
            AccessTools.FieldRefAccess<Temple, List<Relic>>("relicSlots");
        private static readonly AccessTools.FieldRef<Temple, List<Relic>>? _disabledSlotsRef =
            AccessTools.FieldRefAccess<Temple, List<Relic>>("disabledSlots");

        // Cached MethodInfo for the private ActivateRelic call. We look it up once
        // at first use rather than every patch invocation.
        private static MethodInfo? _activateRelicMI;
        private static MethodInfo ResolveActivateRelic()
        {
            return _activateRelicMI ??= AccessTools.Method(typeof(Temple), "ActivateRelic")
                ?? throw new System.MissingMethodException("Temple.ActivateRelic not found");
        }

        [HarmonyPatch(typeof(Temple), "AdjustRelicsBasedOnPriestCount")]
        internal static class Temple_AdjustRelicsBasedOnPriestCount_Patch
        {
            // Prefix: when Unchain Relics is on AND the temple has ≥1 priest, promote
            // every relic in disabledSlots back into relicSlots, then skip vanilla.
            // If 0 priests, defer to vanilla (no relics should be active without a
            // staffed temple — preserves vanilla expectations).
            private static bool Prefix(Temple __instance)
            {
                if (!Config.EnableHallowedReliquary.Value) return true;
                if (!Config.HallowedReliquaryUnchainRelics.Value) return true;
                if (Plugin.IsForeignModLoaded("VC_ModifyTemple")) return true;
                if (__instance == null) return true;
                if (__instance.workersRO == null || __instance.workersRO.Count == 0) return true;
                if (_relicSlotsRef == null || _disabledSlotsRef == null) return true;

                try
                {
                    var slots    = _relicSlotsRef(__instance);
                    var disabled = _disabledSlotsRef(__instance);
                    if (slots == null || disabled == null) return true;

                    // Promote every disabled relic back to active.
                    while (disabled.Count > 0)
                    {
                        int idx = disabled.Count - 1;
                        var relic = disabled[idx];
                        slots.Add(relic);
                        disabled.RemoveAt(idx);
                        if (relic != null)
                            ResolveActivateRelic().Invoke(__instance, new object[] { relic });
                    }

                    // Skip vanilla so it doesn't immediately re-deactivate them.
                    return false;
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Hallowed Reliquary] Unchain prefix failed, " +
                                       $"falling through to vanilla: {ex.Message}");
                    return true;
                }
            }
        }
    }
}
