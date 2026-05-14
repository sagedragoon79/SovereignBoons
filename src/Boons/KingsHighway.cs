// Inspired by Rapid Roads by Olleus (v1.0.0)
// Original DLL: Rapid Roads_FF.dll
// Original prefs: NONE — values hardcoded.
// SB changes: NARROWED to player-favoring patches only.
//   - KEPT: AIGridNode.RecalculateRoadSpeedBonus (road bonus boost) — buff for the player.
//   - KEPT: AggressiveAnimal.movementSpeed slow — nerf for predators, buff for the player.
//   - DROPPED: Character off-road penalty, BatteringRam off-road, Catapult off-road —
//     all three are nerfs that punish the player off-road. Sovereign Boons is a
//     POWER-SPIKE pack, not a balance/penalty pack; off-road penalties don't fit.
//
// Verified targets (decompile_verification.md):
//   - AIGridNode.RecalculateRoadSpeedBonus (private void) at ff_full.cs:477934
//   - AggressiveAnimal.movementSpeed (override getter) at ff_full.cs:35007

using AIPathfinding;
using HarmonyLib;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Faster travel on roads + slower aggressive animals. Two patches, each
    /// independently toggleable.
    /// </summary>
    internal static class KingsHighway
    {
        [HarmonyPatch(typeof(AIGridNode), "RecalculateRoadSpeedBonus")]
        internal static class AIGridNode_Recalculate_Patch
        {
            private static void Postfix(AIGridNode __instance)
            {
                if (!Config.EnableKingsHighway.Value) return;
                if (!Config.KingsHighwayBoostRoadSpeed.Value) return;
                if (Plugin.IsForeignModLoaded("Rapid Roads")) return;
                if (__instance == null) return;

                float mul = Config.KingsHighwayRoadSpeedMul.Value;
                if (mul == 1f) return;
                if (__instance.nodeSpeedBonus <= 0f) return; // not on a road
                __instance.nodeSpeedBonus *= mul;
            }
        }

        [HarmonyPatch(typeof(AggressiveAnimal), "movementSpeed", MethodType.Getter)]
        internal static class AggressiveAnimal_Speed_Patch
        {
            private static void Postfix(ref float __result)
            {
                if (!Config.EnableKingsHighway.Value) return;
                if (!Config.KingsHighwaySlowAggressiveAnimals.Value) return;
                if (Plugin.IsForeignModLoaded("Rapid Roads")) return;
                __result *= Config.KingsHighwayAggressiveAnimalMul.Value;
            }
        }
    }
}
