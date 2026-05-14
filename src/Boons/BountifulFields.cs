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

        // All "no change" sentinels are -1 for consistency with vanilla FF's
        // "use default" conventions. Tradeoff: setting any of these knobs to
        // exactly -1 is unreachable. Users wanting negative values can pick
        // -2 through -10 (or any non-(-1) value in the valid range).
        public const int NoChangeFertility = -1;
        public const int NoChangePlantingDays = -1;
        public const int NoChangeMatureDays = -1;
        public const float NoChangeWeed = -1f;
        public const int NoChangeFrost = -1;
        public const int NoChangeHeat = -1;

        // Lerp bounds — taken from VC_ConfigurableCropFields. UI is 0..10 on a
        // TOLERANCE scale: 10 = high tolerance = 0% dies (immune), 0 = no
        // tolerance = MaxLerp% dies (max vulnerability). The internal field
        // _percentDiesOnFrost / _basePercentDiesOfHeatStress stores 0..MaxLerp.
        private const float MinFrostLerp = 0f;
        private const float MaxFrostLerp = 80f; // source mod's max die% from frost
        private const float MinHeatLerp  = 0f;
        private const float MaxHeatLerp  = 75f; // source mod's max die% from heat

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

        // Captured vanilla snapshot per VegetableFieldsRecord. Populated on first
        // Apply() before any writes, so we can report actual vanilla values via
        // the LogVanilla pref even after overrides have been applied.
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

        public static void RegisterPrefs(MelonPreferences_Category cat)
        {
            foreach (var crop in Crops)
            {
                var e = new CropEntries
                {
                    Apply = cat.CreateEntry($"BountifulFields_{crop}_Apply", false,
                        display_name: $"{crop} — Apply Overrides",
                        description: $"Master switch for {crop} field tuning. " +
                                     "If false, vanilla values are used for this crop. " +
                                     "If true, each knob below is applied UNLESS set to -1 (= 'no change')."),
                    Fertility = cat.CreateEntry($"BountifulFields_{crop}_Fertility", NoChangeFertility,
                        display_name: $"{crop} — Fertility Depletion (per planting)",
                        description: "Fertility points consumed each planting cycle. Lower = field stays fertile " +
                                     "longer (power-spike). -2..-10 RESTORES fertility per cycle. Vanilla varies " +
                                     "per crop (small positive int) — enable 'Log Vanilla Values' to see exact values " +
                                     "in MelonLoader.log. Range: -10..10. -1 = no change (vanilla)."),
                    PlantingDays = cat.CreateEntry($"BountifulFields_{crop}_PlantingDays", NoChangePlantingDays,
                        display_name: $"{crop} — Planting Days",
                        description: "Days needed to plant the field before growth begins. Lower = faster turnaround. " +
                                     "Vanilla varies per crop (usually 5–10) — see Log Vanilla Values. " +
                                     "Range: 5..10. -1 = no change (vanilla)."),
                    MatureDays = cat.CreateEntry($"BountifulFields_{crop}_MatureDays", NoChangeMatureDays,
                        display_name: $"{crop} — Maturity Days",
                        description: "Days from planted to ready-to-harvest. Lower = faster crop (power-spike). " +
                                     "Vanilla varies per crop (~25 fast crops to ~150 grains) — see Log Vanilla Values. " +
                                     "Range: 25..150. -1 = no change (vanilla)."),
                    WeedLevel = cat.CreateEntry($"BountifulFields_{crop}_WeedLevel", NoChangeWeed,
                        display_name: $"{crop} — Weed Injection (per cycle)",
                        description: "Percent weed level ADDED each planting/harvest cycle (game applies as " +
                                     "`weedLevel += value / 100`). Lower or negative = fewer weeds (power-spike). " +
                                     "Vanilla is a small positive percent per crop — see Log Vanilla Values. " +
                                     "Range: -10..10. -1 = no change (vanilla). " +
                                     "To set exactly -1, use -2 or another negative value instead."),
                    FrostTolerance = cat.CreateEntry($"BountifulFields_{crop}_Frost", NoChangeFrost,
                        display_name: $"{crop} — Frost Tolerance (0..10)",
                        description: "UI scale: 0 = no tolerance (max % dies in frost), 10 = fully tolerant " +
                                     "(0% dies). HIGHER = power-spike. Internal write maps UI 0..10 to " +
                                     "_percentDiesOnFrost in range 80..0 (matches VC source's bounds). " +
                                     "Vanilla varies per crop — see Log Vanilla Values. -1 = no change."),
                    HeatTolerance = cat.CreateEntry($"BountifulFields_{crop}_Heat", NoChangeHeat,
                        display_name: $"{crop} — Heat Tolerance (0..10)",
                        description: "UI scale: 0 = no tolerance (max % dies in heatwave), 10 = fully tolerant " +
                                     "(0% dies). HIGHER = power-spike. Internal write maps UI 0..10 to " +
                                     "_basePercentDiesOfHeatStress in range 75..0 (matches VC source's bounds). " +
                                     "Vanilla varies per crop — see Log Vanilla Values. -1 = no change."),
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
                CaptureVanillaIfNeeded();
                MaybeLogVanilla();
                ApplyPerCrop();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.Warning($"[Bountiful Fields] Apply failed: {ex.Message}");
            }
        }

        private static void CaptureVanillaIfNeeded()
        {
            if (_vanillaByRecord.Count > 0) return; // already captured

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

        /// <summary>Reset vanilla-logged flag so the next map load re-dumps. Called from Plugin.OnSceneWasInitialized.</summary>
        public static void ResetLogFlag() => _vanillaLogged = false;

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

            foreach (var record in records)
            {
                if (record == null) continue;
                string recName = record.name;
                if (string.IsNullOrEmpty(recName) || !recName.Contains("Field")) continue;

                string crop = StripFieldSuffix(recName);
                if (!_byCrop.TryGetValue(crop, out var e)) continue;
                if (!e.Apply.Value) continue;

                // Each ApplyX method writes ONLY when the value is in the valid range.
                // -1 (and any out-of-range value, incl. legacy 255 / 999 sentinels from
                // earlier mod versions) is treated as "no change". This protects users
                // with old cfgs from accidental out-of-range writes.
                ApplyIntInRange  (record, "_fertilityDepletionPerPlantingPercent", e.Fertility.Value,    -10, 10);
                ApplyIntInRange  (record, "_daysOfPlanting",                      e.PlantingDays.Value, 5, 10);
                ApplyIntInRange  (record, "_daysToMature",                        e.MatureDays.Value,   25, 150);
                ApplyFloatInRange(record, "_weedLevelMultiplier",                 e.WeedLevel.Value,    -10f, 10f);

                if (e.FrostTolerance.Value >= 0 && e.FrostTolerance.Value <= 10)
                    ApplyIntInRange(record, "_percentDiesOnFrost",
                        ToleranceToPercent(e.FrostTolerance.Value, MinFrostLerp, MaxFrostLerp), 0, 100);
                if (e.HeatTolerance.Value >= 0 && e.HeatTolerance.Value <= 10)
                    ApplyIntInRange(record, "_basePercentDiesOfHeatStress",
                        ToleranceToPercent(e.HeatTolerance.Value, MinHeatLerp, MaxHeatLerp), 0, 100);
            }
        }

        private static void ApplyIntInRange(VegetableFieldsRecord r, string field, int value, int min, int max)
        {
            if (value < min || value > max) return; // out of range → treat as no-change
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

        private static string StripFieldSuffix(string s)
        {
            const string suffix = "Field";
            int idx = s.IndexOf(suffix, System.StringComparison.Ordinal);
            return idx > 0 ? s.Substring(0, idx) : s;
        }

        /// <summary>
        /// Convert a UI tolerance value (0..10) to internal die-percent (min..max).
        /// HIGHER tolerance UI → LOWER die % (inverse relationship).
        /// Matches VC_ConfigurableCropFields' ConvertBack:
        ///   percent = min + (max - min) * (1 - ui / 10)
        /// </summary>
        private static int ToleranceToPercent(int uiValue, float min, float max)
        {
            float t = UnityEngine.Mathf.Clamp01(uiValue / 10f);
            return UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Lerp(min, max, 1f - t));
        }
    }
}
