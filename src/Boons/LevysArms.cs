// Inspired by BasicWeaponEquipment by donimuzur (v3.1.0)
// Original DLLs: Basic Weapon Equipment {default,powerful} stats.dll — Il2Cpp-flavored,
//                won't load on Mono build of FF. Mono re-implementation.
// SB changes vs source design:
//   - UI buttons (per-villager + global "arm all") replaced with two configurable
//     hotkeys: Arm All (default B), Unarm All (default N). No UI prefab injection.
//   - Source's debug `B` → SpawnRaidCamps keybind dropped; `B` repurposed for Arm.
//   - Two-DLL stat preset split collapsed to one float pref `LevysArmsStatMagnitude`.
//   - Weapon fetch IS implemented: arming sets an Upgrade melee seek group
//     (Weapon -> SimpleWeapon) via VillagerItemRequester.SetItemCriteriaToSeek, so
//     militia request + visibly equip a crafted weapon through normal logistics
//     (one-shot, no polling). Mirrors vanilla VillagerOccupationGuard. Unarm clears it.
//   - Hotkey chords supported (Ctrl+A, Alt+Shift+M) via the Chord parser.
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
        // Skip occupations that are already combatants or shouldn't be armed:
        // 1=Hunter, 9=Guard, 13=TransitionToSoldier, 21=Child, 45=Soldier.
        // (Guard/Hunter/TransitionToSoldier are the only occupations that use the
        // item-seek-criteria system, so skipping them also avoids clobbering it.)
        private static readonly HashSet<int> _skipOccupations = new HashSet<int> { 1, 9, 13, 21, 45 };
        private static readonly HashSet<int> _armedIds = new HashSet<int>();

        // A hotkey chord: a base KeyCode plus required modifier state. Modifiers must
        // match EXACTLY (so "A" won't fire while Ctrl is held, and "Ctrl+A" won't fire
        // without Ctrl). Supports e.g. "B", "Ctrl+A", "Alt+Shift+M".
        private struct Chord
        {
            public KeyCode Key;
            public bool Ctrl, Alt, Shift;
        }
        private static Chord _armChord   = new Chord { Key = KeyCode.B };
        private static Chord _unarmChord = new Chord { Key = KeyCode.N };

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
        /// Emergency Militia?" Used by sibling mods (e.g. Essential Provisions' Self
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
            _armChord   = ParseChord(Config.LevysArmsArmKey.Value,   KeyCode.B);
            _unarmChord = ParseChord(Config.LevysArmsUnarmKey.Value, KeyCode.N);
        }

        /// <summary>
        /// Parse a hotkey string into a Chord. Accepts a bare Unity KeyCode ("B", "F4")
        /// or a modifier chord ("Ctrl+A", "Alt+Shift+M"). Recognised modifiers:
        /// Ctrl/Control, Alt, Shift. Falls back to <paramref name="fallback"/> (no
        /// modifiers) if no valid base key is found.
        /// </summary>
        private static Chord ParseChord(string raw, KeyCode fallback)
        {
            var c = new Chord { Key = fallback };
            if (string.IsNullOrWhiteSpace(raw)) return c;

            bool gotKey = false;
            foreach (var tokenRaw in raw.Split('+'))
            {
                var t = tokenRaw.Trim();
                if (t.Length == 0) continue;
                switch (t.ToLowerInvariant())
                {
                    case "ctrl": case "control": c.Ctrl = true; break;
                    case "alt": c.Alt = true; break;
                    case "shift": c.Shift = true; break;
                    default:
                        if (System.Enum.TryParse<KeyCode>(t, ignoreCase: true, out var k)) { c.Key = k; gotKey = true; }
                        break;
                }
            }
            if (!gotKey) { c.Key = fallback; c.Ctrl = c.Alt = c.Shift = false; }
            return c;
        }

        private static bool ChordPressed(Chord c)
        {
            if (!Input.GetKeyDown(c.Key)) return false;
            bool ctrl  = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool alt   = Input.GetKey(KeyCode.LeftAlt)     || Input.GetKey(KeyCode.RightAlt);
            bool shift = Input.GetKey(KeyCode.LeftShift)   || Input.GetKey(KeyCode.RightShift);
            return ctrl == c.Ctrl && alt == c.Alt && shift == c.Shift;
        }

        public static void OnUpdate()
        {
            if (!Config.EnableLevysArms.Value) return;
            if (!GameManager.gameReadyToPlay) return;

            if (ChordPressed(_armChord))
            {
                int n = ArmAllEligible();
                Plugin.Log.Msg($"[Emergency Militia] Armed {n} villager(s).");
            }
            else if (ChordPressed(_unarmChord))
            {
                int n = UnarmAll();
                Plugin.Log.Msg($"[Emergency Militia] Unarmed {n} villager(s).");
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
                    Plugin.Log.Warning($"[Emergency Militia] Arm failed for {v.name}: {ex.Message}");
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
                    Plugin.Log.Warning($"[Emergency Militia] Unarm failed for {v.name}: {ex.Message}");
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

            // Weapon fetch — request a melee weapon through normal logistics so the
            // militia visibly equips one (and the player must actually CRAFT weapons for
            // this to do anything). One-shot: we set the seek criteria here; FF's own
            // logistics delivers it. No polling/scan. Mirrors vanilla VillagerOccupationGuard's
            // melee group (Upgrade = take any weapon, prefer the better tier).
            // Worker occupations don't use the seek-criteria system, so overwriting it here
            // and clearing on unequip is safe (no normal criteria to clobber).
            if (v.itemRequester != null)
            {
                var meleeGroup = new SeekItemGroup(SeekItemGroup.GroupInteraction.Upgrade);
                meleeGroup.entries.Add(new SeekItemEntry(ItemID.Weapon, 1u, 0u));
                meleeGroup.entries.Add(new SeekItemEntry(ItemID.SimpleWeapon, 1u, 0u));
                v.itemRequester.SetItemCriteriaToSeek(new List<SeekItemGroup> { meleeGroup });
            }
        }

        private static void UnequipWeapon(Villager v)
        {
            if (v.combatComp != null)
                _defaultIsMeleeAttackField?.SetValue(v.combatComp, false);
            if (v.equipmentManager != null)
                v.equipmentManager.baseItemStats = default(ItemStats);
            // Stop seeking a weapon. Worker occupations have no normal seek criteria,
            // so clearing returns them to their default (empty) state. The held weapon
            // returns to storage through the villager's normal unused-item handling.
            v.itemRequester?.ClearItemCriteriaToSeek();
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
                    Plugin.Log.Warning($"[Emergency Militia] Re-arm on ChangeOccupation failed for {__instance.name}: {ex.Message}");
                }
            }
        }
    }
}
