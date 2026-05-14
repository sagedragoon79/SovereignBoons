// Folded from VC_UserStorageConfig by VC (v1.3)
// Original DLL: VC_UserStorageConfig_FF.dll
// Original prefs: per-storage-type {Apply, StorageCapacityMul, MinLimits[4], MaxLimits[4]}
//                 with per-item-category min/max quotas (Food/Raw/Produced/Usables).
// SB changes: capacity multiplier per storage type only. The source mod's per-category
//             min/max quota system became redundant when vanilla FF shipped per-category
//             quota UI natively — so Sovereign Boons leaves that to the base game.
//
// Verified targets (decompile_verification.md):
//   - StorageBuilding._storageItemCountCapacity (private int = 2000) at ff_full.cs:353388
//     (catalog had this wrong as _maxStorage — corrected.)
//   - Public read accessor: storageItemCountCapacity (base + bonus)
//   - Subclasses present: RootCellar, StorageDepot, Granary, Stockyard, Storehouse,
//     Treasury, MarketBuilding

using HarmonyLib;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Per-storage-type capacity multiplier. Each storage type gets its own
    /// toggle (so the player can buff Granary without touching Treasury) and
    /// a multiplier slider.
    /// </summary>
    internal static class HoardedStores
    {
        private static readonly AccessTools.FieldRef<StorageBuilding, int>? _capRef =
            AccessTools.FieldRefAccess<StorageBuilding, int>("_storageItemCountCapacity");

        [HarmonyPatch(typeof(StorageBuilding), "Start")]
        internal static class StorageBuilding_Start_Patch
        {
            private static void Postfix(StorageBuilding __instance)
            {
                if (!Config.EnableHoardedStores.Value) return;
                if (Plugin.IsForeignModLoaded("VC_UserStorageConfig")) return;
                if (__instance == null || _capRef == null) return;

                try
                {
                    bool apply;
                    float mul;
                    LookupConfig(__instance, out apply, out mul);
                    if (!apply || mul == 1.0f) return;

                    int current = _capRef(__instance);
                    int boosted = UnityEngine.Mathf.RoundToInt(current * mul);
                    if (boosted < current) boosted = current; // never shrink
                    _capRef(__instance) = boosted;
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Hoarded Stores] StorageBuilding.Start postfix failed: {ex.Message}");
                }
            }
        }

        private static void LookupConfig(StorageBuilding sb, out bool apply, out float mul)
        {
            // Walked in declaration order — most-specific concrete subclass first
            // so MarketBuilding doesn't get caught by a generic StorageBuilding match.
            switch (sb)
            {
                case RootCellar _:      apply = Config.HoardedStoresRootCellarEnable.Value;    mul = Config.HoardedStoresRootCellarMul.Value;    return;
                case Granary _:         apply = Config.HoardedStoresGranaryEnable.Value;       mul = Config.HoardedStoresGranaryMul.Value;       return;
                case Storehouse _:      apply = Config.HoardedStoresStorehouseEnable.Value;    mul = Config.HoardedStoresStorehouseMul.Value;    return;
                case StorageDepot _:    apply = Config.HoardedStoresStorageDepotEnable.Value;  mul = Config.HoardedStoresStorageDepotMul.Value;  return;
                case Stockyard _:       apply = Config.HoardedStoresStockyardEnable.Value;     mul = Config.HoardedStoresStockyardMul.Value;     return;
                case Treasury _:        apply = Config.HoardedStoresTreasuryEnable.Value;      mul = Config.HoardedStoresTreasuryMul.Value;      return;
                case MarketBuilding _:  apply = Config.HoardedStoresMarketEnable.Value;        mul = Config.HoardedStoresMarketMul.Value;        return;
                default:                apply = false; mul = 1.0f; return;
            }
        }
    }
}
