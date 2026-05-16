// Folded from VC_BuildingRadiusAdjust by VC (v1.2)
// Original DLL: VC_BuildingRadiusAdjust_FF.dll (Workshop) / VC_BuildingRadiusAdjust.dll (Nexus)
// Original prefs: 7 declared (Forage/RatCatcher/Hunter/Fishing/Arborist/Market/WorkCamp) but
//                 only 5 Awake patches actually shipped (WorkCamp/Hunter/Fishing/Arborist/Market).
// SB changes: WIRED THE MISSING TWO — Forager Shack and Rat Catcher have radius fields
//             in vanilla but the source mod never patched their Awake. Domain Expansion covers
//             7 buildings, matching the source's pref count with no orphan toggles.
//
// Verified targets (decompile_verification.md + ff_full.cs spot check):
//   - WorkCamp._workRadius              (60f) at ff_full.cs:363019
//   - HunterBuilding._huntingRadius     (60f) at ff_full.cs:342688
//   - FishingShack._fishingRadius       (30f) at ff_full.cs:339095
//   - ArboristBuilding._harvestRadius  (100f) at ff_full.cs:325231
//   - MarketBuilding._strategicPlanningRadius (50f, inherited from Building) at ff_full.cs:327188
//   - ForagerShack._foragingRadius      (60f) at ff_full.cs:340525   [SB extension]
//   - RatCatcherBuilding._workRadius    (60f) at ff_full.cs:349543   [SB extension]

using HarmonyLib;
using MelonLoader;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Percent-based radius multipliers for work-radius buildings. Each
    /// multiplier is independent so the player can buff Hunter without
    /// touching Fishing, etc.
    /// </summary>
    internal static class LongReach
    {
        private static readonly AccessTools.FieldRef<WorkCamp, float>? _workCampRef =
            AccessTools.FieldRefAccess<WorkCamp, float>("_workRadius");
        private static readonly AccessTools.FieldRef<HunterBuilding, float>? _hunterRef =
            AccessTools.FieldRefAccess<HunterBuilding, float>("_huntingRadius");
        private static readonly AccessTools.FieldRef<FishingShack, float>? _fishingRef =
            AccessTools.FieldRefAccess<FishingShack, float>("_fishingRadius");
        private static readonly AccessTools.FieldRef<ArboristBuilding, float>? _arboristRef =
            AccessTools.FieldRefAccess<ArboristBuilding, float>("_harvestRadius");
        // Market's _strategicPlanningRadius lives on the Building base class; AccessTools
        // walks the chain when the field is declared upstream.
        private static readonly AccessTools.FieldRef<MarketBuilding, float>? _marketRef =
            AccessTools.FieldRefAccess<MarketBuilding, float>("_strategicPlanningRadius");
        private static readonly AccessTools.FieldRef<ForagerShack, float>? _foragerRef =
            AccessTools.FieldRefAccess<ForagerShack, float>("_foragingRadius");
        private static readonly AccessTools.FieldRef<RatCatcherBuilding, float>? _ratCatcherRef =
            AccessTools.FieldRefAccess<RatCatcherBuilding, float>("_workRadius");

        private static float Scale(float baseRadius, float pct)
            => baseRadius * (1f + pct / 100f);

        [HarmonyPatch(typeof(WorkCamp), "Awake")]
        internal static class WorkCamp_Awake_Patch
        {
            private static void Postfix(WorkCamp __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_workCampRef == null) return;
                try { _workCampRef(__instance) = Scale(_workCampRef(__instance), Config.LongReachWorkCampPct.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] WorkCamp: {ex.Message}"); }
            }
        }

        [HarmonyPatch(typeof(HunterBuilding), "Awake")]
        internal static class HunterBuilding_Awake_Patch
        {
            private static void Postfix(HunterBuilding __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_hunterRef == null) return;
                try { _hunterRef(__instance) = Scale(_hunterRef(__instance), Config.LongReachHunterPct.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] Hunter: {ex.Message}"); }
            }
        }

        [HarmonyPatch(typeof(FishingShack), "Awake")]
        internal static class FishingShack_Awake_Patch
        {
            private static void Postfix(FishingShack __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_fishingRef == null) return;
                try { _fishingRef(__instance) = Scale(_fishingRef(__instance), Config.LongReachFishingPct.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] Fishing: {ex.Message}"); }
            }
        }

        [HarmonyPatch(typeof(ArboristBuilding), "Awake")]
        internal static class ArboristBuilding_Awake_Patch
        {
            private static void Postfix(ArboristBuilding __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_arboristRef == null) return;
                try { _arboristRef(__instance) = Scale(_arboristRef(__instance), Config.LongReachArboristPct.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] Arborist: {ex.Message}"); }
            }
        }

        [HarmonyPatch(typeof(MarketBuilding), "Awake")]
        internal static class MarketBuilding_Awake_Patch
        {
            private static void Postfix(MarketBuilding __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_marketRef == null) return;
                try { _marketRef(__instance) = Scale(_marketRef(__instance), Config.LongReachMarketPct.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] Market: {ex.Message}"); }
            }
        }

        [HarmonyPatch(typeof(ForagerShack), "Awake")]
        internal static class ForagerShack_Awake_Patch
        {
            private static void Postfix(ForagerShack __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_foragerRef == null) return;
                try { _foragerRef(__instance) = Scale(_foragerRef(__instance), Config.LongReachForagerPct.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] Forager: {ex.Message}"); }
            }
        }

        [HarmonyPatch(typeof(RatCatcherBuilding), "Awake")]
        internal static class RatCatcherBuilding_Awake_Patch
        {
            private static void Postfix(RatCatcherBuilding __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_ratCatcherRef == null) return;
                try { _ratCatcherRef(__instance) = Scale(_ratCatcherRef(__instance), Config.LongReachRatCatcherPct.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] RatCatcher: {ex.Message}"); }
            }
        }
    }
}
