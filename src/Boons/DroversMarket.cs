// Drover's Market — sell your livestock to visiting traders.
//
// Original SB feature. Vanilla FF SHOWS a sell price for livestock at the trading post but the
// player's count always reads 0: the sellable count is `tradingPost.traderStorage.GetItemCount(item)`
// (goods porters have STOCKED at the post), and live animals can never be stocked. The buy direction
// is special-cased (purchased livestock items → TryForHerdPlacement spawns herd animals); the sell
// direction simply was never wired. This boon wires it.
//
// Design (three small patches, vanilla does the heavy lifting):
//   1) UITradingPostBuySellPanel.Init prefix  — for livestock items, numSellable = live town herd
//      count (excluding animals already queued for slaughter).
//   2) UITradingPostItemEntry.Init prefix     — same substitution for the list row's global count.
//   3) TradingPost.SellItems prefix           — MATERIALIZE the sold animals as items into
//      traderStorage (and remove them from their herds), then let the ORIGINAL run untouched:
//      wagon transfer, gold pricing, GoldGeneratedEvent, trade stats, expense ledger all inherited.
//
// Removal policy: drain the fullest herd first; skip animals flagged orderedToBeSlaughtered
// (no conflict with the butcher queue); RemoveAnimalFromHerd + Destroy.
//
// Verified game API (decompile 2026-07-26):
//   - TradingPost.traderStorage (public ReservableItemStorage), SellItems(TradeWagon, Item, uint, bool)
//   - ResourceManager.barnsRO/stablesRO/goatBarnsRO/chickenCoopsRO/dogKennelsRO/catKennelsRO (public RO lists)
//   - ResourceManager.cowItemInfo/horseItemInfo/goatItemInfo/chickenItemInfo/dogItemInfo/catItemInfo (.item)
//   - LivestockBuilding.herd → Herd.animalsInHerdRO / RemoveAnimalFromHerd(LivestockAnimal) (public)
//   - LivestockAnimal.orderedToBeSlaughtered (public bool)

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace SovereignBoons.Boons
{
    internal static class DroversMarket
    {
        internal class LivestockType
        {
            public ItemID Id;
            public Func<ResourceManager, IEnumerable<LivestockBuilding>> Buildings;
            public Func<ResourceManager, Item> CanonicalItem;
        }

        private static readonly LivestockType[] Types =
        {
            new LivestockType { Id = ItemID.Cow,     Buildings = rm => rm.barnsRO.Cast<LivestockBuilding>(),        CanonicalItem = rm => rm.cowItemInfo.item },
            new LivestockType { Id = ItemID.Horse,   Buildings = rm => rm.stablesRO.Cast<LivestockBuilding>(),      CanonicalItem = rm => rm.horseItemInfo.item },
            new LivestockType { Id = ItemID.Goat,    Buildings = rm => rm.goatBarnsRO.Cast<LivestockBuilding>(),    CanonicalItem = rm => rm.goatItemInfo.item },
            new LivestockType { Id = ItemID.Chicken, Buildings = rm => rm.chickenCoopsRO.Cast<LivestockBuilding>(), CanonicalItem = rm => rm.chickenItemInfo.item },
            new LivestockType { Id = ItemID.Dog,     Buildings = rm => rm.dogKennelsRO.Cast<LivestockBuilding>(),   CanonicalItem = rm => rm.dogItemInfo.item },
            new LivestockType { Id = ItemID.Cat,     Buildings = rm => rm.catKennelsRO.Cast<LivestockBuilding>(),   CanonicalItem = rm => rm.catItemInfo.item },
        };

        private static LivestockType ForItem(Item item)
        {
            if (item == null) return null;
            foreach (var t in Types) if (t.Id == item.itemID) return t;
            return null;
        }

        private static ResourceManager RM => UnitySingleton<GameManager>.Instance?.resourceManager;

        // Live sellable animals of a type across the whole town (slaughter-queued animals excluded).
        internal static uint CountSellable(LivestockType t)
        {
            var rm = RM;
            if (rm == null) return 0;
            uint n = 0;
            try
            {
                foreach (var b in t.Buildings(rm))
                {
                    if (b == null || b.herd == null) continue;
                    foreach (var a in b.herd.animalsInHerdRO)
                        if (a != null && !a.orderedToBeSlaughtered) n++;
                }
            }
            catch (Exception ex) { Plugin.Log.Warning($"[Drover's Market] count failed: {ex.Message}"); }
            return n;
        }

        // Remove up to n animals (fullest herd first) and return how many were actually removed.
        internal static uint RemoveAnimals(LivestockType t, uint n)
        {
            var rm = RM;
            if (rm == null || n == 0) return 0;
            uint removed = 0;
            try
            {
                var buildings = t.Buildings(rm)
                    .Where(b => b != null && b.herd != null)
                    .OrderByDescending(b => b.herd.animalsInHerdRO.Count)
                    .ToList();
                foreach (var b in buildings)
                {
                    if (removed >= n) break;
                    // snapshot — we mutate the roster while iterating
                    var candidates = b.herd.animalsInHerdRO.Where(a => a != null && !a.orderedToBeSlaughtered).ToList();
                    foreach (var a in candidates)
                    {
                        if (removed >= n) break;
                        b.herd.RemoveAnimalFromHerd(a);
                        UnityEngine.Object.Destroy(a.gameObject);
                        removed++;
                    }
                }
            }
            catch (Exception ex) { Plugin.Log.Warning($"[Drover's Market] removal failed: {ex.Message}"); }
            return removed;
        }

        // ============================ (1) sell-slider count ============================
        [HarmonyPatch(typeof(UITradingPostBuySellPanel), "Init")]
        internal static class BuySellPanel_Init_Patch
        {
            private static void Prefix(Item _item, ref uint numSellable)
            {
                try
                {
                    if (!Config.EnableDroversMarket.Value) return;
                    var t = ForItem(_item);
                    if (t != null) numSellable = CountSellable(t);
                }
                catch { }
            }
        }

        // ============================ (2) list-row count ============================
        [HarmonyPatch(typeof(UITradingPostItemEntry), "Init")]
        internal static class ItemEntry_Init_Patch
        {
            private static void Prefix(Item _item, ref int globalItemCount)
            {
                try
                {
                    if (!Config.EnableDroversMarket.Value) return;
                    var t = ForItem(_item);
                    if (t != null) globalItemCount = (int)CountSellable(t);
                }
                catch { }
            }
        }

        // ============================ (3) the sale itself ============================
        // Materialize the animals as items into traderStorage, then the original SellItems finds
        // them and performs the entire vanilla sale (wagon goods, gold, events, expense tracking).
        [HarmonyPatch(typeof(TradingPost), "SellItems")]
        internal static class TradingPost_SellItems_Patch
        {
            private static void Prefix(TradingPost __instance, TradeWagon sellTo, Item item, uint numToSell)
            {
                try
                {
                    if (!Config.EnableDroversMarket.Value) return;
                    if (sellTo == null || item == null || numToSell == 0) return;
                    var t = ForItem(item);
                    if (t == null || __instance.traderStorage == null) return;

                    uint alreadyStocked = __instance.traderStorage.GetItemCount(item); // normally 0
                    if (alreadyStocked >= numToSell) return;

                    uint need = numToSell - alreadyStocked;
                    uint available = CountSellable(t);
                    uint take = Math.Min(need, available);
                    if (take == 0) return;

                    uint removed = RemoveAnimals(t, take);
                    if (removed == 0) return;

                    var rm = RM;
                    var canonical = rm != null ? t.CanonicalItem(rm) : null;
                    __instance.traderStorage.AddItems(new ItemBundle(canonical ?? item, removed, 100u));
                    Plugin.Log.Msg($"[Drover's Market] sold {removed} {item.itemID} (materialized for vanilla sale).");
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warning($"[Drover's Market] sell prefix failed: {ex.Message}");
                }
            }
        }
    }
}
