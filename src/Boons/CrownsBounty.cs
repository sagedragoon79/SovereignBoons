// Folded from TaxGoldgainMono by coos (v1.1.0)
// Original DLL: TaxGoldgainMono.dll
// Original prefs: IncomeMultiplier (def 5.0)
// SB changes: NARROWED scope. The source mod multiplies every positive gold gain
//             (sales, refunds, event rewards, tax). Sovereign Boons gates on
//             gain.type == Gain.Type.TaxCollection — true to the boon's name.
//
// Verified targets (decompile_verification.md):
//   - ExpenseManager.AddGain(Gain) (public void) at ff_full.cs:151443
//   - Gain is ExpenseManager.Gain (nested public struct) at ff_full.cs:150698
//   - Gain.amount (public int) at ff_full.cs:150720
//   - Gain.type is Gain.Type enum (Manufacturing, Trade, Refund, TaxCollection)

using HarmonyLib;
using MelonLoader;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Multiplies gold from tax-collection events. Sales, manufacturing refunds,
    /// and trade gains keep their vanilla amounts.
    /// </summary>
    internal static class CrownsBounty
    {
        [HarmonyPatch(typeof(ExpenseManager), "AddGain")]
        internal static class ExpenseManager_AddGain_Patch
        {
            private static void Prefix(ref ExpenseManager.Gain gain)
            {
                if (!Config.EnableCrownsBounty.Value) return;
                if (Plugin.IsForeignModLoaded("TaxGoldgainMono")) return;

                if (gain.type != ExpenseManager.Gain.Type.TaxCollection) return;
                if (gain.amount <= 0) return;

                try
                {
                    float mul = Config.CrownsBountyTaxMultiplier.Value;
                    if (mul == 1.0f) return;

                    int boosted = UnityEngine.Mathf.RoundToInt(gain.amount * mul);
                    if (boosted < gain.amount) boosted = gain.amount; // never reduce
                    gain.amount = boosted;
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Crown's Bounty] AddGain prefix failed: {ex.Message}");
                }
            }
        }
    }
}
