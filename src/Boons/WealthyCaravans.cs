// Folded from TravelingMerchantPlus by coos (v1.1.1)
// Original DLL: TravelingMerchantPlusMono.dll
// Original prefs: numGoldMultiplier (def 5.0), goodsMultiplier (def 5.0), tradingPostMaxStock (def 2000)
// SB changes:
//   - Default multipliers tamed from 5.0 → 2.0 (5x makes economy trivial; user can tune up).
//   - Default trading post cap from 2000 → 1000 (still well over vanilla 100).
//   - "Buy Anything" exposed as a SEPARATE toggle so players can buff gold/goods
//     without unlocking the unconditional merchant-buys-anything behavior.
//
// Verified targets (decompile_verification.md):
//   - TradeWagon.Init(bool includeMonumentItem) at ff_full.cs:357888
//   - TradeWagon.IsBuyingItem(Item) at ff_full.cs:358244
//   - TradeManager.maxTradingPostStockCount (public getter, backing _maxTradingPostStockCount=100)
//     at ff_full.cs:222261. Catalog had it as "MaxTradingPostStockCount" — actual is camelCase.
//   - TradeWagon.numGold (uint), TradeWagon.storage, TradeWagon.merchantDefinition,
//     TradeWagon.itemsWillingToBuy (private List<string>) — all verified.

using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Beefier traveling merchants: more gold, more goods, expanded willing-to-buy
    /// list, and a higher trading-post stock cap.
    /// </summary>
    internal static class WealthyCaravans
    {
        private static readonly AccessTools.FieldRef<TradeWagon, List<string>>? _willingToBuyRef =
            AccessTools.FieldRefAccess<TradeWagon, List<string>>("itemsWillingToBuy");

        // ---------- TradeWagon.Init postfix: buff gold + goods ----------

        [HarmonyPatch(typeof(TradeWagon), "Init")]
        internal static class TradeWagon_Init_Patch
        {
            private static void Postfix(TradeWagon __instance, bool includeMonumentItem)
            {
                if (!Config.EnableWealthyCaravans.Value) return;
                if (Plugin.IsForeignModLoaded("TravelingMerchantPlusMono")) return;
                if (__instance == null || __instance.storage == null) return;

                try
                {
                    BuffGold(__instance);
                    ExpandWillingToBuy(__instance);
                    BuffExistingGoods(__instance);
                    AddMissingGoods(__instance);
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Wealthy Caravans] TradeWagon.Init postfix failed: {ex.Message}");
                }
            }

            private static void BuffGold(TradeWagon w)
            {
                float mul = Config.WealthyCaravansGoldMul.Value;
                if (mul <= 1f) return;
                uint numGold = w.numGold;
                if (numGold == 0) return;
                uint boosted = (uint)(numGold * mul);
                if (boosted <= numGold) return;
                uint diff = boosted - numGold;
                w.storage.AddItems(new ItemBundle(new ItemGoldIngot(), diff, 100u));
            }

            private static void ExpandWillingToBuy(TradeWagon w)
            {
                if (_willingToBuyRef == null) return;
                var list = _willingToBuyRef(w);
                if (list == null) return;
                if (w.merchantDefinition?.goodsList != null)
                {
                    foreach (var g in w.merchantDefinition.goodsList)
                    {
                        if (!string.IsNullOrEmpty(g.itemName) && !list.Contains(g.itemName))
                            list.Add(g.itemName);
                    }
                }
                foreach (var bundle in w.storage.GetCopyOfAllItems())
                {
                    if (bundle != null && !string.IsNullOrEmpty(bundle.name) && !list.Contains(bundle.name))
                        list.Add(bundle.name);
                }
            }

            private static void BuffExistingGoods(TradeWagon w)
            {
                float mul = Config.WealthyCaravansGoodsMul.Value;
                if (mul <= 1f) return;
                string goldName = new ItemGoldIngot().name;
                foreach (var bundle in w.storage.GetCopyOfAllItems())
                {
                    if (bundle == null) continue;
                    if (bundle.name == goldName) continue; // gold already buffed above
                    uint count = bundle.numberOfItems;
                    uint boosted = (uint)(count * mul);
                    if (boosted <= count) continue;
                    uint diff = boosted - count;
                    w.storage.AddItems(new ItemBundle(new Item(bundle.name), diff, 100u));
                }
            }

            private static void AddMissingGoods(TradeWagon w)
            {
                if (_willingToBuyRef == null) return;
                var list = _willingToBuyRef(w);
                if (list == null) return;
                if (w.merchantDefinition?.goodsList == null) return;

                string goldName = new ItemGoldIngot().name;
                float mul = Mathf.Max(1f, Config.WealthyCaravansGoodsMul.Value);

                // Re-expand from merchantDefinition (source mod does this twice — keeps parity).
                foreach (var g in w.merchantDefinition.goodsList)
                {
                    if (!string.IsNullOrEmpty(g.itemName) && !list.Contains(g.itemName))
                        list.Add(g.itemName);
                }

                foreach (var itemName in list)
                {
                    if (string.IsNullOrEmpty(itemName) || itemName == goldName) continue;
                    var item = new Item(itemName);
                    if (w.storage.GetItemCount(item) != 0) continue;

                    uint count = 1u;
                    var match = w.merchantDefinition.goodsList.Find(x => x.itemName == itemName);
                    if (match != null)
                    {
                        int min = Mathf.Max(1, match.minItems);
                        int max = Mathf.Max(min, match.maxItems);
                        count = (uint)CERandom.Range(CERandom.Type.GAMEPLAY, min, max);
                    }
                    if (mul > 1f) count = (uint)(count * mul);

                    w.storage.AddItems(new ItemBundle(item, count, 100u));
                }
            }
        }

        // ---------- TradeWagon.IsBuyingItem prefix: buy anything in goodsList OR storage ----------

        [HarmonyPatch(typeof(TradeWagon), "IsBuyingItem")]
        internal static class TradeWagon_IsBuyingItem_Patch
        {
            private static bool Prefix(TradeWagon __instance, Item item, ref bool __result)
            {
                if (!Config.EnableWealthyCaravans.Value) return true;
                if (!Config.WealthyCaravansBuyAnything.Value) return true;
                if (Plugin.IsForeignModLoaded("TravelingMerchantPlusMono")) return true;
                if (__instance == null || item == null) return true;

                try
                {
                    string name = item.name;
                    if (__instance.merchantDefinition?.goodsList != null)
                    {
                        foreach (var g in __instance.merchantDefinition.goodsList)
                        {
                            if (g.itemName == name) { __result = true; return false; }
                        }
                    }
                    if (__instance.storage != null && __instance.storage.GetItemCount(item) != 0)
                    {
                        __result = true; return false;
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Wealthy Caravans] IsBuyingItem prefix failed: {ex.Message}");
                }
                return true; // fall through to vanilla
            }
        }

        // ---------- TradeManager.maxTradingPostStockCount getter postfix ----------

        [HarmonyPatch(typeof(TradeManager), "maxTradingPostStockCount", MethodType.Getter)]
        internal static class TradeManager_MaxTradingPostStockCount_Patch
        {
            private static void Postfix(ref int __result)
            {
                if (!Config.EnableWealthyCaravans.Value) return;
                if (Plugin.IsForeignModLoaded("TravelingMerchantPlusMono")) return;
                __result = Config.WealthyCaravansMaxStock.Value;
            }
        }
    }
}
