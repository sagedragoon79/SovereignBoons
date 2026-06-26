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
    /// Per-building radius multipliers for work-radius buildings (1.0 = vanilla,
    /// 1.5 = 1.5× the radius). Each multiplier is independent so the player can
    /// buff Hunter without touching Fishing, etc.
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
        // Dog/Cat DLC — Doghouse guard radius (circleRadius => guardRadius, drives the
        // dog's defend area). Scale in Prefix so SelectionCircle.Init reads the new value.
        private static readonly AccessTools.FieldRef<Doghouse, float>? _doghouseRef =
            AccessTools.FieldRefAccess<Doghouse, float>("_guardRadius");

        // Straight multiplier: 1.0 = vanilla, 1.5 = 1.5× the radius, 2.0 = double.
        // Clamped to the documented 0.5..3.0 range so a hand-edited cfg typo
        // (e.g. 50 meant as "+50%") can't blow the radius out.
        private static float Scale(float baseRadius, float mul)
            => baseRadius * UnityEngine.Mathf.Clamp(mul, 0.5f, 3.0f);

        /// <summary>
        /// Config multiplier for a building TYPE (used by the placement-preview patch,
        /// which operates on the prefab Building component — the prefab never runs our
        /// Awake patch, so its circleRadius is still vanilla). Returns 1.0 for buildings
        /// Domain Expansion doesn't touch.
        /// </summary>
        private static float MulFor(Building b)
        {
            switch (b)
            {
                case WorkCamp _:           return Config.LongReachWorkCampMul.Value;
                case HunterBuilding _:     return Config.LongReachHunterMul.Value;
                case FishingShack _:       return Config.LongReachFishingMul.Value;
                case ArboristBuilding _:   return Config.LongReachArboristMul.Value;
                case MarketBuilding _:     return Config.LongReachMarketMul.Value;
                case ForagerShack _:       return Config.LongReachForagerMul.Value;
                case RatCatcherBuilding _: return Config.LongReachRatCatcherMul.Value;
                case Doghouse _:           return Config.LongReachDoghouseMul.Value;
                default:                   return 1f;
            }
        }

        [HarmonyPatch(typeof(WorkCamp), "Awake")]
        internal static class WorkCamp_Awake_Patch
        {
            private static void Prefix(WorkCamp __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_workCampRef == null) return;
                try { _workCampRef(__instance) = Scale(_workCampRef(__instance), Config.LongReachWorkCampMul.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] WorkCamp: {ex.Message}"); }
            }
        }

        [HarmonyPatch(typeof(HunterBuilding), "Awake")]
        internal static class HunterBuilding_Awake_Patch
        {
            private static void Prefix(HunterBuilding __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_hunterRef == null) return;
                try { _hunterRef(__instance) = Scale(_hunterRef(__instance), Config.LongReachHunterMul.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] Hunter: {ex.Message}"); }
            }
        }

        [HarmonyPatch(typeof(FishingShack), "Awake")]
        internal static class FishingShack_Awake_Patch
        {
            private static void Prefix(FishingShack __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_fishingRef == null) return;
                try { _fishingRef(__instance) = Scale(_fishingRef(__instance), Config.LongReachFishingMul.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] Fishing: {ex.Message}"); }
            }
        }

        [HarmonyPatch(typeof(ArboristBuilding), "Awake")]
        internal static class ArboristBuilding_Awake_Patch
        {
            private static void Prefix(ArboristBuilding __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_arboristRef == null) return;
                try { _arboristRef(__instance) = Scale(_arboristRef(__instance), Config.LongReachArboristMul.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] Arborist: {ex.Message}"); }
            }
        }

        [HarmonyPatch(typeof(MarketBuilding), "Awake")]
        internal static class MarketBuilding_Awake_Patch
        {
            private static void Prefix(MarketBuilding __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_marketRef == null) return;
                try { _marketRef(__instance) = Scale(_marketRef(__instance), Config.LongReachMarketMul.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] Market: {ex.Message}"); }
            }
        }

        [HarmonyPatch(typeof(ForagerShack), "Awake")]
        internal static class ForagerShack_Awake_Patch
        {
            private static void Prefix(ForagerShack __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_foragerRef == null) return;
                try { _foragerRef(__instance) = Scale(_foragerRef(__instance), Config.LongReachForagerMul.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] Forager: {ex.Message}"); }
            }
        }

        [HarmonyPatch(typeof(RatCatcherBuilding), "Awake")]
        internal static class RatCatcherBuilding_Awake_Patch
        {
            private static void Prefix(RatCatcherBuilding __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_ratCatcherRef == null) return;
                try { _ratCatcherRef(__instance) = Scale(_ratCatcherRef(__instance), Config.LongReachRatCatcherMul.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] RatCatcher: {ex.Message}"); }
            }
        }

        [HarmonyPatch(typeof(Doghouse), "Awake")]
        internal static class Doghouse_Awake_Patch
        {
            private static void Prefix(Doghouse __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (_doghouseRef == null) return;
                try { _doghouseRef(__instance) = Scale(_doghouseRef(__instance), Config.LongReachDoghouseMul.Value); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Domain Expansion] Doghouse: {ex.Message}"); }
            }
        }

        // ---------- Placement-preview circle ----------
        //
        // PlaceableBuilding.SetMeshFromPrefab reads `building = prefab.GetComponent<Building>()`
        // and draws the work-area ring from `building.circleRadius`. The prefab never runs our
        // Awake patch, so the placement ghost shows the VANILLA radius even though the built
        // result will be scaled. This postfix resizes the preview circle to match.

        private static readonly System.Reflection.FieldInfo? _pbBuildingField =
            AccessTools.Field(typeof(PlaceableBuilding), "_building");
        private static readonly System.Reflection.PropertyInfo? _pbSelectionCircleProp =
            AccessTools.Property(typeof(PlaceableBuilding), "selectionCircle");

        [HarmonyPatch(typeof(PlaceableBuilding), "SetMeshFromPrefab")]
        internal static class PlaceableBuilding_SetMeshFromPrefab_Patch
        {
            private static void Postfix(PlaceableBuilding __instance)
            {
                if (!Config.EnableLongReach.Value) return;
                if (Plugin.IsForeignModLoaded("VC_BuildingRadiusAdjust")) return;
                if (__instance == null) return;
                if (_pbBuildingField == null || _pbSelectionCircleProp == null) return;

                try
                {
                    var building = _pbBuildingField.GetValue(__instance) as Building;
                    if (building == null || !building.showCircleOnPlaceable) return;

                    float mul = MulFor(building);
                    if (mul == 1f) return; // not a Domain Expansion target (or set to vanilla)

                    var sc = _pbSelectionCircleProp.GetValue(__instance) as SelectionCircle;
                    if (sc == null) return;

                    // building.circleRadius is the prefab's vanilla base (Awake never ran on
                    // the prefab); scale it the same way Awake will scale the built instance.
                    sc.radius = Scale(building.circleRadius, mul);
                    sc.CreateEdgeObjects(); // rebuild the ring mesh at the new radius
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Domain Expansion] Placement preview resize failed: {ex.Message}");
                }
            }
        }
    }
}
