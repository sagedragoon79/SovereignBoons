// Folded from VC_FasterWaterRecharge by VC (v1.0)
// Original DLL: VC_FasterWaterRecharge_FF.dll
// Original prefs: Well water recharge multiplier (range 0.5–10), Well water capcity multiplier
// SB changes: same semantics. Fixed the source's typo "capcity" → "capacity".
//             Lower bound raised from 0.5 to 1.0 — this is a power-spike pack,
//             we don't expose nerf-the-well sliders.
//
// Verified targets (decompile_verification.md):
//   - Well.maxWater (private int = 50) at ff_full.cs:362335
//   - Well.waterGainPerSecond (private float = 0.01f) at ff_full.cs:362341
//   - Public read accessors: maxWaterInStorage, waterPerSec (don't confuse with private names)

using HarmonyLib;
using MelonLoader;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Faster well recharge and bigger well capacity. Multiplier-based so the
    /// boon plays nicely with any tech-tree well upgrades the player researches.
    /// </summary>
    internal static class SpringsVigor
    {
        private static readonly AccessTools.FieldRef<Well, int>? _maxWaterRef =
            AccessTools.FieldRefAccess<Well, int>("maxWater");
        private static readonly AccessTools.FieldRef<Well, float>? _rechargeRef =
            AccessTools.FieldRefAccess<Well, float>("waterGainPerSecond");

        [HarmonyPatch(typeof(Well), "Awake")]
        internal static class Well_Awake_Patch
        {
            private static void Postfix(Well __instance)
            {
                if (!Config.EnableSpringsVigor.Value) return;
                if (Plugin.IsForeignModLoaded("VC_FasterWaterRecharge")) return;

                try
                {
                    if (_maxWaterRef != null)
                    {
                        float mul = Config.SpringsVigorCapacityMult.Value;
                        int boosted = UnityEngine.Mathf.RoundToInt(_maxWaterRef(__instance) * mul);
                        _maxWaterRef(__instance) = boosted;
                    }

                    if (_rechargeRef != null)
                    {
                        float mul = Config.SpringsVigorRechargeMult.Value;
                        _rechargeRef(__instance) = _rechargeRef(__instance) * mul;
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Wetter Wells] Well.Awake postfix failed: {ex.Message}");
                }
            }
        }
    }
}
