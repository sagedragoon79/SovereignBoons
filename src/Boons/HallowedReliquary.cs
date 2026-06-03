// Folded from VC_ModifyTemple by VC (v1.4)
// Original DLL: VC_ModifyTemple_FF.dll
// Original prefs: RelicAddons (0..15), BonusMul (0.5..3.0), ForceWorkers (bool)
// SB changes:
//   - Folded VC's RelicAddons via two faithful Harmony patches (no UniverseLib
//     needed — UniverseLib in VC is only used for its standalone config
//     window; the actual slot expansion is plain Object.Instantiate). SB
//     exposes the addon count as HallowedReliquaryBonusSlots (range 0..6 →
//     2..8 total) and clones UI widgets from the existing AssignedRelic slot.
//   - Unchain Relics layered on top: once slots exist, this toggle promotes
//     every disabled relic back to active so a single priest activates all
//     installed relics (vanilla otherwise caps active = priest count).
//   - KEPT BonusMul (spirituality bonus per relic multiplier) unchanged.
//   - DROPPED VC's ForceWorkers — Greater Halls already covers worker counts.
//
// Verified targets:
//   - Temple._maxRelicCount [SerializeField int] — extended in Awake Prefix.
//   - Temple.relicSlotCooldowns (private List<int>) — vanilla Awake adds 6
//     entries unconditionally; we pre-pad with (newMax*2 - 6) so the total
//     stays at newMax*2 (matches VC's formula; covers active + disabled).
//   - UISubWidgetTempleControls.relicSlots (private List<UIRelicSlot>) —
//     extended in Init Prefix by cloning the first slot's GameObject.
//   - Temple.AdjustRelicsBasedOnPriestCount (private void) — postfix promotes
//     disabledSlots → relicSlots for Unchain Relics, then fires onRelicsChanged.
//   - ReligionManager._spiritualityBonusPerRelic (private float = 50f) — BonusMul.
//   - ResourceManager.templesRO — sweep target for toggle-mid-session.
//
// History notes (2026-05-29):
//   - Initial "unchain only" design left slot count at vanilla 2; user wanted
//     more slots. Attempted to bump _maxRelicCount + pad disabled in postfix —
//     this broke the Temple UI because the slot UI widgets are baked into the
//     prefab at vanilla count. Reverted, then ported VC's real approach:
//     extend in Awake AND clone widget GameObjects in the UI sub-widget Init.

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Spirituality bonus per relic multiplier + "Unchain Relics" (one priest
    /// exposes every relic slot, regardless of worker count).
    /// </summary>
    internal static class HallowedReliquary
    {
        // ---------- ReligionManager bonus multiplier ----------

        private static readonly AccessTools.FieldRef<ReligionManager, float>? _bonusPerRelicRef =
            AccessTools.FieldRefAccess<ReligionManager, float>("_spiritualityBonusPerRelic");

        private static float? _vanillaBonusPerRelic;
        private static bool _applied;

        public static void Reset()
        {
            _applied = false;
            // Map (re)load → re-sweep so a save loaded with the toggle on has its
            // temples promoted without waiting for a priest change.
            _sweepNeeded = true;
            _expandedTempleIds.Clear();
            _expandedWidgetIds.Clear();
            _unlockedThisMap = false;
        }

        // ---------- Unlock All Relics (one-shot per map) ----------

        private static bool _unlockedThisMap;

        public static void TryUnlockAllRelicsOnce()
        {
            if (_unlockedThisMap) return;
            if (!Config.EnableHallowedReliquary.Value) return;
            if (!Config.HallowedReliquaryUnlockAllRelics.Value) return;
            if (Plugin.IsForeignModLoaded("VC_ModifyTemple")) return;
            if (!GameManager.gameReadyToPlay) return;

            try
            {
                var gm = UnitySingleton<GameManager>.Instance;
                var rm = gm?.religionManager;
                if (rm == null) return;

                int before = rm.obtainedRelicsRO?.Count ?? 0;
                rm.UnlockAllRelics();
                int after = rm.obtainedRelicsRO?.Count ?? before;
                _unlockedThisMap = true;
                Plugin.Log.Msg($"[Hallowed Reliquary] Unlocked all relics: {before} → {after} obtained.");
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning($"[Hallowed Reliquary] UnlockAllRelics failed: {ex.Message}");
            }
        }

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
            catch (Exception ex)
            {
                Plugin.Log.Warning($"[Hallowed Reliquary] TryApplyBonusOnce failed: {ex.Message}");
            }
        }

        // ---------- Unchain Relics ----------

        private static readonly AccessTools.FieldRef<Temple, List<Relic>>? _relicSlotsRef =
            AccessTools.FieldRefAccess<Temple, List<Relic>>("relicSlots");
        private static readonly AccessTools.FieldRef<Temple, List<Relic>>? _disabledSlotsRef =
            AccessTools.FieldRefAccess<Temple, List<Relic>>("disabledSlots");
        private static readonly AccessTools.FieldRef<Temple, int>? _maxRelicCountRef =
            AccessTools.FieldRefAccess<Temple, int>("_maxRelicCount");
        private static readonly FieldInfo? _onRelicsChangedField =
            AccessTools.Field(typeof(Temple), "onRelicsChanged");

        /// <summary>Temples we've already expanded in Awake — prevents double-extending if Awake
        /// somehow re-fires on the same instance.</summary>
        private static readonly HashSet<int> _expandedTempleIds = new HashSet<int>();

        /// <summary>Temple UI sub-widgets we've already cloned slots into.</summary>
        private static readonly HashSet<int> _expandedWidgetIds = new HashSet<int>();

        // Cached MethodInfo for the private ActivateRelic call.
        private static MethodInfo? _activateRelicMI;
        private static MethodInfo? ResolveActivateRelic()
        {
            return _activateRelicMI ??= AccessTools.Method(typeof(Temple), "ActivateRelic");
        }

        // Cached MethodInfo for the recovery sweep.
        private static MethodInfo? _adjustRelicsMI;
        private static MethodInfo? ResolveAdjustRelics()
        {
            return _adjustRelicsMI ??= AccessTools.Method(typeof(Temple), "AdjustRelicsBasedOnPriestCount");
        }

        // Sweep state: trigger a manual AdjustRelics call on every Temple when
        // a natural trigger won't (toggle flipped mid-game, save just loaded).
        private static bool _sweepNeeded = true;
        private static bool _lastUnchainToggle;

        /// <summary>
        /// Called from Plugin.OnUpdate. Idempotent: only does work when a sweep
        /// is pending (after map load or toggle change), then clears the flag.
        /// </summary>
        public static void MaybeSweepTemples()
        {
            if (!GameManager.gameReadyToPlay) return;
            if (!Config.EnableHallowedReliquary.Value) return;
            if (Plugin.IsForeignModLoaded("VC_ModifyTemple")) return;

            // Toggle change → schedule a sweep (covers both off→on and on→off;
            // off→on promotes, on→off lets vanilla rebalance on next AdjustRelics
            // call we trigger).
            bool current = Config.HallowedReliquaryUnchainRelics.Value;
            if (current != _lastUnchainToggle)
            {
                _lastUnchainToggle = current;
                _sweepNeeded = true;
            }

            if (!_sweepNeeded) return;

            try
            {
                var gm = UnitySingleton<GameManager>.Instance;
                var temples = gm?.resourceManager?.templesRO;
                if (temples == null) return;

                var adjust = ResolveAdjustRelics();
                if (adjust == null) return;

                int swept = 0;
                foreach (var temple in temples)
                {
                    if (temple == null) continue;
                    try { adjust.Invoke(temple, null); swept++; }
                    catch (Exception ex)
                    {
                        Plugin.Log.Warning($"[Hallowed Reliquary] AdjustRelics sweep failed on a temple: {ex.Message}");
                    }
                }
                _sweepNeeded = false;
                if (swept > 0)
                    Plugin.Log.Msg($"[Hallowed Reliquary] Swept {swept} temple(s) — relic slots refreshed.");
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning($"[Hallowed Reliquary] Temple sweep outer failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Fire Temple's onRelicsChanged delegate (the UI refresh hook vanilla
        /// invokes in its own AdjustRelics loop). Reflective so we don't care
        /// about access modifier or whether it's declared as event or field.
        /// </summary>
        private static void FireOnRelicsChanged(Temple temple)
        {
            if (_onRelicsChangedField == null) return;
            try
            {
                var del = _onRelicsChangedField.GetValue(temple) as Action;
                del?.Invoke();
            }
            catch { /* best-effort UI refresh; swallow */ }
        }

        [HarmonyPatch(typeof(Temple), "AdjustRelicsBasedOnPriestCount")]
        internal static class Temple_AdjustRelicsBasedOnPriestCount_Patch
        {
            // Postfix: let vanilla balance first (it handles its own onRelicsChanged
            // events while moving slots), THEN promote everything that remains in
            // disabledSlots back to relicSlots and fire the UI refresh once.
            // Idempotent — re-runs cheaply when disabled is already empty.
            private static void Postfix(Temple __instance)
            {
                if (!Config.EnableHallowedReliquary.Value) return;
                if (!Config.HallowedReliquaryUnchainRelics.Value) return;
                if (Plugin.IsForeignModLoaded("VC_ModifyTemple")) return;
                if (__instance == null) return;
                // Preserve vanilla "unstaffed temple = inactive" — only unchain when
                // there's at least one priest.
                if (__instance.workersRO == null || __instance.workersRO.Count == 0) return;
                if (_relicSlotsRef == null || _disabledSlotsRef == null) return;

                try
                {
                    var slots    = _relicSlotsRef(__instance);
                    var disabled = _disabledSlotsRef(__instance);
                    if (slots == null || disabled == null) return;

                    // Promotion is independent of slot count — works whether vanilla 2 or
                    // expanded via the Awake patches below. Caps active relics at whatever
                    // the Temple's current total slot count is (relicSlots + disabledSlots).
                    if (disabled.Count == 0) return;
                    var activate = ResolveActivateRelic();
                    while (disabled.Count > 0)
                    {
                        int idx = disabled.Count - 1;
                        var relic = disabled[idx];
                        slots.Add(relic);
                        disabled.RemoveAt(idx);
                        if (relic != null && activate != null)
                            activate.Invoke(__instance, new object[] { relic });
                    }

                    FireOnRelicsChanged(__instance);
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warning($"[Hallowed Reliquary] Unchain postfix failed: {ex.Message}");
                }
            }
        }

        // ---------- Slot expansion (data layer) ----------

        [HarmonyPatch(typeof(Temple), "Awake")]
        internal static class Temple_Awake_Patch
        {
            // Prefix: runs BEFORE vanilla Awake so:
            //   - _maxRelicCount is already extended when vanilla pads disabledSlots
            //     with `maxRelicCount` nulls (no second pad needed).
            //   - relicSlotCooldowns can be pre-padded so vanilla's hard-coded
            //     `Add(0) * 6` afterwards yields the right total.
            // Matches VC_ModifyTemple's formula: cooldowns total = newMax * 2.
            private static void Prefix(Temple __instance)
            {
                if (!Config.EnableHallowedReliquary.Value) return;
                if (Plugin.IsForeignModLoaded("VC_ModifyTemple")) return;
                if (__instance == null) return;
                if (!_expandedTempleIds.Add(__instance.GetInstanceID())) return;

                int bonus = Config.HallowedReliquaryBonusSlots.Value;
                if (bonus <= 0) return;
                if (_maxRelicCountRef == null) return;

                try
                {
                    int oldMax = _maxRelicCountRef(__instance);
                    int newMax = oldMax + bonus;
                    _maxRelicCountRef(__instance) = newMax;

                    // Extend cooldowns to newMax*2. Vanilla Awake will add 6 more after us,
                    // so we add (newMax*2 - 6) here. Matches VC's formula.
                    var cdField = AccessTools.Field(typeof(Temple), "relicSlotCooldowns");
                    if (cdField?.GetValue(__instance) is List<int> cooldowns)
                    {
                        int targetTotal = newMax * 2;
                        // After our adds + vanilla's 6, we want targetTotal entries.
                        int needToAddNow = targetTotal - 6;
                        for (int i = 0; i < needToAddNow; i++) cooldowns.Add(0);
                    }

                    Plugin.Log.Msg($"[Hallowed Reliquary] Temple expanded: maxRelicCount {oldMax} → {newMax} (+{bonus}).");
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warning($"[Hallowed Reliquary] Temple.Awake expansion failed: {ex.Message}");
                }
            }
        }

        // ---------- Slot expansion (UI layer) ----------

        [HarmonyPatch(typeof(UISubWidgetTempleControls), "Init")]
        internal static class UISubWidgetTempleControls_Init_Patch
        {
            private static readonly FieldInfo? _widgetRelicSlotsField =
                AccessTools.Field(typeof(UISubWidgetTempleControls), "relicSlots");

            // Prefix: clone the prefab's existing UIRelicSlot widget GameObject `bonus`
            // times into its parent layout BEFORE base.Init() runs UpdateControls.
            // UpdateControls indexes `relicSlots[i]` up to `temple.relicSlotsRO.Count`,
            // so we must have enough widgets in the list to cover every data slot.
            private static void Prefix(UISubWidgetTempleControls __instance)
            {
                if (!Config.EnableHallowedReliquary.Value) return;
                if (Plugin.IsForeignModLoaded("VC_ModifyTemple")) return;
                if (__instance == null) return;
                if (!_expandedWidgetIds.Add(__instance.GetInstanceID())) return;

                int bonus = Config.HallowedReliquaryBonusSlots.Value;
                if (bonus <= 0) return;
                if (_widgetRelicSlotsField == null) return;

                try
                {
                    if (_widgetRelicSlotsField.GetValue(__instance) is not List<UIRelicSlot> list)
                    {
                        Plugin.Log.Warning("[Hallowed Reliquary] relicSlots field cast failed; UI clone skipped.");
                        return;
                    }
                    if (list.Count == 0 || list[0] == null)
                    {
                        Plugin.Log.Warning("[Hallowed Reliquary] no template slot in widget; UI clone skipped.");
                        return;
                    }

                    // Use the first prefab slot as the template. Its GameObject sits inside
                    // a HorizontalLayoutGroup row container; that row container itself sits in
                    // a parent that ideally has a VerticalLayoutGroup so new rows stack below.
                    var templateGO = list[0].gameObject;
                    var row1 = templateGO != null ? templateGO.transform.parent : null;
                    if (templateGO == null || row1 == null)
                    {
                        Plugin.Log.Warning("[Hallowed Reliquary] template slot has no parent; UI clone skipped.");
                        return;
                    }

                    // Decide row layout. ROW_CAPACITY is conservative — vanilla shows 2 slots
                    // with comfortable spacing; pushing past ~5 per row starts visibly compressing.
                    const int ROW_CAPACITY = 5;
                    int vanillaInRow1 = list.Count;
                    int row1ClonesPossible = System.Math.Max(0, ROW_CAPACITY - vanillaInRow1);
                    int row1Clones = System.Math.Min(bonus, row1ClonesPossible);
                    int remaining = bonus - row1Clones;

                    int added = 0;
                    // --- Row 1: clone into the existing row container ---
                    for (int i = 0; i < row1Clones; i++)
                    {
                        var clone = UnityEngine.Object.Instantiate(templateGO, row1);
                        clone.name = templateGO.name + $"_sb_r1_{i}";
                        var slot = clone.GetComponentInChildren<UIRelicSlot>(includeInactive: true);
                        if (slot != null) { list.Add(slot); added++; }
                    }

                    // --- Extra rows: only attempt if parent supports vertical stacking. ---
                    if (remaining > 0)
                    {
                        var row1Parent = row1.parent;
                        bool canStackVertically = row1Parent != null &&
                            row1Parent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() != null;

                        if (!canStackVertically)
                        {
                            // Fallback: dump the rest into row 1. They'll compress visibly past
                            // ROW_CAPACITY but slot data still works — better than dropped slots.
                            Plugin.Log.Warning($"[Hallowed Reliquary] Row parent has no VerticalLayoutGroup — cramming remaining {remaining} slots into row 1.");
                            for (int i = 0; i < remaining; i++)
                            {
                                var clone = UnityEngine.Object.Instantiate(templateGO, row1);
                                clone.name = templateGO.name + $"_sb_overflow_{i}";
                                var slot = clone.GetComponentInChildren<UIRelicSlot>(includeInactive: true);
                                if (slot != null) { list.Add(slot); added++; }
                            }
                        }
                        else
                        {
                            int rowIdx = 2;
                            int baseSibling = row1.GetSiblingIndex();
                            while (remaining > 0)
                            {
                                int slotsThisRow = System.Math.Min(ROW_CAPACITY, remaining);

                                // Clone the row container (with its HorizontalLayoutGroup + sizing),
                                // strip the inherited children, then add fresh slot clones.
                                var newRowGO = UnityEngine.Object.Instantiate(row1.gameObject, row1Parent);
                                newRowGO.name = row1.name + $"_sb_row{rowIdx}";
                                newRowGO.transform.SetSiblingIndex(baseSibling + rowIdx - 1);

                                while (newRowGO.transform.childCount > 0)
                                {
                                    var child = newRowGO.transform.GetChild(0);
                                    child.SetParent(null);
                                    UnityEngine.Object.Destroy(child.gameObject);
                                }

                                for (int i = 0; i < slotsThisRow; i++)
                                {
                                    var clone = UnityEngine.Object.Instantiate(templateGO, newRowGO.transform);
                                    clone.name = templateGO.name + $"_sb_r{rowIdx}_{i}";
                                    var slot = clone.GetComponentInChildren<UIRelicSlot>(includeInactive: true);
                                    if (slot != null) { list.Add(slot); added++; }
                                }

                                remaining -= slotsThisRow;
                                rowIdx++;
                            }
                        }
                    }

                    Plugin.Log.Msg($"[Hallowed Reliquary] Cloned {added} relic slot UI widget(s) (list now {list.Count}).");
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warning($"[Hallowed Reliquary] UI clone failed: {ex.Message}");
                }
            }
        }
    }
}
