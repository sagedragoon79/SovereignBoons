// Inspired by BasicWeaponEquipment by donimuzur (v3.1.0)
// Original DLLs: Basic Weapon Equipment {default,powerful} stats.dll — Il2Cpp-flavored,
//                won't load on Mono build of FF. Mono re-implementation.
// SB changes vs source design:
//   - UI buttons (per-villager + global "arm all") replaced with two configurable
//     hotkeys: Arm All (default B), Unarm All (default N). No UI prefab injection.
//   - Source's debug `B` → SpawnRaidCamps keybind dropped; `B` repurposed for Arm.
//   - Two-DLL stat preset split collapsed to one float pref `LevysArmsStatMagnitude`.
//   - itemRequester re-weapon-fetch + search-entry tuning deferred to v0.7. v0.6
//     ships the meaningful core: militia combat config + ItemStats buff. Villagers
//     fight with whatever weapon they already carry.
//   - In-memory armed-set tracking. Does NOT persist across save/load.
//   - ChangeOccupation postfix re-applies the buff so it doesn't get clobbered.
//
// Inaccessible fields handled via reflection:
//   - CombatComponent.defaultIsMeleeAttack — private bool
//   - VillagerOccupation.occupation — protected Occupation field

using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Hotkey-driven militia summon. Arms every eligible villager (non-Hunter,
    /// non-Guard, non-Soldier, non-Child) with militia combat config + stat buff.
    /// Buff re-applies on occupation change.
    /// </summary>
    internal static class LevysArms
    {
        private static readonly HashSet<int> _skipOccupations = new HashSet<int> { 1, 9, 21, 45 };
        private static readonly HashSet<int> _armedIds = new HashSet<int>();

        private static KeyCode _armKey = KeyCode.B;
        private static KeyCode _unarmKey = KeyCode.N;

        // Reflected access for fields not in our compilation scope.
        private static readonly FieldInfo? _defaultIsMeleeAttackField =
            AccessTools.Field(typeof(CombatComponent), "defaultIsMeleeAttack");
        private static readonly FieldInfo? _occupationField =
            AccessTools.Field(typeof(VillagerOccupation), "occupation");

        public static void Reset()
        {
            _armedIds.Clear();
        }

        /// <summary>
        /// Public reflective-friendly query: "is this villager currently armed by
        /// Levy's Arms?" Used by sibling mods (e.g. Essential Provisions' Self
        /// Preservation) to skip flee logic for our militia. Returns false when the
        /// boon is disabled OR the villager wasn't armed by us.
        ///
        /// Stable API — keep the signature `public static bool IsArmed(Villager)`
        /// so reflective callers don't break across SB versions.
        /// </summary>
        public static bool IsArmed(Villager v)
        {
            if (v == null) return false;
            if (!Config.EnableLevysArms.Value) return false;
            return _armedIds.Contains(v.GetInstanceID());
        }

        public static void ResolveHotkeys()
        {
            if (System.Enum.TryParse<KeyCode>(Config.LevysArmsArmKey.Value, ignoreCase: true, out var k1)) _armKey = k1;
            if (System.Enum.TryParse<KeyCode>(Config.LevysArmsUnarmKey.Value, ignoreCase: true, out var k2)) _unarmKey = k2;
        }

        public static void OnUpdate()
        {
            if (!Config.EnableLevysArms.Value) return;
            if (!GameManager.gameReadyToPlay) return;

            if (Input.GetKeyDown(_armKey))
            {
                int n = ArmAllEligible();
                Plugin.Log.Msg($"[Levy's Arms] Armed {n} villager(s).");
            }
            else if (Input.GetKeyDown(_unarmKey))
            {
                int n = UnarmAll();
                Plugin.Log.Msg($"[Levy's Arms] Unarmed {n} villager(s).");
            }
        }

        // ---------- Public actions ----------

        public static int ArmAllEligible()
        {
            var gm = UnitySingleton<GameManager>.Instance;
            var villagers = gm?.resourceManager?.villagersRO;
            if (villagers == null) return 0;
            var cm = gm?.combatManager;
            if (cm == null) return 0;

            int count = 0;
            foreach (var v in villagers)
            {
                if (v == null || IsSkippedOccupation(v)) continue;
                try
                {
                    EquipWeapon(v, cm);
                    _armedIds.Add(v.GetInstanceID());
                    count++;
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Levy's Arms] Arm failed for {v.name}: {ex.Message}");
                }
            }
            return count;
        }

        public static int UnarmAll()
        {
            var gm = UnitySingleton<GameManager>.Instance;
            var villagers = gm?.resourceManager?.villagersRO;
            if (villagers == null) return 0;

            int count = 0;
            foreach (var v in villagers)
            {
                if (v == null) continue;
                int id = v.GetInstanceID();
                if (!_armedIds.Contains(id)) continue;
                try { UnequipWeapon(v); _armedIds.Remove(id); count++; }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Levy's Arms] Unarm failed for {v.name}: {ex.Message}");
                }
            }
            return count;
        }

        private static bool IsSkippedOccupation(Villager v)
        {
            if (v.occupation == null) return true;
            if (_occupationField == null) return true;
            var occ = _occupationField.GetValue(v.occupation);
            if (occ == null) return true;
            return _skipOccupations.Contains((int)occ);
        }

        private static int? GetOccupationValue(Villager v)
        {
            if (v.occupation == null || _occupationField == null) return null;
            var occ = _occupationField.GetValue(v.occupation);
            return occ == null ? (int?)null : (int)occ;
        }

        // ---------- Buff application ----------

        private static void EquipWeapon(Villager v, CombatManager cm)
        {
            if (v.combatComp == null) return;

            // Combat config — make the villager a hostile-target-seeking militia unit.
            // Public set accessors on CombatComponent / DamageableComponent.
            _defaultIsMeleeAttackField?.SetValue(v.combatComp, true);
            v.combatComp.trainingLevel = 30;
            v.combatComp.searchRange = 90f;
            v.combatComp.applyChaseRetreatingTargetRules = false;
            v.combatComp.teamDef = cm.guardTowerTeamDefinition;
            v.combatComp.combatTargetType = (CombatTargetType)5;

            // Stat buff — single magnitude scales every Perc field uniformly.
            float m = Config.LevysArmsStatMagnitude.Value;
            if (v.equipmentManager != null)
            {
                v.equipmentManager.baseItemStats = new ItemStats
                {
                    maxLifeModifierPerc          = m,
                    armorModifierPerc            = m,
                    meleeDamageReductionPerc     = m,
                    meleeDamageIncreasePerc      = m,
                    meleeDamageTier2IncreasePerc = m,
                    rangedDamageReductionPerc    = m,
                    rangedDamageIncreasePerc     = m,
                    rangedDamageTier2IncreasePerc= m,
                    attackSpeedModifierPerc      = m,
                    mountedMoveSpeedModifierPerc = m,
                };
            }
        }

        private static void UnequipWeapon(Villager v)
        {
            if (v.combatComp != null)
                _defaultIsMeleeAttackField?.SetValue(v.combatComp, false);
            if (v.equipmentManager != null)
                v.equipmentManager.baseItemStats = default(ItemStats);
            // Note: teamDef stays at guardTowerTeamDefinition until save reload.
        }

        // ---------- ChangeOccupation re-application ----------

        [HarmonyPatch(typeof(Villager), "ChangeOccupation")]
        internal static class Villager_ChangeOccupation_Patch
        {
            private static void Postfix(Villager __instance, VillagerOccupation.Occupation occupationToChangeTo)
            {
                if (!Config.EnableLevysArms.Value) return;
                if (__instance == null) return;
                if (!_armedIds.Contains(__instance.GetInstanceID())) return;

                // If villager moves into a skipped occupation, drop them from armed set.
                if (_skipOccupations.Contains((int)occupationToChangeTo))
                {
                    _armedIds.Remove(__instance.GetInstanceID());
                    return;
                }

                var gm = UnitySingleton<GameManager>.Instance;
                var cm = gm?.combatManager;
                if (cm == null) return;

                try { EquipWeapon(__instance, cm); }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Levy's Arms] Re-arm on ChangeOccupation failed for {__instance.name}: {ex.Message}");
                }
            }
        }
    }
}
