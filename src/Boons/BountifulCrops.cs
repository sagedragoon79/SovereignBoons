// Bountiful Crops — five new farmable crops, folded under the Bountiful Fields feature.
//
// Ported from the PepperSpike proof-of-concept (v0.3.9, fully proven in-game 2026-07-25):
//   Pepper       → ItemSpice  (1/10 yield "dried & ground", sandy plateau 0.60–0.85)
//   MonksComfort → ItemRoots  (3:1 yield "only the roots are kept", clay plateau 0.15–0.40)
//   Hemp         → ItemFlax   (1:1, native flax harvest chain, clay)
//   Soybean      → ItemNuts   (1:1 staple food, Bean-companion soil)
//   Corn         → ItemGrain  (1:1, native grain chain, Wheat-companion soil, the tender grain)
// Record keys use the vanilla "XField" convention so Bountiful Fields' per-crop tuning
// (Crops array + StripFieldSuffix mapping) covers them exactly like vanilla crops.
//
// THE THREE-PART HARVEST CHAIN (required for ANY crop yielding a non-farm item):
//   (1) PlantResource.CheckWorkAvailability postfix → plants holding the item join GreensToHarvest
//   (2) GetFarmerSearchDefs postfix → a farmer SearchDefinition bound to the item + its store bucket
//   (3) CommitToSpecificObj prefix → def-swap (the Hungarian matcher binds ONE def per field with no
//       item check; the vanilla greens def shadows ours → re-bind at commit when the plant holds our item)
//
// Verified-gotcha notes carried from the spike (do not "simplify" these away):
//   - ObjectDataStore.Load has two overloads → pin the signature or Harmony throws Ambiguous.
//   - ObjectDataRecord.GetValueAsString returns NULL for a MISSING key → supply all four item slots.
//   - Picker icons schedule via PERSISTENT Button.onClick with the typeID BAKED as the int arg —
//     the component's own OnButtonClick is vestigial; disable + re-wire on clones.
//   - CropPlanting.Init calls GetCropPrefab(...).GetComponent with NO null check during save-load →
//     model links must be ensured inside the getters or loaded fields are DEMOLISHED.
//   - UICropInfoWindow.OnPlantDataChanged indexes GetDragItemPrefabs()[typeID] unbounded → pad every
//     draggable area before it runs or the crop window silently never opens.
//   - CheckToStayInWorkBucket has a 4th defaulted param — reflection does NOT apply C# defaults.
//   - Items come from TWO data paths (ItemsRecord sheet vs ItemSetupData) → read loc tags off a live
//     item instance, never the sheet.
//
// Master toggle semantics (save safety): Config.EnableBountifulCrops gates ONLY the picker icons
// (i.e., scheduling NEW plantings). Records, typeIDs, model links, and the harvest chain are ALWAYS
// active so saves containing these crops keep working when the toggle is off.
//
// Icon art: PNGs next to SovereignBoons.dll in Mods — SBCrop_Pepper.png, SBCrop_MonksComfort.png,
// SBCrop_Hemp.png, SBCrop_Soybean.png, SBCrop_Corn.png (256px, true alpha). Missing file → the
// clone keeps its donor sprite (still functional).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS8618 // CropSpec/ItemRename are data-table POCOs initialized inline below.

namespace SovereignBoons.Boons
{
    // Everything one injected crop needs. Item factories are compile-checked lambdas (no reflection).
    internal class CropSpec
    {
        public string Key;                      // record key — MUST end in "Field" (Bountiful Fields convention)
        public int TypeID;                      // crop typeID (vanilla 0..11)
        public string DisplayName;
        public string PluralDisplayName;
        public string DescText;
        public string DonorCrop;                // model + rendering-settings + icon-cell donor
        public int DonorTypeID;                 // drag-bar prefab donor
        public string DonorSpriteHint;          // lowercase fragment of donor sprite names (bar reskin)
        public string ItemName;                 // e.g. "ItemSpice"
        public Func<Item> NewItem;              // fresh instance for the SearchDefinition
        public Func<WorkBucketManager, Item> CanonicalItem;
        public WorkBucketIdentifier StoreBucket;
        public bool NativeFarmItem;             // one of the six native farm items → vanilla chain; skip (1)+(2)
        public double YieldFactor;              // fractional tally: stocked = tally of (qty × factor)
        public string IconFile;
        public Dictionary<string, string> VfColumns;

        public string DescKey => Key + "_Description";
        public SearchDefinition FarmerDef;
        public GameObject DragTemplate;
        public Sprite IconSprite;
        public bool IconTried;
        public double TallyBank;
    }

    internal static class BountifulCrops
    {
        internal static readonly List<CropSpec> Crops = new List<CropSpec>
        {
            new CropSpec
            {
                Key = "PepperField", TypeID = 12,
                DisplayName = "Pepper", PluralDisplayName = "Peppers",
                DescText = "A heat-loving spice crop. Hates frost; thrives in warm seasons.",
                DonorCrop = "LeekField", DonorTypeID = 10, DonorSpriteHint = "leek",
                ItemName = "ItemSpice",
                NewItem = () => new ItemSpice(),
                CanonicalItem = wbm => wbm.itemSpice,
                StoreBucket = WorkBucketIdentifier.CanStoreSpices,
                YieldFactor = 0.1,              // peppers are dried & ground to spice off-screen
                IconFile = "SBCrop_Pepper.png",
                VfColumns = new Dictionary<string, string>
                {
                    { "daysOfPlanting", "10" }, { "daysToMature", "70" }, { "daysToRot", "20" },
                    { "percentDiesOnFrost", "80" }, { "basePercentDiesOfHeatStress", "5" },
                    { "percentDiesOnDrought", "30" },
                    { "fertilityDepletionPerPlantingPercent", "10" }, { "fodderPercentOnRot", "0.25" },
                    { "weedLevelMultiplier", "1.0" }, { "rockinessResilience", "1.0" }, { "fertilityMultiplier", "1.0" },
                    // Plateau 0.60–0.85: a notch sandier than Flax = sandiest crop in game.
                    { "SCOffset1", "0.4" }, { "SCOffset2", "0.6" }, { "SCOffset3", "0.85" }, { "SCOffset4", "0.95" },
                    { "SCGreenMagnitude", "1.25" }, { "SCRedMagnitudePenalty", "-0.5" },
                    { "locked", "0" },
                },
            },
            new CropSpec
            {
                Key = "MonksComfortField", TypeID = 13,
                DisplayName = "Monk's Comfort", PluralDisplayName = "Monk's Comfort",
                DescText = "A hardy monastery herb whose dried roots bring comfort to the afflicted. Thrives in heavy clay.",
                DonorCrop = "FlaxField", DonorTypeID = 2, DonorSpriteHint = "flax",
                ItemName = "ItemRoots",
                NewItem = () => new ItemRoots(),
                CanonicalItem = wbm => wbm.itemRoots,
                StoreBucket = WorkBucketIdentifier.CanStoreRoots,
                YieldFactor = 1.0 / 3.0,        // 3 harvested → 1 root
                IconFile = "SBCrop_MonksComfort.png",
                VfColumns = new Dictionary<string, string>
                {
                    { "daysOfPlanting", "10" }, { "daysToMature", "65" }, { "daysToRot", "25" },
                    { "percentDiesOnFrost", "30" }, { "basePercentDiesOfHeatStress", "10" },
                    { "percentDiesOnDrought", "20" },
                    { "fertilityDepletionPerPlantingPercent", "8" }, { "fodderPercentOnRot", "0.25" },
                    { "weedLevelMultiplier", "1.0" }, { "rockinessResilience", "1.0" }, { "fertilityMultiplier", "1.0" },
                    // Mirrored Pepper: plateau 0.15–0.40 = clay-est crop in game (Cabbage/Pea companion).
                    { "SCOffset1", "0.05" }, { "SCOffset2", "0.15" }, { "SCOffset3", "0.4" }, { "SCOffset4", "0.6" },
                    { "SCGreenMagnitude", "1.25" }, { "SCRedMagnitudePenalty", "-0.5" },
                    { "locked", "0" },
                },
            },
            new CropSpec
            {
                Key = "HempField", TypeID = 14,
                DisplayName = "Hemp", PluralDisplayName = "Hemp",
                DescText = "A tall, fast-growing fiber crop. Its coarse stalks are retted into fiber for cloth and rope. Thrives in heavy clay.",
                DonorCrop = "FlaxField", DonorTypeID = 2, DonorSpriteHint = "flax",
                ItemName = "ItemFlax",
                NewItem = () => new ItemFlax(),
                CanonicalItem = wbm => wbm.itemFlax,
                StoreBucket = WorkBucketIdentifier.CanStoreHarvestedFlax,
                NativeFarmItem = true,          // Flax is native — vanilla flax-harvest chain does it all
                YieldFactor = 1.0,
                IconFile = "SBCrop_Hemp.png",
                VfColumns = new Dictionary<string, string>
                {
                    { "daysOfPlanting", "10" }, { "daysToMature", "60" }, { "daysToRot", "25" },
                    { "percentDiesOnFrost", "25" }, { "basePercentDiesOfHeatStress", "10" },
                    { "percentDiesOnDrought", "25" },
                    { "fertilityDepletionPerPlantingPercent", "12" }, { "fodderPercentOnRot", "0.25" },
                    { "weedLevelMultiplier", "1.0" }, { "rockinessResilience", "1.0" }, { "fertilityMultiplier", "1.0" },
                    // Same clay plateau as Monk's Comfort: clay fields rotate MC↔Hemp, sandy rotate Flax↔Peppers.
                    { "SCOffset1", "0.05" }, { "SCOffset2", "0.15" }, { "SCOffset3", "0.4" }, { "SCOffset4", "0.6" },
                    { "SCGreenMagnitude", "1.25" }, { "SCRedMagnitudePenalty", "-0.5" },
                    { "locked", "0" },
                },
            },
            new CropSpec
            {
                Key = "SoybeanField", TypeID = 15,
                DisplayName = "Soybean", PluralDisplayName = "Soybeans",
                DescText = "A hearty legume whose protein-rich beans store like nuts. Restores little from the soil it borrows.",
                DonorCrop = "BeanField", DonorTypeID = 0, DonorSpriteHint = "bean",
                ItemName = "ItemNuts",
                NewItem = () => new ItemNuts(),
                CanonicalItem = wbm => wbm.itemNuts,
                StoreBucket = WorkBucketIdentifier.CanStoreNuts,
                YieldFactor = 1.0,
                IconFile = "SBCrop_Soybean.png",
                VfColumns = new Dictionary<string, string>
                {
                    { "daysOfPlanting", "10" }, { "daysToMature", "70" }, { "daysToRot", "20" },
                    { "percentDiesOnFrost", "45" }, { "basePercentDiesOfHeatStress", "10" },
                    { "percentDiesOnDrought", "25" },
                    { "fertilityDepletionPerPlantingPercent", "5" }, // legume nitrogen-fixing nod — gentlest crop on soil
                    { "fodderPercentOnRot", "0.25" },
                    { "weedLevelMultiplier", "1.0" }, { "rockinessResilience", "1.0" }, { "fertilityMultiplier", "1.0" },
                    // Soil companion to Beans: BeanField's exact curve.
                    { "SCOffset1", "0.25" }, { "SCOffset2", "0.35" }, { "SCOffset3", "0.62" }, { "SCOffset4", "0.7" },
                    { "SCGreenMagnitude", "1.25" }, { "SCRedMagnitudePenalty", "-0.5" },
                    { "locked", "0" },
                },
            },
            new CropSpec
            {
                Key = "CornField", TypeID = 16,
                DisplayName = "Corn", PluralDisplayName = "Corn",
                DescText = "A tall summer grain that drinks deep and cannot abide the cold. It's got the juice!",
                DonorCrop = "WheatField", DonorTypeID = 3, DonorSpriteHint = "wheat",
                ItemName = "ItemGrain",
                NewItem = () => new ItemGrain(),
                CanonicalItem = wbm => wbm.itemGrain,
                StoreBucket = WorkBucketIdentifier.CanStoreGrain,
                NativeFarmItem = true,          // Grain is native — vanilla grain-harvest chain does it all
                YieldFactor = 1.0,
                IconFile = "SBCrop_Corn.png",
                VfColumns = new Dictionary<string, string>
                {
                    // Bountiful-Soil pair: Wheat's soil, OPPOSITE window — the TENDER grain (frost 70,
                    // plant late spring) vs the hardy early grains; thirstiest grain in the game.
                    { "daysOfPlanting", "10" }, { "daysToMature", "75" }, { "daysToRot", "20" },
                    { "percentDiesOnFrost", "70" }, { "basePercentDiesOfHeatStress", "5" },
                    { "percentDiesOnDrought", "40" },
                    { "fertilityDepletionPerPlantingPercent", "12" }, // hungry feeder
                    { "fodderPercentOnRot", "0.35" },                 // stalks make decent fodder
                    { "weedLevelMultiplier", "1.0" }, { "rockinessResilience", "1.0" }, { "fertilityMultiplier", "1.0" },
                    // Wheat's exact curve — grain fields run Wheat/Rye early → Corn late.
                    { "SCOffset1", "0.3" }, { "SCOffset2", "0.4" }, { "SCOffset3", "0.52" }, { "SCOffset4", "0.7" },
                    { "SCGreenMagnitude", "1.25" }, { "SCRedMagnitudePenalty", "-0.5" },
                    { "locked", "0" },
                },
            },
            new CropSpec
            {
                Key = "PurpleWillowField", TypeID = 17,
                DisplayName = "Purple Willow", PluralDisplayName = "Purple Willow",
                DescText = "A wetland osier whose supple purple rods are cut for basketry. Drinks deep and laughs at frost.",
                DonorCrop = "FlaxField", DonorTypeID = 2, DonorSpriteHint = "flax",
                ItemName = "ItemWillow",
                NewItem = () => new ItemWillow(),
                CanonicalItem = wbm => wbm.itemWillow,
                StoreBucket = WorkBucketIdentifier.CanStoreWillow,
                YieldFactor = 1.0,
                IconFile = "SBCrop_PurpleWillow.png",
                VfColumns = new Dictionary<string, string>
                {
                    // Personality: HARDIEST crop vs frost (wetland willows shrug off cold) and the
                    // THIRSTIEST vs drought — the anti-Pepper. Woody rods mature slowly, stand long.
                    { "daysOfPlanting", "10" }, { "daysToMature", "80" }, { "daysToRot", "30" },
                    { "percentDiesOnFrost", "10" }, { "basePercentDiesOfHeatStress", "30" },
                    { "percentDiesOnDrought", "40" },
                    { "fertilityDepletionPerPlantingPercent", "4" }, // willows barely draw on the soil
                    { "fodderPercentOnRot", "0.25" },
                    { "weedLevelMultiplier", "1.0" }, { "rockinessResilience", "1.0" }, { "fertilityMultiplier", "1.0" },
                    // Clay guild (with Monk's Comfort + Hemp): plateau 0.15–0.40.
                    { "SCOffset1", "0.05" }, { "SCOffset2", "0.15" }, { "SCOffset3", "0.4" }, { "SCOffset4", "0.6" },
                    { "SCGreenMagnitude", "1.25" }, { "SCRedMagnitudePenalty", "-0.5" },
                    { "locked", "0" },
                },
            },
            new CropSpec
            {
                Key = "CreminiField", TypeID = 18,
                DisplayName = "Cremini", PluralDisplayName = "Cremini",
                DescText = "Earthy brown mushrooms raised on beds of compost. They spring up fast in cool, damp weather — and wilt in the heat.",
                DonorCrop = "CabbageField", DonorTypeID = 9, DonorSpriteHint = "cabbage", // low rosettes read as caps
                ItemName = "ItemMushroom",
                NewItem = () => new ItemMushroom(),
                CanonicalItem = wbm => wbm.itemMushroom,
                StoreBucket = WorkBucketIdentifier.CanStoreMushroom,
                YieldFactor = 1.0,
                IconFile = "SBCrop_Cremini.png",
                VfColumns = new Dictionary<string, string>
                {
                    // Personality: the FAST, tolerant filler — quickest custom crop (45 days), worst
                    // heat tolerance in the roster (mushrooms wilt), needs damp, barely draws on soil.
                    { "daysOfPlanting", "10" }, { "daysToMature", "45" }, { "daysToRot", "15" },
                    { "percentDiesOnFrost", "20" }, { "basePercentDiesOfHeatStress", "40" },
                    { "percentDiesOnDrought", "35" },
                    { "fertilityDepletionPerPlantingPercent", "2" }, // compost beds, not soil crops
                    { "fodderPercentOnRot", "0.25" },
                    { "weedLevelMultiplier", "1.0" }, { "rockinessResilience", "1.0" }, { "fertilityMultiplier", "1.0" },
                    // Unique niche: the ONLY wide-plateau crop (0.25–0.65) — grows in nearly any damp
                    // earth, so it slots into any field's spare rotation window.
                    { "SCOffset1", "0.1" }, { "SCOffset2", "0.25" }, { "SCOffset3", "0.65" }, { "SCOffset4", "0.85" },
                    { "SCGreenMagnitude", "1.25" }, { "SCRedMagnitudePenalty", "-0.5" },
                    { "locked", "0" },
                },
            },
            new CropSpec
            {
                Key = "HerbsField", TypeID = 19,
                DisplayName = "Herb Garden", PluralDisplayName = "Herb Garden",
                DescText = "A fragrant bed of garden herbs. Like clover it feeds the soil as it grows — and the cuttings dry down to a fraction of their bulk.",
                DonorCrop = "CloverField", DonorTypeID = 5, DonorSpriteHint = "clover",
                ItemName = "ItemHerbs",
                NewItem = () => new ItemHerbs(),
                CanonicalItem = wbm => wbm.itemHerbs,
                StoreBucket = WorkBucketIdentifier.CanStoreHerbs,
                YieldFactor = 1.0 / 3.0,        // 3 harvested → 1 herbs (dried cuttings)
                IconFile = "SBCrop_Herbs.png",
                VfColumns = new Dictionary<string, string>
                {
                    // "Clover, but harvestable" (user): FERTILITY-POSITIVE (-3 restores, like Clover's
                    // -3) and weed-suppressing (-4, milder than Clover's -8) — the cover crop that
                    // also pays out. Modestly hardy all around.
                    { "daysOfPlanting", "10" }, { "daysToMature", "60" }, { "daysToRot", "20" },
                    { "percentDiesOnFrost", "15" }, { "basePercentDiesOfHeatStress", "15" },
                    { "percentDiesOnDrought", "20" },
                    { "fertilityDepletionPerPlantingPercent", "-3" },
                    { "fodderPercentOnRot", "0.25" },
                    { "weedLevelMultiplier", "-4.0" }, { "rockinessResilience", "1.0" }, { "fertilityMultiplier", "1.0" },
                    // Clover's exact soil curve (0.40–0.55 plateau) — rotate Herbs wherever Clover fits.
                    { "SCOffset1", "0.2" }, { "SCOffset2", "0.4" }, { "SCOffset3", "0.55" }, { "SCOffset4", "0.7" },
                    { "SCGreenMagnitude", "1.25" }, { "SCRedMagnitudePenalty", "-0.5" },
                    { "locked", "0" },
                },
            },
        };

        // Vanilla ITEM display renames, applied via the item's own loc tags read off a live instance.
        // With Flax AND Hemp both yielding ItemFlax, the shared good becomes generic "Fibers".
        internal class ItemRename { public string ItemName; public string Plural; public string Singular; }
        internal static readonly ItemRename[] ItemRenames =
        {
            new ItemRename { ItemName = "ItemFlax", Plural = "Fibers", Singular = "Fiber" },
        };

        internal static CropSpec ByKey(string recordName) => Crops.FirstOrDefault(c => c.Key == recordName);
        internal static CropSpec ByTypeID(int typeID) => Crops.FirstOrDefault(c => c.TypeID == typeID);
    }

    // =====================================================================================
    // DATA INJECTION — records + typeID mapping + I2.Loc terms, per crop. Always on (save safety).
    // =====================================================================================
    [HarmonyPatch(typeof(ObjectDataStore), "Load", new Type[] { typeof(LocalizationManager) })]
    internal static class BC_ObjectDataStore_Load_Patch
    {
        private static void Postfix(LocalizationManager __0)
        {
            try
            {
                foreach (var spec in BountifulCrops.Crops) InjectRecords(spec, __0);
                BC_Registry.InjectTypeIds();
                BC_Registry.InjectDisplayNames();
                int totalCrops = ObjectDataStore.GetAllDataRecords<VegetableFieldsRecord>()?.Count ?? -1;
                Plugin.Log.Msg($"[Bountiful Crops] {BountifulCrops.Crops.Count} crops injected ({totalCrops} total registered).");
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning($"[Bountiful Crops] injection failed: {ex}");
            }
        }

        private static void InjectRecords(CropSpec spec, LocalizationManager loc)
        {
            // itemQty1 drives ONLY the tooltip's relative "Crop Yield" stat bar — copy the donor's
            // real record quantity scaled by our YieldFactor so the bar shows a true expectation.
            int qty = 3;
            var donorVf = ObjectDataStore.GetDataRecord<VegetableFieldsRecord>(spec.DonorCrop);
            var donorRes = donorVf != null ? ObjectDataStore.GetDataRecord<ResourcesRecord>(donorVf.resourceName) : null;
            if (donorRes?.itemsToQtyDict != null && donorRes.itemsToQtyDict.Count > 0)
            {
                int donorTotal = donorRes.itemsToQtyDict.Values.Sum();
                qty = Math.Max(1, (int)Math.Round(donorTotal * spec.YieldFactor));
            }
            var resColumns = new Dictionary<string, string>
            {
                { "name", spec.Key },
                { "item1", spec.ItemName }, { "itemQty1", qty.ToString() },
                { "item2", "" }, { "itemQty2", "0" },
                { "item3", "" }, { "itemQty3", "0" },
                { "item4", "" }, { "itemQty4", "0" },
            };
            Put<ResourcesRecord>(spec.Key, new ResourcesRecord(new ObjectDataRecord(spec.Key, resColumns, loc)));

            var vfColumns = new Dictionary<string, string>(spec.VfColumns)
            {
                { "name", spec.Key },
                { "resourceName", spec.Key },
            };
            Put<VegetableFieldsRecord>(spec.Key, new VegetableFieldsRecord(new ObjectDataRecord(spec.Key, vfColumns, loc)));
        }

        private static void Put<T>(string name, T record) where T : DataRecord
        {
            if (!ObjectDataStore.dataSheetNameToDataRecords.TryGetValue(typeof(T), out var inner) || inner == null)
            {
                inner = new Dictionary<string, DataRecord>();
                ObjectDataStore.dataSheetNameToDataRecords[typeof(T)] = inner;
            }
            inner[name] = record; // overwrite-safe on Load re-run
        }
    }

    internal static class BC_Registry
    {
        internal static void InjectTypeIds()
        {
            var info = UICropTypeInfo.cropTypeInfo_s;
            var fwd = (Dictionary<int, string>)AccessTools.Field(typeof(UICropTypeInfo), "typeIDToRecordName").GetValue(info);
            var rev = (Dictionary<string, int>)AccessTools.Field(typeof(UICropTypeInfo), "recordNameToTypeID").GetValue(info);
            foreach (var spec in BountifulCrops.Crops)
            {
                fwd[spec.TypeID] = spec.Key;
                rev[spec.Key] = spec.TypeID;
            }
        }

        internal static void InjectDisplayNames()
        {
            foreach (var src in I2.Loc.LocalizationManager.Sources)
                foreach (var spec in BountifulCrops.Crops)
                {
                    SetTerm(src, "VegetableFields_Display_" + spec.Key, spec.DisplayName);
                    SetTerm(src, "VegetableFields_PluralDisplay_" + spec.Key, spec.PluralDisplayName);
                    SetTerm(src, spec.DescKey, spec.DescText);
                }
        }

        internal static void SetTerm(I2.Loc.LanguageSourceData src, string term, string value)
        {
            var td = src.AddTerm(term);
            if (td.Languages == null || td.Languages.Length == 0)
                td.Languages = new string[Math.Max(1, src.mLanguages?.Count ?? 1)];
            for (int i = 0; i < td.Languages.Length; i++)
                td.Languages[i] = value;
        }

        // Item renames need the item's ACTUAL loc tags; items come from two data paths, so read the
        // tags off a live instance at map time. One-shot with retry until tags resolve.
        private static bool _renamesApplied;
        internal static void ApplyItemRenames()
        {
            if (_renamesApplied || BountifulCrops.ItemRenames.Length == 0) return;
            foreach (var r in BountifulCrops.ItemRenames)
            {
                var item = MiscUtilities.CreateObjectByName<Item>(r.ItemName);
                if (item == null || string.IsNullOrEmpty(item.descriptionLocTag)) return; // retry later
                foreach (var src in I2.Loc.LocalizationManager.Sources)
                {
                    SetTerm(src, item.descriptionLocTag, r.Plural);
                    if (!string.IsNullOrEmpty(item.descriptionSingularLocTag))
                        SetTerm(src, item.descriptionSingularLocTag, r.Singular);
                }
                Plugin.Log.Msg($"[Bountiful Crops] item renamed: {r.ItemName} → '{r.Plural}'/'{r.Singular}'.");
            }
            _renamesApplied = true;
        }
    }

    // =====================================================================================
    // UI WIRING — picker icon clones (gated by the master toggle) + drag-bar templates + model links.
    // =====================================================================================
    [HarmonyPatch(typeof(UICropInfoScheduleBar), "TurnOnSelectionPopup")]
    internal static class BC_Popup_Patch
    {
        private static readonly FieldInfo F_recordName = AccessTools.Field(typeof(UICropInfoScheduleBarCropIcon), "_vegetableFieldRecordName");
        private static readonly FieldInfo F_iconID     = AccessTools.Field(typeof(UICropInfoScheduleBarCropIcon), "iconID");
        private static readonly FieldInfo F_cropIcons  = AccessTools.Field(typeof(UIHorizontalDraggableArea), "_cropIcons");
        private static readonly FieldInfo F_fieldObjectName   = AccessTools.Field(typeof(CropInfoTooltipDataProvider), "fieldObjectName");
        private static readonly FieldInfo F_descrLocalization = AccessTools.Field(typeof(CropInfoTooltipDataProvider), "descrLocalization");

        private static readonly string[] DragTintFields =
            { "outlineImage", "greenTint", "greyTint", "brownTint",
              "waitingStatus", "completedStatus", "failedBeginStatus", "lateHarvestStatus" };

        private static void Postfix(UICropInfoScheduleBar __instance)
        {
            try
            {
                EnsureCropModelFor(UnityEngine.Object.FindObjectOfType<AgricultureManager>());
                var area = __instance.draggableAreaComp;
                if (area == null) return;
                EnsureDragPrefabs(area);
                if (Config.EnableBountifulCrops.Value)
                {
                    // Reverse order: Hemp (last Flax-donor spec) claims Flax's free row slot so the
                    // fiber crops sit side by side; earlier specs take other free rows.
                    foreach (var spec in BountifulCrops.Crops.AsEnumerable().Reverse()) EnsureIcon(area, spec);
                }
                ReskinLiveItems(area);
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning($"[Bountiful Crops] popup wiring failed: {ex}");
            }
        }

        // Crop model + rendering settings, cloned from each spec's donor. AgricultureManager is
        // per-map (no session-wide done-flag); iterate lists directly (GetCropPrefab here would
        // recurse into our own prefix).
        internal static void EnsureCropModelFor(AgricultureManager am)
        {
            if (am == null) return;
            BC_Registry.ApplyItemRenames();
            var listField = AccessTools.Field(typeof(AgricultureManager), "_cropRenderingSettings");
            var rsList = listField?.GetValue(am) as System.Collections.IList;

            foreach (var spec in BountifulCrops.Crops)
            {
                GameObject donorPrefab = null;
                bool havePrefab = false;
                foreach (var ppi in am.plantPrefabs)
                {
                    if (ppi == null) continue;
                    if (ppi.recordName == spec.Key) havePrefab = true;
                    else if (ppi.recordName == spec.DonorCrop) donorPrefab = ppi.prefab;
                }
                if (!havePrefab && donorPrefab != null)
                    am.plantPrefabs.Add(new AgricultureManager.PlantPrefabInfo { recordName = spec.Key, prefab = donorPrefab });

                if (rsList != null)
                {
                    CropRenderingSettings donorRs = null;
                    bool haveRs = false;
                    foreach (var o in rsList)
                    {
                        if (o is CropRenderingSettings rs)
                        {
                            if (rs.recordName == spec.Key) haveRs = true;
                            else if (rs.recordName == spec.DonorCrop) donorRs = rs;
                        }
                    }
                    if (!haveRs && donorRs != null)
                    {
                        var clone = UnityEngine.Object.Instantiate(donorRs);
                        AccessTools.Field(typeof(CropRenderingSettings), "_recordName").SetValue(clone, spec.Key);
                        rsList.Add(clone);
                    }
                }
            }
        }

        // Drag-bar templates: parked ACTIVE under an INACTIVE holder (Instantiate copies root
        // activeSelf, and the game's fresh-instantiate path never calls SetActive(true)).
        private static GameObject _templateHolder;

        internal static void EnsureDragPrefabs(UIHorizontalDraggableArea area)
        {
            var prefabs = area.GetDragItemPrefabs();
            if (prefabs == null) return;

            foreach (var spec in BountifulCrops.Crops)
            {
                if (spec.DragTemplate == null && spec.DonorTypeID >= 0 && spec.DonorTypeID < prefabs.Count && prefabs[spec.DonorTypeID] != null)
                {
                    if (_templateHolder == null)
                    {
                        _templateHolder = new GameObject("SB_BountifulCrops_Templates");
                        _templateHolder.SetActive(false);
                        UnityEngine.Object.DontDestroyOnLoad(_templateHolder);
                    }
                    spec.DragTemplate = UnityEngine.Object.Instantiate(prefabs[spec.DonorTypeID], _templateHolder.transform);
                    spec.DragTemplate.SetActive(true);
                    spec.DragTemplate.name = "DragItem_" + spec.Key;
                    ReskinDragBar(spec.DragTemplate, spec);
                }

                var fallback = (spec.DonorTypeID >= 0 && spec.DonorTypeID < prefabs.Count) ? prefabs[spec.DonorTypeID] : null;
                while (prefabs.Count <= spec.TypeID) prefabs.Add(fallback);
                prefabs[spec.TypeID] = spec.DragTemplate ?? prefabs[spec.TypeID] ?? fallback;
            }
        }

        internal static void ReskinDragBar(GameObject bar, CropSpec spec)
        {
            var sprite = BC_IconLoader.Get(spec);
            if (sprite == null) return;
            var di = bar.GetComponent<UIHorizontalDragItem>();
            var exclude = new HashSet<Image>();
            if (di != null)
                foreach (var fn in DragTintFields)
                    if (AccessTools.Field(typeof(UIHorizontalDragItem), fn)?.GetValue(di) is Image img && img != null)
                        exclude.Add(img);

            foreach (var img in bar.GetComponentsInChildren<Image>(true).Where(i => !exclude.Contains(i)))
            {
                string goName = img.gameObject.name.ToLower();
                string spName = img.sprite != null ? img.sprite.name.ToLower() : "";
                if (spName.Contains(spec.DonorSpriteHint) || goName == "image" || goName.Contains("icon") || goName.Contains("crop"))
                    img.sprite = sprite;
            }
        }

        internal static void ReskinLiveItems(UIHorizontalDraggableArea area)
        {
            try
            {
                foreach (var item in area.dragItemsRO)
                {
                    if (item == null || item.data == null) continue;
                    var spec = BountifulCrops.ByTypeID(item.data.typeID);
                    if (spec != null) ReskinDragBar(item.gameObject, spec);
                }
            }
            catch { }
        }

        // Clone a donor icon cell → repoint the icon component, the tooltip provider, AND the
        // persistent Button.onClick (typeID baked as the serialized int arg). Fields are set while
        // the clone is INACTIVE so Start() reads the new values. Placed into a row with a free slot.
        private static void EnsureIcon(UIHorizontalDraggableArea area, CropSpec spec)
        {
            var icons = area.cropIcons;
            if (icons.Any(i => i != null && i.vegetableFieldRecordName == spec.Key)) return;

            var donor = icons.FirstOrDefault(i => i != null && i.vegetableFieldRecordName == spec.DonorCrop)
                        ?? icons.FirstOrDefault(i => i != null && !string.IsNullOrEmpty(i.vegetableFieldRecordName)
                                                     && i.vegetableFieldRecordName.EndsWith("Field"));
            if (donor == null) return;

            var clone = UnityEngine.Object.Instantiate(donor.gameObject, donor.transform.parent);
            clone.SetActive(false);
            clone.name = "CropIcon_" + spec.Key;

            try
            {
                var donorRow = donor.transform.parent;
                var rowsContainer = donorRow != null ? donorRow.parent : null;
                if (rowsContainer != null)
                {
                    int CountIcons(Transform row) => row.GetComponentsInChildren<UICropInfoScheduleBarCropIcon>(true).Length;
                    Transform targetRow = null;
                    if (CountIcons(donorRow) <= 2) targetRow = donorRow; // clone included in count
                    else
                        for (int i = 0; i < rowsContainer.childCount; i++)
                        {
                            var row = rowsContainer.GetChild(i);
                            int n = CountIcons(row);
                            if (n >= 1 && n < 2) { targetRow = row; break; }
                        }
                    // All rows full → use/create overflow rows at the bottom of the panel (rows hold
                    // two). Fully automatic for future crops: each new overflow row grows the popup
                    // background by one row height so nothing ever hangs off the panel.
                    for (int oi = 1; targetRow == null && oi <= 16; oi++)
                    {
                        string rowName = oi == 1 ? "SBCrops_OverflowRow" : $"SBCrops_OverflowRow{oi}";
                        var existing = rowsContainer.Find(rowName);
                        if (existing != null)
                        {
                            if (CountIcons(existing) < 2) targetRow = existing;
                            continue; // full → try the next overflow row index
                        }
                        var newRow = UnityEngine.Object.Instantiate(donorRow.gameObject, rowsContainer);
                        newRow.name = rowName;
                        foreach (var ic in newRow.GetComponentsInChildren<UICropInfoScheduleBarCropIcon>(true))
                        {
                            ic.transform.SetParent(null); // detach so same-frame counts exclude it
                            UnityEngine.Object.Destroy(ic.gameObject);
                        }
                        // Sit ABOVE the maintenance/clover row (keep crops clustered)…
                        if (rowsContainer.childCount >= 2)
                            newRow.transform.SetSiblingIndex(rowsContainer.childCount - 2);
                        // …and grow the popup background one row per created overflow row.
                        float rowH = (donorRow is RectTransform drt ? drt.rect.height : 40f) + 6f;
                        for (Transform t = rowsContainer; t != null; t = t.parent)
                        {
                            if (t.GetComponent<Image>() != null && t is RectTransform rt)
                            {
                                rt.sizeDelta = new Vector2(rt.sizeDelta.x, rt.sizeDelta.y + rowH);
                                break;
                            }
                        }
                        targetRow = newRow.transform;
                    }
                    if (targetRow != null && targetRow != donorRow)
                        clone.transform.SetParent(targetRow, false);
                }
            }
            catch { }

            var comp = clone.GetComponent<UICropInfoScheduleBarCropIcon>();
            F_recordName.SetValue(comp, spec.Key);
            F_iconID.SetValue(comp, spec.TypeID);

            var info = clone.GetComponentInChildren<CropInfoTooltipDataProvider>(true);
            if (info != null)
            {
                F_fieldObjectName.SetValue(info, spec.Key);
                F_descrLocalization.SetValue(info, spec.DescKey);
            }

            var sprite = BC_IconLoader.Get(spec);
            if (sprite != null)
                foreach (var img in clone.GetComponentsInChildren<Image>(true))
                    if (img.gameObject.name == "Image" || img.transform.parent == clone.transform)
                        img.sprite = sprite;

            var btn = clone.GetComponent<Button>();
            if (btn != null)
            {
                int pcount = btn.onClick.GetPersistentEventCount();
                for (int i = 0; i < pcount; i++)
                {
                    var target = btn.onClick.GetPersistentTarget(i);
                    string method = btn.onClick.GetPersistentMethodName(i);
                    btn.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    if (target != null && !string.IsNullOrEmpty(method))
                    {
                        var mi = AccessTools.Method(target.GetType(), method, new Type[] { typeof(int) });
                        if (mi != null)
                        {
                            var tgt = target; var m = mi; var id = spec.TypeID;
                            btn.onClick.AddListener(() =>
                            {
                                try { m.Invoke(tgt, new object[] { id }); }
                                catch (Exception ex) { Plugin.Log.Warning($"[Bountiful Crops] rewired click failed: {ex.Message}"); }
                            });
                        }
                    }
                }
            }

            clone.SetActive(true); // Start() now runs against the repointed fields
            comp.Activate();
            F_cropIcons?.SetValue(area, null); // invalidate cache
        }
    }

    // Install drag-bar templates before ANY drag item is instantiated (saved schedules load at
    // window-open, before the popup ever fires). Namespace-level: Harmony merges [HarmonyPatch]
    // attrs from enclosing classes into nested ones — never nest patch classes.
    [HarmonyPatch(typeof(UIHorizontalDraggableArea), "LoadDragItem")]
    internal static class BC_LoadDragItem_Patch
    {
        private static void Prefix(UIHorizontalDraggableArea __instance)
        {
            try { BC_Popup_Patch.EnsureDragPrefabs(__instance); } catch { }
        }
        private static void Postfix(UIHorizontalDraggableArea __instance)
        {
            try { BC_Popup_Patch.ReskinLiveItems(__instance); } catch { }
        }
    }

    [HarmonyPatch(typeof(UIHorizontalDraggableArea), "AddDragItem", new Type[] { typeof(int) })]
    internal static class BC_AddDragItem_Patch
    {
        private static void Prefix(UIHorizontalDraggableArea __instance)
        {
            try { BC_Popup_Patch.EnsureDragPrefabs(__instance); } catch { }
        }
        private static void Postfix(UIHorizontalDraggableArea __instance)
        {
            try { BC_Popup_Patch.ReskinLiveItems(__instance); } catch { }
        }
    }

    // =====================================================================================
    // LOAD SAFETY — save-load reconstructs plants via GetCropPrefab with no null check.
    // =====================================================================================
    [HarmonyPatch(typeof(AgricultureManager), "GetCropPrefab")]
    internal static class BC_GetCropPrefab_Patch
    {
        private static void Prefix(AgricultureManager __instance, string recordName)
        {
            try { if (BountifulCrops.ByKey(recordName) != null) BC_Popup_Patch.EnsureCropModelFor(__instance); }
            catch { }
        }
    }

    [HarmonyPatch(typeof(AgricultureManager), "GetCropRenderingSettings")]
    internal static class BC_GetCropRenderingSettings_Patch
    {
        private static void Prefix(AgricultureManager __instance, string recordName)
        {
            try { if (BountifulCrops.ByKey(recordName) != null) BC_Popup_Patch.EnsureCropModelFor(__instance); }
            catch { }
        }
    }

    // =====================================================================================
    // WINDOW-OPEN CRASH FIX — OnPlantDataChanged indexes GetDragItemPrefabs()[typeID] unbounded.
    // =====================================================================================
    [HarmonyPatch(typeof(UICropInfoWindow), "OnPlantDataChanged")]
    internal static class BC_OnPlantDataChanged_Patch
    {
        private static void Prefix(UICropInfoWindow __instance)
        {
            try
            {
                foreach (var area in __instance.GetComponentsInChildren<UIHorizontalDraggableArea>(true))
                    BC_Popup_Patch.EnsureDragPrefabs(area);
            }
            catch { }
        }
    }

    // =====================================================================================
    // HARVEST CHAIN (1/3) — routing. Only acts when the item IS present (a false
    // CheckToStayInWorkBucket call would evict vanilla greens crops from their bucket).
    // =====================================================================================
    [HarmonyPatch(typeof(PlantResource), "CheckWorkAvailability")]
    internal static class BC_PlantResource_CheckWork_Patch
    {
        private static readonly MethodInfo M_stay = AccessTools.Method(typeof(PlantResource), "CheckToStayInWorkBucket");

        private static void Postfix(PlantResource __instance)
        {
            try
            {
                if (!__instance.isActiveAndEnabled || !__instance.isValid) return;
                if (!__instance.isFarmerHarvestAllowed || __instance.resourceOwner == null) return;

                var wbm = UnitySingleton<GameManager>.Instance?.workBucketManager;
                if (wbm == null || __instance.storage == null) return;

                foreach (var spec in BountifulCrops.Crops)
                {
                    if (spec.NativeFarmItem) continue;
                    var item = spec.CanonicalItem(wbm);
                    if (item == null || __instance.storage.GetNumberOfUnreservedItems(item) == 0) continue;

                    bool canWork = __instance.CanAcceptAnotherWorker(null, null, playerOnly: true);
                    // 4-arg signature — reflection does NOT apply the defaulted float; pass 0f.
                    M_stay.Invoke(__instance, new object[] { canWork, __instance.resourceOwner, WorkBucketIdentifier.GreensToHarvest, 0f });
                    break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning($"[Bountiful Crops] harvest routing failed: {ex.Message}");
            }
        }
    }

    // =====================================================================================
    // HARVEST CHAIN (2/3) — dispatch. One item-bound farmer SearchDefinition per custom-item crop.
    // =====================================================================================
    [HarmonyPatch(typeof(SearchDefinitionManager), "GetFarmerSearchDefs")]
    internal static class BC_FarmerDefs_Patch
    {
        private static readonly HashSet<object> _augmented = new HashSet<object>();

        private static void Postfix(List<SearchDefinition> __result)
        {
            try
            {
                if (__result == null || _augmented.Contains(__result)) return;
                var wbm = UnitySingleton<GameManager>.Instance?.workBucketManager;
                if (wbm == null) return;

                foreach (var spec in BountifulCrops.Crops)
                {
                    if (spec.NativeFarmItem) continue;
                    var store = wbm.GetWorkBucket(wbm, spec.StoreBucket);
                    var def = new SearchDefinition(
                        wbm.GetWorkBucket(wbm, WorkBucketIdentifier.CropFieldHasGreensToHarvest),
                        new Pair<Item, StorageSet>(spec.NewItem(), new StorageSet(store, store)),
                        VillagerState.State.HarvestingCrops, VillagerState.State.HarvestingCrops,
                        _TaskExpires: false, _needsStorageSpaceToExecute: true,
                        _taskExpirationTime: 0f, _scoreModifier: 150f,
                        _itemOverrideForIntent: null, _useAvoidance: false, _ignoreDetachmentCosts: true);
                    def.bucketOwnerGroupedBucketID = WorkBucketIdentifier.GreensToHarvest;
                    __result.Add(def);
                    spec.FarmerDef = def;
                }
                _augmented.Add(__result);
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning($"[Bountiful Crops] farmer searchdef patch failed: {ex}");
            }
        }
    }

    // =====================================================================================
    // HARVEST CHAIN (3/3) — the commit-time def swap that defeats def-shadowing.
    // =====================================================================================
    [HarmonyPatch(typeof(ResourceCollectorService), "CommitToSpecificObj")]
    internal static class BC_Commit_Patch
    {
        private static void Prefix(IRegistersForWork workObj, ref SearchDefinition searchDef)
        {
            try
            {
                if (searchDef == null || !(workObj is PlantResource plant) || plant.storage == null) return;
                var wbm = UnitySingleton<GameManager>.Instance?.workBucketManager;
                if (wbm == null) return;

                foreach (var spec in BountifulCrops.Crops)
                {
                    if (spec.FarmerDef == null || ReferenceEquals(searchDef, spec.FarmerDef)) continue;
                    var item = spec.CanonicalItem(wbm);
                    if (item == null || plant.storage.GetItemCount(item) == 0) continue;
                    searchDef = spec.FarmerDef;
                    return;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning($"[Bountiful Crops] commit swap failed: {ex.Message}");
            }
        }
    }

    // =====================================================================================
    // YIELD SCALING — per-crop fractional tally; remainder banks across bundles/harvests.
    // =====================================================================================
    [HarmonyPatch(typeof(PlantResource), "AddHarvestableItems")]
    internal static class BC_AddHarvestable_Patch
    {
        private static bool Prefix(PlantResource __instance, ItemBundle items)
        {
            try
            {
                if (items == null || __instance.storage == null) return true;
                var spec = BountifulCrops.Crops.FirstOrDefault(c => c.ItemName == items.name);
                if (spec == null || spec.YieldFactor >= 1.0) return true;

                uint qty = items.numberOfItems;
                spec.TallyBank += qty * spec.YieldFactor;
                uint give = (uint)Math.Floor(spec.TallyBank);
                if (give > 0)
                {
                    spec.TallyBank -= give;
                    __instance.storage.AddItems(new ItemBundle(items, give, 100u));
                }
                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    // Window-open exception surfacing — vanilla swallows exceptions in this path silently (the crop
    // window just refuses to open with no log). Finalizers make the real error visible.
    [HarmonyPatch(typeof(UICropInfoWindowGameLayer), "SetTargetData")]
    internal static class BC_SetTargetData_Diag
    {
        private static void Finalizer(Exception __exception)
        {
            if (__exception != null)
                Plugin.Log.Warning($"[Bountiful Crops] CropWindow SetTargetData THREW: {__exception}");
        }
    }

    [HarmonyPatch(typeof(UICropInfoWindow), "LoadWindow")]
    internal static class BC_LoadWindow_Diag
    {
        private static void Finalizer(Exception __exception)
        {
            if (__exception != null)
                Plugin.Log.Warning($"[Bountiful Crops] CropWindow LoadWindow THREW: {__exception}");
        }
    }

    // Loads each crop's icon: a same-named PNG in the Mods folder overrides (art iteration without a
    // recompile); otherwise the base64 embedded in the DLL (SBCropIcons — Workshop single-DLL safe).
    internal static class BC_IconLoader
    {
        internal static Sprite Get(CropSpec spec)
        {
            if (spec.IconTried) return spec.IconSprite;
            spec.IconTried = true;
            try
            {
                byte[] pngData = null;
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string path = Path.Combine(dir, spec.IconFile);
                if (File.Exists(path)) pngData = File.ReadAllBytes(path);
                else if (SBCropIcons.Base64ByFile.TryGetValue(spec.IconFile, out var b64))
                    pngData = Convert.FromBase64String(b64);

                if (pngData != null)
                {
                    var tex = new Texture2D(2, 2);
                    tex.LoadImage(pngData);
                    spec.IconSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
                else Plugin.Log.Msg($"[Bountiful Crops] no icon for {spec.DisplayName} — using donor sprite.");
            }
            catch (Exception ex) { Plugin.Log.Warning($"[Bountiful Crops] {spec.Key} icon load failed: {ex.Message}"); }
            return spec.IconSprite;
        }
    }
}
