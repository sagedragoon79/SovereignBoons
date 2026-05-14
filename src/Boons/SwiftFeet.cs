// Folded from FastVillagers by Krasipeace (v1.0.4)
// Original DLL: FastVillagers_FF.dll
// Original prefs: VillagerSpeed (replaces _shoeBonusBase), WagonMoveSpeed, WagonCarryCapacity
// SB changes: kept same semantics. Renamed "VillagerSpeed" → "ShoeBonus" because
//             the value REPLACES Character._shoeBonusBase (not a multiplier).
//             Vanilla _shoeBonusBase = 0.15; setting higher = faster.
//
// Verified targets (decompile_verification.md):
//   - Character._shoeBonusBase (private float = 0.15f) at ff_full.cs:375831
//   - Character._turningSpeed (private float) — patched via AccessTools, unique on Character
//   - TransportWagon._movementSpeed (private float) at ff_full.cs:359780-region
//   - TransportWagon.carryCapacity (public float) — direct write works
//
// Caveat: Character.Awake fires on every Character including raiders. The source mod
//         knowingly buffs raiders too. We follow that — call it a feature.

using HarmonyLib;
using MelonLoader;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Faster villager run speed (Character._shoeBonusBase replacement) and
    /// beefier transport wagons (movement speed + carry capacity override).
    /// </summary>
    internal static class SwiftFeet
    {
        private static readonly AccessTools.FieldRef<Character, float>? _shoeBonusRef =
            AccessTools.FieldRefAccess<Character, float>("_shoeBonusBase");
        private static readonly AccessTools.FieldRef<Character, float>? _charTurningRef =
            AccessTools.FieldRefAccess<Character, float>("_turningSpeed");
        private static readonly AccessTools.FieldRef<TransportWagon, float>? _wagonSpeedRef =
            AccessTools.FieldRefAccess<TransportWagon, float>("_movementSpeed");
        private static readonly AccessTools.FieldRef<TransportWagon, float>? _wagonTurningRef =
            AccessTools.FieldRefAccess<TransportWagon, float>("_turningSpeed");

        [HarmonyPatch(typeof(Character), "Awake")]
        internal static class Character_Awake_Patch
        {
            private static void Postfix(Character __instance)
            {
                if (!Config.EnableSwiftFeet.Value) return;
                if (Plugin.IsForeignModLoaded("FastVillagers")) return;

                try
                {
                    float bonus = Config.SwiftFeetShoeBonus.Value;
                    if (_shoeBonusRef != null) _shoeBonusRef(__instance) = bonus;
                    if (_charTurningRef != null) _charTurningRef(__instance) = bonus * 50f;
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Swift Feet] Character.Awake postfix failed: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(TransportWagon), "Awake")]
        internal static class TransportWagon_Awake_Patch
        {
            private static void Postfix(TransportWagon __instance)
            {
                if (!Config.EnableSwiftFeet.Value) return;
                if (Plugin.IsForeignModLoaded("FastVillagers")) return;

                try
                {
                    float speed = Config.SwiftFeetWagonSpeed.Value;
                    if (_wagonSpeedRef != null) _wagonSpeedRef(__instance) = speed;
                    if (_wagonTurningRef != null) _wagonTurningRef(__instance) = speed * 50f;
                    __instance.carryCapacity = Config.SwiftFeetWagonCapacity.Value;
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Swift Feet] TransportWagon.Awake postfix failed: {ex.Message}");
                }
            }
        }
    }
}
