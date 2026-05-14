using MelonLoader;

namespace SovereignBoons
{
    /// <summary>
    /// Central registry for every MelonPreferences entry. Boon files only ever
    /// read from these properties — they never create their own entries. All
    /// master toggles default to false: this is an opt-in power-spike pack.
    ///
    /// Layout convention:
    ///   ----- &lt;Boon Name&gt; (folded from &lt;mod&gt; by &lt;author&gt;) -----
    ///   public static MelonPreferences_Entry&lt;...&gt; Enable&lt;Boon&gt; { get; private set; } = null!;
    ///   public static MelonPreferences_Entry&lt;...&gt; &lt;Tunable&gt;    { get; private set; } = null!;
    ///
    /// Each fold also appends a registration block in
    /// KeepClarityIntegration.RegisterEntries() under its bucket.
    /// </summary>
    public static class Config
    {
        private static MelonPreferences_Category _root = null!;

        // ===== Economy bucket =====

        // ----- Crown's Bounty (folded from TaxGoldgainMono by coos) -----
        public static MelonPreferences_Entry<bool>  EnableCrownsBounty { get; private set; } = null!;
        public static MelonPreferences_Entry<float> CrownsBountyTaxMultiplier { get; private set; } = null!;

        // ===== Workforce bucket =====

        // ----- Swift Feet (folded from FastVillagers by Krasipeace) -----
        public static MelonPreferences_Entry<bool>  EnableSwiftFeet         { get; private set; } = null!;
        public static MelonPreferences_Entry<float> SwiftFeetShoeBonus      { get; private set; } = null!;
        public static MelonPreferences_Entry<float> SwiftFeetWagonSpeed     { get; private set; } = null!;
        public static MelonPreferences_Entry<int>   SwiftFeetWagonCapacity  { get; private set; } = null!;

        // ----- Eager Hands (folded from ForcedChildLabor by Krasipeace) -----
        public static MelonPreferences_Entry<bool> EnableEagerHands       { get; private set; } = null!;
        public static MelonPreferences_Entry<int>  EagerHandsChildAge     { get; private set; } = null!;
        public static MelonPreferences_Entry<int>  EagerHandsAdolescentAge{ get; private set; } = null!;
        public static MelonPreferences_Entry<int>  EagerHandsSchoolMinAge { get; private set; } = null!;
        public static MelonPreferences_Entry<int>  EagerHandsSchoolMaxAge { get; private set; } = null!;

        // ===== Buildings bucket =====

        // ----- Spring's Vigor (folded from VC_FasterWaterRecharge by VC) -----
        public static MelonPreferences_Entry<bool>  EnableSpringsVigor       { get; private set; } = null!;
        public static MelonPreferences_Entry<float> SpringsVigorRechargeMult { get; private set; } = null!;
        public static MelonPreferences_Entry<float> SpringsVigorCapacityMult { get; private set; } = null!;

        // ----- Long Reach (folded from VC_BuildingRadiusAdjust by VC) -----
        public static MelonPreferences_Entry<bool>  EnableLongReach          { get; private set; } = null!;
        public static MelonPreferences_Entry<float> LongReachWorkCampPct     { get; private set; } = null!;
        public static MelonPreferences_Entry<float> LongReachHunterPct       { get; private set; } = null!;
        public static MelonPreferences_Entry<float> LongReachFishingPct      { get; private set; } = null!;
        public static MelonPreferences_Entry<float> LongReachArboristPct     { get; private set; } = null!;
        public static MelonPreferences_Entry<float> LongReachMarketPct       { get; private set; } = null!;
        public static MelonPreferences_Entry<float> LongReachForagerPct      { get; private set; } = null!;
        public static MelonPreferences_Entry<float> LongReachRatCatcherPct   { get; private set; } = null!;

        // ----- Civic Pride (folded from VC_DesirabilityBuildingsControl by VC) -----
        public static MelonPreferences_Entry<bool>  EnableCivicPride { get; private set; } = null!;
        public static MelonPreferences_Entry<float> CivicPrideRadiusMul { get; private set; } = null!;
        public static MelonPreferences_Entry<float> CivicPrideBonusMul  { get; private set; } = null!;

        // ----- Hoarded Stores (folded from VC_UserStorageConfig by VC) -----
        public static MelonPreferences_Entry<bool>  EnableHoardedStores             { get; private set; } = null!;
        public static MelonPreferences_Entry<bool>  HoardedStoresRootCellarEnable   { get; private set; } = null!;
        public static MelonPreferences_Entry<float> HoardedStoresRootCellarMul      { get; private set; } = null!;
        public static MelonPreferences_Entry<bool>  HoardedStoresGranaryEnable      { get; private set; } = null!;
        public static MelonPreferences_Entry<float> HoardedStoresGranaryMul         { get; private set; } = null!;
        public static MelonPreferences_Entry<bool>  HoardedStoresStorehouseEnable   { get; private set; } = null!;
        public static MelonPreferences_Entry<float> HoardedStoresStorehouseMul      { get; private set; } = null!;
        public static MelonPreferences_Entry<bool>  HoardedStoresStorageDepotEnable { get; private set; } = null!;
        public static MelonPreferences_Entry<float> HoardedStoresStorageDepotMul    { get; private set; } = null!;
        public static MelonPreferences_Entry<bool>  HoardedStoresStockyardEnable    { get; private set; } = null!;
        public static MelonPreferences_Entry<float> HoardedStoresStockyardMul       { get; private set; } = null!;
        public static MelonPreferences_Entry<bool>  HoardedStoresTreasuryEnable     { get; private set; } = null!;
        public static MelonPreferences_Entry<float> HoardedStoresTreasuryMul        { get; private set; } = null!;
        public static MelonPreferences_Entry<bool>  HoardedStoresMarketEnable       { get; private set; } = null!;
        public static MelonPreferences_Entry<float> HoardedStoresMarketMul          { get; private set; } = null!;

        // ----- Greater Halls (folded from VC_ModifyWorkerSlots by VC) -----
        public static MelonPreferences_Entry<bool> EnableGreaterHalls { get; private set; } = null!;
        // Per-building add-on entries created dynamically in Boons.GreaterHalls.RegisterPrefs.

        // ----- Hallowed Reliquary (folded from VC_ModifyTemple by VC) -----
        public static MelonPreferences_Entry<bool>  EnableHallowedReliquary         { get; private set; } = null!;
        public static MelonPreferences_Entry<float> HallowedReliquaryBonusMul       { get; private set; } = null!;
        public static MelonPreferences_Entry<bool>  HallowedReliquaryUnchainRelics  { get; private set; } = null!;

        // ----- Bountiful Fields (folded from VC_ConfigurableCropFields by VC) -----
        public static MelonPreferences_Entry<bool>  EnableBountifulFields              { get; private set; } = null!;
        public static MelonPreferences_Entry<float> BountifulFieldsGridsPerFarmerMul   { get; private set; } = null!;
        public static MelonPreferences_Entry<int>   BountifulFieldsMaintenanceDays     { get; private set; } = null!;
        // Per-crop entries created dynamically in Boons.BountifulFields.RegisterPrefs.

        // ===== Weather bucket =====

        // ----- Temperate Skies (folded from VC_NoBlizzardAndDrought by VC) -----
        public static MelonPreferences_Entry<bool> EnableTemperateSkies              { get; private set; } = null!;
        public static MelonPreferences_Entry<bool> TemperateSkiesDisableBlizzard     { get; private set; } = null!;
        public static MelonPreferences_Entry<bool> TemperateSkiesDisableHeatwave     { get; private set; } = null!;
        public static MelonPreferences_Entry<bool> TemperateSkiesDisableAllExtreme   { get; private set; } = null!;
        public static MelonPreferences_Entry<bool> TemperateSkiesDisableDrought      { get; private set; } = null!;

        // ===== Misc bucket =====

        // ----- Steadfast Resolve (folded from FFEnableAchievements by idontcare) -----
        public static MelonPreferences_Entry<bool> EnableSteadfastResolve { get; private set; } = null!;

        public static void Initialize()
        {
            _root = MelonPreferences.CreateCategory("SovereignBoons", "Sovereign Boons");

            // ===== Crown's Bounty =====
            EnableCrownsBounty = _root.CreateEntry(
                "EnableCrownsBounty", false,
                display_name: "Crown's Bounty — Enabled",
                description: "Multiply gold from tax collection events. Only tax inflows are affected — " +
                             "sales, refunds, and event rewards keep their vanilla amounts. " +
                             "Folded from TaxGoldgainMono by coos.");

            CrownsBountyTaxMultiplier = _root.CreateEntry(
                "CrownsBountyTaxMultiplier", 2.0f,
                display_name: "Crown's Bounty — Tax Multiplier",
                description: "Multiplier applied to gold from each tax-collection event. " +
                             "Vanilla = 1.0. Range 1.0 – 10.0.");

            // ===== Swift Feet =====
            EnableSwiftFeet = _root.CreateEntry(
                "EnableSwiftFeet", false,
                display_name: "Swift Feet — Enabled",
                description: "Faster villager run speed and beefier transport wagons. " +
                             "Folded from FastVillagers by Krasipeace.");

            SwiftFeetShoeBonus = _root.CreateEntry(
                "SwiftFeetShoeBonus", 0.30f,
                display_name: "Swift Feet — Villager Shoe Bonus",
                description: "Replaces Character._shoeBonusBase. Vanilla = 0.15 (no change). " +
                             "Set higher for faster villagers. Affects all Characters including raiders. " +
                             "Range 0.10 – 2.00.");

            SwiftFeetWagonSpeed = _root.CreateEntry(
                "SwiftFeetWagonSpeed", 8.0f,
                display_name: "Swift Feet — Wagon Move Speed",
                description: "TransportWagon._movementSpeed override. Vanilla is roughly 5. " +
                             "Range 4.0 – 20.0.");

            SwiftFeetWagonCapacity = _root.CreateEntry(
                "SwiftFeetWagonCapacity", 300,
                display_name: "Swift Feet — Wagon Carry Capacity",
                description: "TransportWagon.carryCapacity override. Vanilla ~150. " +
                             "Range 150 – 1000.");

            // ===== Eager Hands =====
            EnableEagerHands = _root.CreateEntry(
                "EnableEagerHands", false,
                display_name: "Eager Hands — Enabled",
                description: "Lower the age at which villagers join the labor pool. Affects existing " +
                             "saves on next map load. Folded from Forced Child Labor by Krasipeace.");

            EagerHandsChildAge = _root.CreateEntry(
                "EagerHandsChildAge", 12,
                display_name: "Eager Hands — Child Cutoff",
                description: "Age at which villagers stop being 'children' and become available as " +
                             "child workers. Vanilla = 15. Range 5 – 18.");

            EagerHandsAdolescentAge = _root.CreateEntry(
                "EagerHandsAdolescentAge", 18,
                display_name: "Eager Hands — Adolescent Cutoff",
                description: "Age at which villagers promote from adolescent to adult labor pool. " +
                             "Vanilla = 25. Range 12 – 30.");

            EagerHandsSchoolMinAge = _root.CreateEntry(
                "EagerHandsSchoolMinAge", 5,
                display_name: "Eager Hands — School Min Enrollment Age",
                description: "Minimum age for school enrollment. Vanilla = 5. Range 3 – 10.");

            EagerHandsSchoolMaxAge = _root.CreateEntry(
                "EagerHandsSchoolMaxAge", 10,
                display_name: "Eager Hands — School Max Enrollment Age",
                description: "Maximum age for school enrollment. Set very low to effectively disable " +
                             "schooling. Vanilla = 10. Range 5 – 60.");

            // ===== Spring's Vigor =====
            EnableSpringsVigor = _root.CreateEntry(
                "EnableSpringsVigor", false,
                display_name: "Spring's Vigor — Enabled",
                description: "Faster well recharge and bigger well capacity. " +
                             "Folded from VC_FasterWaterRecharge by VC.");

            SpringsVigorRechargeMult = _root.CreateEntry(
                "SpringsVigorRechargeMult", 2.0f,
                display_name: "Spring's Vigor — Recharge Multiplier",
                description: "Multiplier on Well.waterGainPerSecond. Vanilla = 0.01 (×1.0). " +
                             "Range 1.0 – 10.0.");

            SpringsVigorCapacityMult = _root.CreateEntry(
                "SpringsVigorCapacityMult", 2.0f,
                display_name: "Spring's Vigor — Capacity Multiplier",
                description: "Multiplier on Well.maxWater. Vanilla = 50 (×1.0). " +
                             "Range 1.0 – 10.0.");

            // ===== Steadfast Resolve =====
            EnableSteadfastResolve = _root.CreateEntry(
                "EnableSteadfastResolve", false,
                display_name: "Steadfast Resolve — Enabled",
                description: "Achievements unlock even when non-default game settings or mods are in use. " +
                             "Flips SettingsManager.allowCustomSettingsForAchievements at boot. " +
                             "Folded from FFEnableAchievements by idontcare.");

            // ===== Long Reach =====
            EnableLongReach = _root.CreateEntry("EnableLongReach", false,
                display_name: "Long Reach — Enabled",
                description: "Per-building work-radius multipliers. Folded from VC_BuildingRadiusAdjust by VC.");
            LongReachWorkCampPct   = _root.CreateEntry("LongReachWorkCampPct",   0f, display_name: "Long Reach — Work Camp +%",   description: "WorkCamp work radius. 0 = no change. Range -50..200.");
            LongReachHunterPct     = _root.CreateEntry("LongReachHunterPct",     0f, display_name: "Long Reach — Hunter +%",       description: "HunterBuilding radius. 0 = no change. Range -50..200.");
            LongReachFishingPct    = _root.CreateEntry("LongReachFishingPct",    0f, display_name: "Long Reach — Fishing +%",      description: "FishingShack radius. 0 = no change. Range -50..200.");
            LongReachArboristPct   = _root.CreateEntry("LongReachArboristPct",   0f, display_name: "Long Reach — Arborist +%",     description: "ArboristBuilding radius. 0 = no change. Range -50..200.");
            LongReachMarketPct     = _root.CreateEntry("LongReachMarketPct",     0f, display_name: "Long Reach — Market +%",       description: "MarketBuilding strategic planning radius. 0 = no change. Range -50..200.");
            LongReachForagerPct    = _root.CreateEntry("LongReachForagerPct",    0f, display_name: "Long Reach — Forager Shack +%",description: "ForagerShack foraging radius. 0 = no change. Range -50..200. [SB extension — source mod's pref was orphaned]");
            LongReachRatCatcherPct = _root.CreateEntry("LongReachRatCatcherPct", 0f, display_name: "Long Reach — Rat Catcher +%",  description: "RatCatcherBuilding work radius. 0 = no change. Range -50..200. [SB extension]");

            // ===== Civic Pride =====
            EnableCivicPride = _root.CreateEntry("EnableCivicPride", false,
                display_name: "Civic Pride — Enabled",
                description: "Multiply DecorativeBuilding desirability radius and bonus. " +
                             "Folded from VC_DesirabilityBuildingsControl by VC.");
            CivicPrideRadiusMul = _root.CreateEntry("CivicPrideRadiusMul", 1.5f,
                display_name: "Civic Pride — Radius Multiplier",
                description: "Multiplier on _strategicPlanningRadius. Vanilla = 1.0. Range 0.5..10.");
            CivicPrideBonusMul = _root.CreateEntry("CivicPrideBonusMul", 1.5f,
                display_name: "Civic Pride — Bonus Multiplier",
                description: "Multiplier on _strategicPlanningBonus. Vanilla = 1.0. Range 0.5..10.");

            // ===== Hoarded Stores =====
            EnableHoardedStores = _root.CreateEntry("EnableHoardedStores", false,
                display_name: "Hoarded Stores — Enabled",
                description: "Per-storage-type capacity multiplier. " +
                             "Folded from VC_UserStorageConfig by VC (capacity only; per-item-category quotas deferred to v0.4).");
            HoardedStoresRootCellarEnable   = _root.CreateEntry("HoardedStoresRootCellarEnable", false,   display_name: "Hoarded Stores — Root Cellar Apply",     description: "Apply capacity multiplier to Root Cellars.");
            HoardedStoresRootCellarMul      = _root.CreateEntry("HoardedStoresRootCellarMul", 2.0f,       display_name: "Hoarded Stores — Root Cellar Mul",       description: "Capacity multiplier for Root Cellars. Range 1..50.");
            HoardedStoresGranaryEnable      = _root.CreateEntry("HoardedStoresGranaryEnable", false,      display_name: "Hoarded Stores — Granary Apply",         description: "Apply capacity multiplier to Granaries.");
            HoardedStoresGranaryMul         = _root.CreateEntry("HoardedStoresGranaryMul", 2.0f,          display_name: "Hoarded Stores — Granary Mul",           description: "Capacity multiplier for Granaries. Range 1..50.");
            HoardedStoresStorehouseEnable   = _root.CreateEntry("HoardedStoresStorehouseEnable", false,   display_name: "Hoarded Stores — Storehouse Apply",      description: "Apply capacity multiplier to Storehouses.");
            HoardedStoresStorehouseMul      = _root.CreateEntry("HoardedStoresStorehouseMul", 2.0f,       display_name: "Hoarded Stores — Storehouse Mul",        description: "Capacity multiplier for Storehouses. Range 1..50.");
            HoardedStoresStorageDepotEnable = _root.CreateEntry("HoardedStoresStorageDepotEnable", false, display_name: "Hoarded Stores — Storage Depot Apply",   description: "Apply capacity multiplier to Storage Depots.");
            HoardedStoresStorageDepotMul    = _root.CreateEntry("HoardedStoresStorageDepotMul", 2.0f,     display_name: "Hoarded Stores — Storage Depot Mul",     description: "Capacity multiplier for Storage Depots. Range 1..50.");
            HoardedStoresStockyardEnable    = _root.CreateEntry("HoardedStoresStockyardEnable", false,    display_name: "Hoarded Stores — Stockyard Apply",       description: "Apply capacity multiplier to Stockyards.");
            HoardedStoresStockyardMul       = _root.CreateEntry("HoardedStoresStockyardMul", 2.0f,        display_name: "Hoarded Stores — Stockyard Mul",         description: "Capacity multiplier for Stockyards. Range 1..50.");
            HoardedStoresTreasuryEnable     = _root.CreateEntry("HoardedStoresTreasuryEnable", false,     display_name: "Hoarded Stores — Treasury Apply",        description: "Apply capacity multiplier to Treasuries.");
            HoardedStoresTreasuryMul        = _root.CreateEntry("HoardedStoresTreasuryMul", 2.0f,         display_name: "Hoarded Stores — Treasury Mul",          description: "Capacity multiplier for Treasuries. Range 1..50.");
            HoardedStoresMarketEnable       = _root.CreateEntry("HoardedStoresMarketEnable", false,       display_name: "Hoarded Stores — Market Apply",          description: "Apply capacity multiplier to Markets.");
            HoardedStoresMarketMul          = _root.CreateEntry("HoardedStoresMarketMul", 2.0f,           display_name: "Hoarded Stores — Market Mul",            description: "Capacity multiplier for Markets. Range 1..50.");

            // ===== Greater Halls =====
            EnableGreaterHalls = _root.CreateEntry("EnableGreaterHalls", false,
                display_name: "Greater Halls — Enabled",
                description: "Per-building add-on to maxWorkers / maxResidents for ~46 building types. " +
                             "Folded from VC_ModifyWorkerSlots by VC.");
            Boons.GreaterHalls.RegisterPrefs(_root);

            // ===== Hallowed Reliquary =====
            EnableHallowedReliquary = _root.CreateEntry("EnableHallowedReliquary", false,
                display_name: "Hallowed Reliquary — Enabled",
                description: "Spirituality bonus per relic multiplier + optional unchaining of " +
                             "relic activation from priest count. Inspired by VC_ModifyTemple by VC.");
            HallowedReliquaryBonusMul = _root.CreateEntry("HallowedReliquaryBonusMul", 1.5f,
                display_name: "Hallowed Reliquary — Bonus Multiplier",
                description: "Multiplier on ReligionManager._spiritualityBonusPerRelic. Vanilla = 1.0. Range 0.5..3.0.");
            HallowedReliquaryUnchainRelics = _root.CreateEntry("HallowedReliquaryUnchainRelics", true,
                display_name: "Hallowed Reliquary — Unchain Relics from Priest Count",
                description: "When true, a single priest in the Temple activates ALL assigned " +
                             "relics. Vanilla deactivates relics if priest count is below the slot " +
                             "count — flip this off to keep vanilla behavior.");

            // ===== Bountiful Fields =====
            EnableBountifulFields = _root.CreateEntry("EnableBountifulFields", false,
                display_name: "Bountiful Fields — Enabled",
                description: "Per-crop tuning of fertility/days/weed/frost/heat plus globals. " +
                             "Folded from VC_ConfigurableCropFields by VC.");
            BountifulFieldsGridsPerFarmerMul = _root.CreateEntry("BountifulFieldsGridsPerFarmerMul", 1.0f,
                display_name: "Bountiful Fields — Grids per Farmer Mul",
                description: "Multiplier on AgricultureManager._gridsPerFarmer. Vanilla = 1.0. Range 0.5..2.0.");
            BountifulFieldsMaintenanceDays = _root.CreateEntry("BountifulFieldsMaintenanceDays", -1,
                display_name: "Bountiful Fields — Maintenance Length (days)",
                description: "Override AgricultureManager._maintenanceLengthInDays. Range 45..90. -1 = no change.");
            Boons.BountifulFields.RegisterPrefs(_root);

            // ===== Temperate Skies =====
            EnableTemperateSkies = _root.CreateEntry("EnableTemperateSkies", false,
                display_name: "Temperate Skies — Enabled",
                description: "Suppress extreme weather events independently. " +
                             "Folded from VC_NoBlizzardAndDrought by VC.");
            TemperateSkiesDisableBlizzard   = _root.CreateEntry("TemperateSkiesDisableBlizzard",   false, display_name: "Temperate Skies — Disable Blizzard",      description: "Remove Blizzard from extreme weather rolls.");
            TemperateSkiesDisableHeatwave   = _root.CreateEntry("TemperateSkiesDisableHeatwave",   false, display_name: "Temperate Skies — Disable Heatwave",      description: "Remove Heatwave from extreme weather rolls.");
            TemperateSkiesDisableAllExtreme = _root.CreateEntry("TemperateSkiesDisableAllExtreme", false, display_name: "Temperate Skies — Disable ALL Extreme",   description: "Force chanceOfExtremeWeather to 0. Stacks with the per-event toggles.");
            TemperateSkiesDisableDrought    = _root.CreateEntry("TemperateSkiesDisableDrought",    false, display_name: "Temperate Skies — Disable Drought",       description: "Replace WeatherProfile.chanceOfDrought with a flat-zero curve.");

            MelonLogger.Msg("[SovereignBoons] Config initialized (Phase 1+2: 12 boons; " +
                            $"{Boons.GreaterHalls.Buildings.Length} building add-ons; " +
                            $"{Boons.BountifulFields.Crops.Length} crops)");
        }
    }
}
