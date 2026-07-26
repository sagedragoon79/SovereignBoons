// Merchant's Gambit — trade-arbitrage calculator + one-click stairstep executor.
//
// Original SB feature (not a fold). Power-spike / economy tool.
//
// What it does: on the hotkey, it scans every TradingPost's visiting merchants, prices
// every tradeable item on every wagon, finds profitable "buy item I cheap from wagon A,
// sell I dear to wagon B" arbitrage, simulates the greedy stairstep (the compounding
// buy-5/sell-5 → buy-7/sell-7 loop), and — unless Dry Run is on — executes it by driving
// the game's own TradingPost.BuyItems / SellItems.
//
// Verified game API (Assembly-CSharp):
//   - GameManager.resourceManager.tradingPostsRO : ReadOnlyCollection<TradingPost>
//   - TradingPost.visitingTraders : protected List<TradeWagon>   (reflected)
//   - TradingPost.numGold (uint), .BuyItems(TradeWagon, Item, uint, bool destock, bool buyAll) : bool,
//                 .SellItems(TradeWagon, Item, uint, bool destock)
//   - TradeWagon.GetItemPrice(Item) : float, .numGold (uint), .IsBuyingItem(Item) : bool,
//                .storage.GetCopyOfAllItems() : List<ItemBundle>
//   - TradeManager.GetItemPriceModifier(Item, bool isSelling) : float
//     BUY price/unit  = Ceil(GetItemPrice(I) * GetItemPriceModifier(I, isSelling:false))  [×~1.8 spread]
//     SELL price/unit = Ceil(GetItemPrice(I) * GetItemPriceModifier(I, isSelling:true))
//   - WorkBucketManager.itemByItemIDRO : IReadOnlyDictionary<ItemID, Item>  (canonical Item instances,
//     so GetItemPrice's per-wagon cache — incl. requested-item premium — is hit correctly)
//
// Prices are FLAT per-unit (not volume-dependent), so a single (item, buyWagon, sellWagon)
// target's max quantity is min(A's stock, B's gold / sellPrice) as long as there's enough
// starting gold to prime one unit — the compounding lets gold grow to cover the rest.

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SovereignBoons.Boons
{
    internal static class MerchantsGambit
    {
        // ---------- hotkey (minimal chord) ----------
        private struct Chord { public KeyCode Key; public bool Ctrl, Alt, Shift; }
        private static readonly Chord _default = new Chord { Key = KeyCode.B, Ctrl = true };
        private static Chord _scanChord = _default;
        private static string _lastKeyStr = "";

        private static readonly FieldInfo? _visitingTradersField =
            AccessTools.Field(typeof(TradingPost), "visitingTraders");

        public static void OnUpdate()
        {
            if (!Config.EnableMerchantsGambit.Value) return;
            if (!GameManager.gameReadyToPlay) return;

            // Re-parse only when the config string changes (cheap; supports live rebinds).
            if (_lastKeyStr != Config.MerchantsGambitScanKey.Value)
            {
                _lastKeyStr = Config.MerchantsGambitScanKey.Value;
                _scanChord = ParseChord(_lastKeyStr, _default);
            }

            if (ChordPressed(_scanChord)) Run();
        }

        private static Chord ParseChord(string raw, Chord fallback)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            var c = new Chord();
            bool gotKey = false;
            foreach (var tok in raw.Split('+'))
            {
                var t = tok.Trim();
                if (t.Length == 0) continue;
                switch (t.ToLowerInvariant())
                {
                    case "ctrl": case "control": c.Ctrl = true; break;
                    case "alt": c.Alt = true; break;
                    case "shift": c.Shift = true; break;
                    default: if (Enum.TryParse<KeyCode>(t, ignoreCase: true, out var k)) { c.Key = k; gotKey = true; } break;
                }
            }
            return gotKey ? c : fallback;
        }

        private static bool ChordPressed(Chord c)
        {
            if (!Input.GetKeyDown(c.Key)) return false;
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            return ctrl == c.Ctrl && alt == c.Alt && shift == c.Shift;
        }

        // ---------- plan model ----------
        private sealed class Target
        {
            public Item Item = null!;
            public TradeWagon Buy = null!;
            public TradeWagon Sell = null!;
            public int BuyPrice;
            public int SellPrice;
            public long Qty;
            public int ProfitPerUnit => SellPrice - BuyPrice;
            public long TotalProfit => Qty * ProfitPerUnit;
        }

        // ---------- entry ----------
        private static void Run()
        {
            try
            {
                var gm = UnitySingleton<GameManager>.Instance;
                var rm = gm?.resourceManager;
                var tm = gm?.tradeManager;
                var wbm = gm?.workBucketManager;
                if (rm == null || tm == null || wbm == null) return;

                bool dry = Config.MerchantsGambitDryRun.Value;
                int minProfit = Math.Max(1, Config.MerchantsGambitMinProfit.Value);

                long grandProfit = 0;
                int postsActed = 0, tradeCount = 0;
                bool anyBlocked = false;
                string blockReason = "";

                foreach (var post in rm.tradingPostsRO)
                {
                    if (post == null) continue;
                    var wagons = GetWagons(post);
                    if (wagons == null || wagons.Count == 0) continue;

                    var plan = BuildPlan(post, wagons, tm, wbm, minProfit);
                    if (plan.Count == 0)
                    {
                        // Nothing actionable here — but does a profitable spread exist that's
                        // just blocked by gold? Surface it so the player knows it's working and
                        // that funding the deal would pay off.
                        if (HasBlockedOpportunity(post, wagons, tm, wbm, minProfit, out var r))
                        {
                            anyBlocked = true;
                            if (blockReason.Length == 0) blockReason = r;
                        }
                        continue;
                    }

                    long postProfit = 0;
                    foreach (var t in plan) postProfit += t.TotalProfit;
                    if (postProfit <= 0) continue;

                    postsActed++;
                    grandProfit += postProfit;
                    tradeCount += plan.Count;

                    foreach (var t in plan)
                        Plugin.Log.Msg($"[Merchant's Gambit] {(dry ? "(dry) " : "")}buy {t.Qty} {t.Item.name} @{t.BuyPrice} → sell @{t.SellPrice}  (+{t.TotalProfit}g)");

                    if (!dry) Execute(post, plan);
                }

                string msg;
                if (postsActed > 0)
                    msg = $"Merchant's Gambit: {(dry ? "(dry run) " : "")}+{grandProfit:n0}g across {tradeCount} trade(s), {postsActed} post(s)";
                else if (anyBlocked)
                    msg = $"Merchant's Gambit: arbitrage opportunity exists — {blockReason}";
                else
                    msg = "Merchant's Gambit: no arbitrage spreads right now";
                Toast.Show(msg);
                Plugin.Log.Msg("[Merchant's Gambit] " + msg);
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning($"[Merchant's Gambit] Run failed: {ex.Message}");
            }
        }

        // ---------- pricing (mirrors TradingPost.BuyItems / SellItems exactly) ----------
        private static int BuyPrice(TradeWagon w, Item i, TradeManager tm)
            => Mathf.CeilToInt(w.GetItemPrice(i) * tm.GetItemPriceModifier(i, isSelling: false));
        private static int SellPrice(TradeWagon w, Item i, TradeManager tm)
            => Mathf.CeilToInt(w.GetItemPrice(i) * tm.GetItemPriceModifier(i, isSelling: true));

        // ---------- greedy planner over a snapshot ----------
        private static List<Target> BuildPlan(TradingPost post, List<TradeWagon> wagons, TradeManager tm, WorkBucketManager wbm, int minProfit)
        {
            var plan = new List<Target>();

            long postGold = post.numGold;
            var wagonGold = new Dictionary<TradeWagon, long>();
            var stock = new Dictionary<TradeWagon, Dictionary<ItemID, long>>();
            foreach (var w in wagons)
            {
                if (w == null) continue;
                wagonGold[w] = w.numGold;
                var d = new Dictionary<ItemID, long>();
                var bundles = w.storage != null ? w.storage.GetCopyOfAllItems() : null;
                if (bundles != null)
                    foreach (var b in bundles)
                    {
                        if (b == null || b.itemID == ItemID.GoldIngot) continue;
                        d[b.itemID] = (d.TryGetValue(b.itemID, out var c) ? c : 0) + b.numberOfItems;
                    }
                stock[w] = d;
            }

            int guard = 0;
            while (guard++ < 256)
            {
                Target? best = null;
                foreach (var A in wagons)
                {
                    if (A == null || !stock.TryGetValue(A, out var aStock)) continue;
                    foreach (var kv in aStock)
                    {
                        if (kv.Value <= 0) continue;
                        if (!wbm.itemByItemIDRO.TryGetValue(kv.Key, out var item) || item == null) continue;
                        int bp = BuyPrice(A, item, tm);
                        if (bp <= 0) continue;
                        if (postGold < bp) continue; // not enough gold to prime even one unit

                        foreach (var B in wagons)
                        {
                            if (B == null || B == A) continue;
                            if (!B.IsBuyingItem(item)) continue;
                            int sp = SellPrice(B, item, tm);
                            int profit = sp - bp;
                            if (profit < minProfit) continue;
                            if (wagonGold[B] < sp) continue;

                            long qty = Math.Min(kv.Value, wagonGold[B] / sp);
                            if (qty <= 0) continue;

                            long total = qty * profit;
                            if (best == null || profit > best.ProfitPerUnit ||
                                (profit == best.ProfitPerUnit && total > best.TotalProfit))
                            {
                                best = new Target { Item = item, Buy = A, Sell = B, BuyPrice = bp, SellPrice = sp, Qty = qty };
                            }
                        }
                    }
                }

                if (best == null) break;

                // Apply to snapshot: gold compounds, B pays out, A receives, A's stock drains.
                postGold += best.TotalProfit;
                wagonGold[best.Sell] -= best.Qty * best.SellPrice;
                wagonGold[best.Buy] += best.Qty * best.BuyPrice;
                stock[best.Buy][best.Item.itemID] -= best.Qty;
                plan.Add(best);
            }

            return plan;
        }

        // ---------- executor: real stairstep via the game's own trade methods ----------
        private static void Execute(TradingPost post, List<Target> plan)
        {
            int maxCycles = Math.Max(10, Config.MerchantsGambitMaxCycles.Value);
            int cycles = 0;
            foreach (var t in plan)
            {
                long remaining = t.Qty;
                while (remaining > 0 && cycles < maxCycles)
                {
                    long postGold = post.numGold;
                    long affordable = t.BuyPrice > 0 ? postGold / t.BuyPrice : 0;
                    long batch = Math.Min(remaining, affordable);
                    if (batch <= 0) break; // out of priming gold (shouldn't happen for a valid plan)

                    bool bought = post.BuyItems(t.Buy, t.Item, (uint)batch, false, false);
                    if (!bought) break;
                    post.SellItems(t.Sell, t.Item, (uint)batch, false);
                    remaining -= batch;
                    cycles++;
                }
            }
            if (cycles >= maxCycles)
                Plugin.Log.Warning($"[Merchant's Gambit] Hit max-cycle cap ({maxCycles}); some planned trades may be incomplete. Raise Max Cycles if intended.");
        }

        // Lightweight "does a profitable spread exist that we just can't fund right now?"
        // Runs only when the executable plan is empty. Reports the dominant blocker.
        private static bool HasBlockedOpportunity(TradingPost post, List<TradeWagon> wagons, TradeManager tm, WorkBucketManager wbm, int minProfit, out string reason)
        {
            reason = "";
            long postGold = post.numGold;
            bool needPlayerGold = false, needMerchantGold = false;

            foreach (var A in wagons)
            {
                if (A?.storage == null) continue;
                var bundles = A.storage.GetCopyOfAllItems();
                if (bundles == null) continue;
                foreach (var b in bundles)
                {
                    if (b == null || b.itemID == ItemID.GoldIngot || b.numberOfItems == 0) continue;
                    if (!wbm.itemByItemIDRO.TryGetValue(b.itemID, out var item) || item == null) continue;
                    int bp = BuyPrice(A, item, tm);
                    if (bp <= 0) continue;
                    foreach (var B in wagons)
                    {
                        if (B == null || B == A || !B.IsBuyingItem(item)) continue;
                        int sp = SellPrice(B, item, tm);
                        if (sp - bp < minProfit) continue;   // no profitable spread here
                        // A profitable spread exists — why can't we act on it?
                        if (postGold < bp) needPlayerGold = true;
                        if (B.numGold < sp) needMerchantGold = true;
                    }
                }
            }

            if (needPlayerGold) { reason = "you lack the gold to prime the deal"; return true; }
            if (needMerchantGold) { reason = "the merchant is low on gold — sell them goods to fund it"; return true; }
            return false;
        }

        private static List<TradeWagon>? GetWagons(TradingPost post)
        {
            try
            {
                if (_visitingTradersField?.GetValue(post) is List<TradeWagon> list) return list;
            }
            catch { /* reflection best-effort */ }
            return null;
        }
    }
}
