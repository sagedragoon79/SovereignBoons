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

            MelonLogger.Msg("[SovereignBoons] Config initialized (Phase 1: 5 boons)");
        }
    }
}
