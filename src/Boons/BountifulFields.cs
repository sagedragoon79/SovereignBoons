// Folded from VC_ConfigurableCropFields by VC (v1.7)
// Original DLL: VC_ConfigurableCropFields_FF.dll
// Original prefs: per-crop CropFieldCfgData{Fertility, WeedSuppression, PlantingDays,
//                 MatureDays, Frost, Heat} for 12 crops + globals
//                 (Grids per farmer multiplier, Maintenance days, Maintenance weed suppression).
// SB changes:
//   - Replaced custom CropFieldCfgData struct with flat MelonPreferences entries
//     (avoids the Toml-converter dependency).
//   - Sentinel "no change" values match the source mod:
//       Fertility/WeedSup: 255 = no change (range -10..10)
//       PlantingDays:       -1 = no change (range 5..10)
//       MatureDays:         -1 = no change (range 25..150)
//       Frost/Heat:         -1 = no change (UI lerp 0..10 = mild..harsh)
//   - Per-crop Apply toggle so the user can turn ALL crop overrides off without
//     unsetting the master.
//
// Verified targets (decompile_verification.md):
//   - ObjectDataStore.GetAllDataRecords<VegetableFieldsRecord>() — present.
//   - VegetableFieldsRecord private fields all confirmed at ff_full.cs:83171-region:
//     _fertilityDepletionPerPlantingPercent (int), _daysOfPlanting (int),
//     _daysToMature (int), _weedLevelMultiplier (float), _percentDiesOnFrost (int),
//     _basePercentDiesOfHeatStress (int).
//   - AgricultureManager._gridsPerFarmer (int) and ._maintenanceLengthInDays (int)
//     accessed via FieldRefAccess.

using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Per-crop tuning of fertility loss, planting/maturity days, weed suppression,
    /// frost &amp; heat tolerance. Plus globals for farmer-grid coverage and field
    /// maintenance. Applied once per map load.
    /// </summary>
    internal static class BountifulFields
    {
        // 12 vanilla crop types — same set the source mod targets.
        internal static readonly string[] Crops = new[]
        {
            "Turnip", "Carrot", "Wheat", "Buckwheat", "Rye", "Bean",
            "Pea",    "Cabbage", "Leek", "Flax",      "Clover", "Hay",
        };

        public const int NoChangeFertility = 255;
        public const int NoChangePlantingDays = -1;
        public const int NoChangeMatureDays = -1;
        public const float NoChangeWeed = 999f;
        public const int NoChangeFrost = -1;
        public const int NoChangeHeat = -1;

        // Lerp bounds (taken from source mod's MinMaxValueValidator constants).
        // UI exposes 0..10 (mild..harsh); internal field stores the actual percent.
        private const float MinFrostLerp = 0f;
        private const float MaxFrostLerp = 100f;
        private const float MinHeatLerp  = 0f;
        private const float MaxHeatLerp  = 100f;

        // Per-crop entries — keyed by short crop name (e.g. "Wheat").
        internal sealed class CropEntries
        {
            public MelonPreferences_Entry<bool>  Apply          = null!;
            public MelonPreferences_Entry<int>   Fertility      = null!; // 255 = no change
            public MelonPreferences_Entry<int>   PlantingDays   = null!; // -1 = no change
            public MelonPreferences_Entry<int>   MatureDays     = null!; // -1 = no change
            public MelonPreferences_Entry<float> WeedLevel      = null!; // 999 = no change
            public MelonPreferences_Entry<int>   FrostTolerance = null!; // 0..10 lerp, -1 = no change
            public MelonPreferences_Entry<int>   HeatTolerance  = null!; // 0..10 lerp, -1 = no change
        }

        private static readonly Dictionary<string, CropEntries> _byCrop =
            new Dictionary<string, CropEntries>();

        public static void RegisterPrefs(MelonPreferences_Category cat)
        {
            foreach (var crop in Crops)
            {
                var e = new CropEntries
                {
                    Apply = cat.CreateEntry($"BountifulFields_{crop}_Apply", false,
                        display_name: $"{crop} — Apply Overrides",
                        description: $"Master switch for {crop} field tuning. " +
                                     "If false, vanilla values are used for this crop."),
                    Fertility = cat.CreateEntry($"BountifulFields_{crop}_Fertility", NoChangeFertility,
                        display_name: $"{crop} — Fertility Depletion %",
                        description: $"Fertility lost per planting cycle. Vanilla varies per crop. " +
                                     $"Range -10..10. Sentinel {NoChangeFertility} = no change."),
                    PlantingDays = cat.CreateEntry($"BountifulFields_{crop}_PlantingDays", NoChangePlantingDays,
                        display_name: $"{crop} — Planting Days",
                        description: "Days needed to plant the field. Range 5..10. -1 = no change."),
                    MatureDays = cat.CreateEntry($"BountifulFields_{crop}_MatureDays", NoChangeMatureDays,
                        display_name: $"{crop} — Maturity Days",
                        description: "Days to mature from planting to harvest. Range 25..150. -1 = no change."),
                    WeedLevel = cat.CreateEntry($"BountifulFields_{crop}_WeedLevel", NoChangeWeed,
                        display_name: $"{crop} — Weed Level Multiplier",
                        description: $"Weeds grow {{value}}× the vanilla rate. Range -10..10. " +
                                     $"Sentinel {NoChangeWeed} = no change."),
                    FrostTolerance = cat.CreateEntry($"BountifulFields_{crop}_Frost", NoChangeFrost,
                        display_name: $"{crop} — Frost Tolerance",
                        description: "0 (immune) – 10 (very vulnerable). -1 = no change."),
                    HeatTolerance = cat.CreateEntry($"BountifulFields_{crop}_Heat", NoChangeHeat,
                        display_name: $"{crop} — Heat Tolerance",
                        description: "0 (immune) – 10 (very vulnerable). -1 = no change."),
                };
                _byCrop[crop] = e;
            }
        }

        public sealed class CropPref
        {
            public string Crop = "";
            public CropEntries Entries = null!;
        }

        public static IEnumerable<CropPref> Iterate()
        {
            foreach (var crop in Crops)
                if (_byCrop.TryGetValue(crop, out var e))
                    yield return new CropPref { Crop = crop, Entries = e };
        }

        // Called from Plugin.OnSceneWasInitialized("Map"). Idempotent — the source mod
        // uses a "first-frame OnUpdate" pattern to defer until GameManager is alive;
        // we run on scene init which serves the same purpose.
        public static void Apply()
        {
            if (!Config.EnableBountifulFields.Value) return;
            if (Plugin.IsForeignModLoaded("VC_ConfigurableCropFields")) return;

            try
            {
                ApplyGlobals();
                ApplyPerCrop();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.Warning($"[Bountiful Fields] Apply failed: {ex.Message}");
            }
        }

        private static void ApplyGlobals()
        {
            var gm = UnitySingleton<GameManager>.Instance;
            var am = gm?.agricultureManager;
            if (am == null) return;

            float gridMul = Config.BountifulFieldsGridsPerFarmerMul.Value;
            if (gridMul != 1.0f)
            {
                var gridsRef = AccessTools.FieldRefAccess<AgricultureManager, int>("_gridsPerFarmer");
                if (gridsRef != null)
                {
                    int vanilla = gridsRef(am);
                    int boosted = UnityEngine.Mathf.RoundToInt(vanilla * gridMul);
                    if (boosted != vanilla)
                    {
                        gridsRef(am) = boosted;
                        Plugin.Log.Msg($"[Bountiful Fields] gridsPerFarmer: {vanilla} → {boosted} (×{gridMul}).");
                    }
                }
            }

            int maintDays = Config.BountifulFieldsMaintenanceDays.Value;
            if (maintDays != -1)
            {
                var maintRef = AccessTools.FieldRefAccess<AgricultureManager, int>("_maintenanceLengthInDays");
                if (maintRef != null)
                {
                    maintRef(am) = maintDays;
                    Plugin.Log.Msg($"[Bountiful Fields] maintenanceLengthInDays = {maintDays}.");
                }
            }
        }

        private static void ApplyPerCrop()
        {
            var records = ObjectDataStore.GetAllDataRecords<VegetableFieldsRecord>();
            if (records == null) return;

            // Map "Wheat" → "WheatField" (matches DataRecord.name pattern).
            foreach (var record in records)
            {
                if (record == null) continue;
                string recName = record.name;
                if (string.IsNullOrEmpty(recName) || !recName.Contains("Field")) continue;

                string crop = StripFieldSuffix(recName);
                if (!_byCrop.TryGetValue(crop, out var e)) continue;
                if (!e.Apply.Value) continue;

                ApplyIntField(record, "_fertilityDepletionPerPlantingPercent", e.Fertility.Value, NoChangeFertility);
                ApplyIntField(record, "_daysOfPlanting", e.PlantingDays.Value, NoChangePlantingDays);
                ApplyIntField(record, "_daysToMature", e.MatureDays.Value, NoChangeMatureDays);
                ApplyFloatField(record, "_weedLevelMultiplier", e.WeedLevel.Value, NoChangeWeed);

                if (e.FrostTolerance.Value != NoChangeFrost)
                    ApplyIntField(record, "_percentDiesOnFrost",
                        LerpToPercent(e.FrostTolerance.Value, MinFrostLerp, MaxFrostLerp), int.MinValue);
                if (e.HeatTolerance.Value != NoChangeHeat)
                    ApplyIntField(record, "_basePercentDiesOfHeatStress",
                        LerpToPercent(e.HeatTolerance.Value, MinHeatLerp, MaxHeatLerp), int.MinValue);
            }
        }

        private static string StripFieldSuffix(string s)
        {
            const string suffix = "Field";
            int idx = s.IndexOf(suffix, System.StringComparison.Ordinal);
            return idx > 0 ? s.Substring(0, idx) : s;
        }

        private static int LerpToPercent(int uiValue, float min, float max)
        {
            float t = UnityEngine.Mathf.Clamp01(uiValue / 10f);
            return UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Lerp(min, max, t));
        }

        private static void ApplyIntField(VegetableFieldsRecord record, string fieldName, int value, int sentinel)
        {
            if (value == sentinel) return;
            var fi = AccessTools.Field(typeof(VegetableFieldsRecord), fieldName);
            if (fi == null) return;
            fi.SetValue(record, value);
        }

        private static void ApplyFloatField(VegetableFieldsRecord record, string fieldName, float value, float sentinel)
        {
            if (UnityEngine.Mathf.Approximately(value, sentinel)) return;
            var fi = AccessTools.Field(typeof(VegetableFieldsRecord), fieldName);
            if (fi == null) return;
            fi.SetValue(record, value);
        }
    }
}
