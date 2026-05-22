// Folded from VC_ConfigurableCropFields by VC (v1.7)
// Original DLL: VC_ConfigurableCropFields_FF.dll
// SB design (v0.6+):
//   - VANILLA values per crop are the defaults — no -1 sentinel. Players see
//     "here's what vanilla is" and edit from there. Per-crop Apply toggle gates
//     whether SB writes the cfg values back to VegetableFieldsRecord on map load.
//   - Frost/Heat are raw percent fields (0..100) — matches the in-game data and
//     the diagnostic log dump. Source mod's 0..10 lerp is gone.
//   - No "no change" sentinel anywhere. If you don't want to override a crop,
//     leave Apply=false. If you do, every value is real and bounded.
//   - Range guards stay (skip absurd values) for safety but no longer the
//     primary "skip this knob" mechanism.
//
// Verified targets (decompile_verification.md):
//   - ObjectDataStore.GetAllDataRecords<VegetableFieldsRecord>() — present.
//   - VegetableFieldsRecord private fields confirmed at ff_full.cs:83171-region.
//   - AgricultureManager._gridsPerFarmer (int) and ._maintenanceLengthInDays (int)
//     accessed via FieldRefAccess.

using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Per-crop tuning of fertility loss, planting/maturity days, weed injection,
    /// frost &amp; heat die-percent. Plus globals for farmer-grid coverage and
    /// field maintenance. Applied once per map load.
    /// </summary>
    internal static class BountifulFields
    {
        /// <summary>Vanilla values per crop, captured from a live FF map dump 2026-05-14.</summary>
        internal sealed class CropVanilla
        {
            public int Fertility;     // _fertilityDepletionPerPlantingPercent (negative = restore)
            public int PlantingDays;  // _daysOfPlanting (all crops = 10 in vanilla)
            public int MatureDays;    // _daysToMature
            public float WeedLevel;   // _weedLevelMultiplier (negative = removes weeds per cycle)
            public int FrostDiePct;   // _percentDiesOnFrost (0..100)
            public int HeatDiePct;    // _basePercentDiesOfHeatStress (0..100)
        }

        // Vanilla snapshot — exact values from in-game dump. Used as cfg defaults.
        internal static readonly Dictionary<string, CropVanilla> Vanilla =
            new Dictionary<string, CropVanilla>
            {
                ["Turnip"]    = new CropVanilla { Fertility = 3,  PlantingDays = 10, MatureDays = 41,  WeedLevel = -5f,  FrostDiePct = 0,  HeatDiePct = 35 },
                ["Carrot"]    = new CropVanilla { Fertility = 3,  PlantingDays = 10, MatureDays = 58,  WeedLevel =  0f,  FrostDiePct = 0,  HeatDiePct = 50 },
                ["Wheat"]     = new CropVanilla { Fertility = 8,  PlantingDays = 10, MatureDays = 118, WeedLevel = -4f,  FrostDiePct = 20, HeatDiePct = 0  },
                ["Buckwheat"] = new CropVanilla { Fertility = 1,  PlantingDays = 10, MatureDays = 60,  WeedLevel = -10f, FrostDiePct = 80, HeatDiePct = 0  },
                ["Rye"]       = new CropVanilla { Fertility = 6,  PlantingDays = 10, MatureDays = 118, WeedLevel = -6f,  FrostDiePct = 0,  HeatDiePct = 0  },
                ["Bean"]      = new CropVanilla { Fertility = -1, PlantingDays = 10, MatureDays = 85,  WeedLevel = -2f,  FrostDiePct = 80, HeatDiePct = 0  },
                ["Pea"]       = new CropVanilla { Fertility = -1, PlantingDays = 10, MatureDays = 51,  WeedLevel =  0f,  FrostDiePct = 0,  HeatDiePct = 75 },
                ["Cabbage"]   = new CropVanilla { Fertility = 4,  PlantingDays = 10, MatureDays = 85,  WeedLevel = -3f,  FrostDiePct = 0,  HeatDiePct = 0  },
                ["Leek"]      = new CropVanilla { Fertility = 6,  PlantingDays = 10, MatureDays = 118, WeedLevel =  0f,  FrostDiePct = 0,  HeatDiePct = 0  },
                ["Flax"]      = new CropVanilla { Fertility = 5,  PlantingDays = 10, MatureDays = 95,  WeedLevel =  0f,  FrostDiePct = 0,  HeatDiePct = 0  },
                ["Clover"]    = new CropVanilla { Fertility = -3, PlantingDays = 10, MatureDays = 64,  WeedLevel = -8f,  FrostDiePct = 0,  HeatDiePct = 0  },
                ["Hay"]       = new CropVanilla { Fertility = 1,  PlantingDays = 10, MatureDays = 128, WeedLevel = -5f,  FrostDiePct = 0,  HeatDiePct = 0  },
            };

        internal static readonly string[] Crops = new[]
        {
            "Turnip", "Carrot", "Wheat", "Buckwheat", "Rye", "Bean",
            "Pea",    "Cabbage", "Leek", "Flax",      "Clover", "Hay",
        };

        // Per-crop entries — keyed by short crop name (e.g. "Wheat").
        internal sealed class CropEntries
        {
            public MelonPreferences_Entry<bool>  Apply         = null!;
            public MelonPreferences_Entry<int>   Fertility     = null!;
            public MelonPreferences_Entry<int>   PlantingDays  = null!;
            public MelonPreferences_Entry<int>   MatureDays    = null!;
            public MelonPreferences_Entry<float> WeedLevel     = null!;
            public MelonPreferences_Entry<int>   FrostDiePct   = null!;
            public MelonPreferences_Entry<int>   HeatDiePct    = null!;
        }

        private static readonly Dictionary<string, CropEntries> _byCrop =
            new Dictionary<string, CropEntries>();

        // Captured vanilla snapshot per VegetableFieldsRecord. Populated on first
        // Apply() before any writes so the LogVanilla dump shows real vanilla even
        // after overrides have been applied.
        private sealed class VanillaSnapshot
        {
            public int Fertility;
            public int PlantingDays;
            public int MatureDays;
            public float WeedLevel;
            public int PercentDiesOnFrost;
            public int BasePercentDiesOfHeatStress;
        }
        private static readonly Dictionary<string, VanillaSnapshot> _vanillaByRecord =
            new Dictionary<string, VanillaSnapshot>();
        private static bool _vanillaLogged;
        private static bool _diseasesLogged;

        public static void RegisterPrefs(MelonPreferences_Category cat)
        {
            foreach (var crop in Crops)
            {
                var v = Vanilla[crop];
                var e = new CropEntries
                {
                    Apply = cat.CreateEntry($"BountifulFields_{crop}_Apply", false,
                        display_name: $"{crop} — Apply Overrides",
                        description: $"Master switch for {crop} field tuning. " +
                                     "OFF (default) → vanilla values are used (knobs below are ignored). " +
                                     "ON → SB writes the values below to the crop's data record on map load. " +
                                     "Default: false."),

                    Fertility = cat.CreateEntry($"BountifulFields_{crop}_Fertility", v.Fertility,
                        display_name: $"{crop} — Fertility Depletion",
                        description: $"Fertility points consumed per planting cycle. Negative values RESTORE " +
                                     $"fertility. Lower = field stays fertile longer (power-spike). " +
                                     $"Range: -10..10. Vanilla / Default for {crop}: {v.Fertility}."),

                    PlantingDays = cat.CreateEntry($"BountifulFields_{crop}_PlantingDays", v.PlantingDays,
                        display_name: $"{crop} — Planting Days",
                        description: $"Days needed to plant the field before growth begins. Lower = faster " +
                                     $"turnaround (power-spike). Range: 1..30. Vanilla / Default for {crop}: " +
                                     $"{v.PlantingDays} (vanilla = 10 for every crop)."),

                    MatureDays = cat.CreateEntry($"BountifulFields_{crop}_MatureDays", v.MatureDays,
                        display_name: $"{crop} — Maturity Days",
                        description: $"Days from planted to ready-to-harvest. Lower = faster crop (power-spike). " +
                                     $"Range: 10..300. Vanilla / Default for {crop}: {v.MatureDays}."),

                    WeedLevel = cat.CreateEntry($"BountifulFields_{crop}_WeedLevel", v.WeedLevel,
                        display_name: $"{crop} — Weed Injection",
                        description: $"Percent weed level ADDED each planting/harvest cycle (game applies as " +
                                     $"`weedLevel += value / 100`). NEGATIVE = the crop REMOVES weeds (most " +
                                     $"vanilla crops do). Lower = fewer weeds (power-spike). " +
                                     $"Range: -20..10. Vanilla / Default for {crop}: {v.WeedLevel:F1}."),

                    FrostDiePct = cat.CreateEntry($"BountifulFields_{crop}_FrostDiePct", v.FrostDiePct,
                        display_name: $"{crop} — Frost Die %",
                        description: $"Percent of crop that dies in a frost event. Lower = more frost-resistant " +
                                     $"(power-spike). 0 = immune. Range: 0..100. " +
                                     $"Vanilla / Default for {crop}: {v.FrostDiePct}."),

                    HeatDiePct = cat.CreateEntry($"BountifulFields_{crop}_HeatDiePct", v.HeatDiePct,
                        display_name: $"{crop} — Heat Die %",
                        description: $"Percent of crop that dies in a heatwave event. Lower = more heat-resistant " +
                                     $"(power-spike). 0 = immune. Range: 0..100. " +
                                     $"Vanilla / Default for {crop}: {v.HeatDiePct}."),
                };
                _byCrop[crop] = e;
            }
        }

        public sealed class CropPref
        {
            public string Crop = "";
            public CropEntries Entries = null!;
            public CropVanilla Vanilla = null!;
        }

        public static IEnumerable<CropPref> Iterate()
        {
            foreach (var crop in Crops)
                if (_byCrop.TryGetValue(crop, out var e))
                    yield return new CropPref { Crop = crop, Entries = e, Vanilla = BountifulFields.Vanilla[crop] };
        }

        // Called from Plugin.OnSceneWasInitialized("Map").
        public static void Apply()
        {
            if (!Config.EnableBountifulFields.Value) return;
            if (Plugin.IsForeignModLoaded("VC_ConfigurableCropFields")) return;

            try
            {
                ApplyGlobals();
                CaptureVanillaIfNeeded();
                MaybeLogVanilla();
                MaybeLogDiseases();
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
            if (maintDays >= 45 && maintDays <= 90)
            {
                var maintRef = AccessTools.FieldRefAccess<AgricultureManager, int>("_maintenanceLengthInDays");
                if (maintRef != null)
                {
                    maintRef(am) = maintDays;
                    Plugin.Log.Msg($"[Bountiful Fields] maintenanceLengthInDays = {maintDays}.");
                }
            }
        }

        private static void CaptureVanillaIfNeeded()
        {
            if (_vanillaByRecord.Count > 0) return;

            var records = ObjectDataStore.GetAllDataRecords<VegetableFieldsRecord>();
            if (records == null) return;

            foreach (var record in records)
            {
                if (record == null) continue;
                string name = record.name;
                if (string.IsNullOrEmpty(name) || !name.Contains("Field")) continue;
                if (_vanillaByRecord.ContainsKey(name)) continue;

                _vanillaByRecord[name] = new VanillaSnapshot
                {
                    Fertility                   = ReadInt(record, "_fertilityDepletionPerPlantingPercent"),
                    PlantingDays                = ReadInt(record, "_daysOfPlanting"),
                    MatureDays                  = ReadInt(record, "_daysToMature"),
                    WeedLevel                   = ReadFloat(record, "_weedLevelMultiplier"),
                    PercentDiesOnFrost          = ReadInt(record, "_percentDiesOnFrost"),
                    BasePercentDiesOfHeatStress = ReadInt(record, "_basePercentDiesOfHeatStress"),
                };
            }
        }

        private static int ReadInt(VegetableFieldsRecord r, string field)
        {
            var fi = AccessTools.Field(typeof(VegetableFieldsRecord), field);
            if (fi == null) return 0;
            var v = fi.GetValue(r);
            return v is int i ? i : 0;
        }

        private static float ReadFloat(VegetableFieldsRecord r, string field)
        {
            var fi = AccessTools.Field(typeof(VegetableFieldsRecord), field);
            if (fi == null) return 0f;
            var v = fi.GetValue(r);
            return v is float f ? f : 0f;
        }

        private static void MaybeLogVanilla()
        {
            if (!Config.BountifulFieldsLogVanilla.Value) return;
            if (_vanillaLogged) return;
            _vanillaLogged = true;

            Plugin.Log.Msg("[Bountiful Fields] ===== VANILLA VEGETABLE FIELD VALUES =====");
            Plugin.Log.Msg("  Record                    Fertility  PlantDays  MatureDays  WeedInjection  FrostDie%  HeatDie%");
            foreach (var kv in _vanillaByRecord)
            {
                var s = kv.Value;
                Plugin.Log.Msg(string.Format(
                    "  {0,-24}  {1,9}  {2,9}  {3,10}  {4,13:F2}  {5,9}  {6,8}",
                    kv.Key, s.Fertility, s.PlantingDays, s.MatureDays, s.WeedLevel,
                    s.PercentDiesOnFrost, s.BasePercentDiesOfHeatStress));
            }
            Plugin.Log.Msg("[Bountiful Fields] ===== END VANILLA DUMP =====");
        }

        /// <summary>
        /// Diagnostic: dump every CropDiseaseRecord (the disease definitions) to the log.
        /// Disease records live in asset bundles, not the DLL, so this is the only way to
        /// see their target-crop lists and tuning. Gated on its own LogDiseases toggle
        /// (~80 lines) so it doesn't ride along with the vanilla-values dump.
        /// </summary>
        private static void MaybeLogDiseases()
        {
            if (!Config.BountifulFieldsLogDiseases.Value) return;
            if (_diseasesLogged) return;
            _diseasesLogged = true;

            try
            {
                var diseases = ObjectDataStore.GetAllDataRecords<CropDiseaseRecord>();
                if (diseases == null)
                {
                    Plugin.Log.Msg("[Bountiful Fields] No CropDiseaseRecord data found.");
                    return;
                }

                Plugin.Log.Msg("[Bountiful Fields] ===== CROP DISEASE RECORDS =====");
                int count = 0;
                foreach (var d in diseases)
                {
                    if (d == null) continue;
                    count++;

                    var targets = new System.Collections.Generic.List<string>();
                    if (!string.IsNullOrEmpty(d.targetCrop1)) targets.Add(d.targetCrop1);
                    if (!string.IsNullOrEmpty(d.targetCrop2)) targets.Add(d.targetCrop2);
                    if (!string.IsNullOrEmpty(d.targetCrop3)) targets.Add(d.targetCrop3);
                    if (!string.IsNullOrEmpty(d.targetCrop4)) targets.Add(d.targetCrop4);
                    string targetList = targets.Count > 0 ? string.Join(", ", targets.ToArray()) : "(none)";

                    Plugin.Log.Msg($"  • {d.name}  (\"{d.diseaseName}\")");
                    Plugin.Log.Msg($"      targets:      {targetList}");
                    Plugin.Log.Msg($"      infect/plant: {d.infectionChancePerPlantingPercent:F3}   " +
                                   $"cropLoss@full: {d.cropLossAtFullMagnitudePercent:F3}");
                    Plugin.Log.Msg($"      activeMonths: {d.activeStartMonthNum}..{d.activeEndMonthNum}   " +
                                   $"initMag: {d.initialMagnitudePercent:F3}   " +
                                   $"+/plant: {d.perPlantingMagnitudeIncreasePercent:F3}   " +
                                   $"-/plant: {d.perPlantingMagnitudeDecreasePercent:F3}");
                    Plugin.Log.Msg($"      spreadChance@full: {d.spreadChanceAtFullMagnitudePercent:F3}   " +
                                   $"spreadDist: {d.spreadDistanceAtFullMagnitudeMeters}m   " +
                                   $"gracePeriodYears: {d.gracePeriodYears}");
                }
                Plugin.Log.Msg($"[Bountiful Fields] ===== END DISEASE DUMP ({count} diseases) =====");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.Warning($"[Bountiful Fields] Disease dump failed: {ex.Message}");
            }
        }

        /// <summary>Reset diagnostic-dump flags so the next map load re-dumps. Called from Plugin.OnSceneWasInitialized.</summary>
        public static void ResetLogFlag()
        {
            _vanillaLogged = false;
            _diseasesLogged = false;
        }

        private static void ApplyPerCrop()
        {
            var records = ObjectDataStore.GetAllDataRecords<VegetableFieldsRecord>();
            if (records == null) return;

            foreach (var record in records)
            {
                if (record == null) continue;
                string recName = record.name;
                if (string.IsNullOrEmpty(recName) || !recName.Contains("Field")) continue;

                string crop = StripFieldSuffix(recName);
                if (!_byCrop.TryGetValue(crop, out var e)) continue;
                if (!e.Apply.Value) continue;

                // Range-guarded writes — keep values in their sensible bounds even if
                // user typos something absurd in cfg. Range allows the full vanilla
                // span plus some headroom.
                ApplyIntInRange  (record, "_fertilityDepletionPerPlantingPercent", e.Fertility.Value,    -10, 10);
                ApplyIntInRange  (record, "_daysOfPlanting",                      e.PlantingDays.Value, 1, 30);
                ApplyIntInRange  (record, "_daysToMature",                        e.MatureDays.Value,   10, 300);
                ApplyFloatInRange(record, "_weedLevelMultiplier",                 e.WeedLevel.Value,    -20f, 10f);
                ApplyIntInRange  (record, "_percentDiesOnFrost",                  e.FrostDiePct.Value,  0, 100);
                ApplyIntInRange  (record, "_basePercentDiesOfHeatStress",         e.HeatDiePct.Value,   0, 100);
            }
        }

        private static string StripFieldSuffix(string s)
        {
            const string suffix = "Field";
            int idx = s.IndexOf(suffix, System.StringComparison.Ordinal);
            return idx > 0 ? s.Substring(0, idx) : s;
        }

        private static void ApplyIntInRange(VegetableFieldsRecord r, string field, int value, int min, int max)
        {
            if (value < min || value > max) return; // out of range → leave vanilla alone
            var fi = AccessTools.Field(typeof(VegetableFieldsRecord), field);
            if (fi == null) return;
            fi.SetValue(r, value);
        }

        private static void ApplyFloatInRange(VegetableFieldsRecord r, string field, float value, float min, float max)
        {
            if (value < min || value > max) return;
            var fi = AccessTools.Field(typeof(VegetableFieldsRecord), field);
            if (fi == null) return;
            fi.SetValue(r, value);
        }
    }
}
