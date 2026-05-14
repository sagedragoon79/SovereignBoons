using System;
using System.Reflection;
using MelonLoader;

namespace SovereignBoons
{
    /// <summary>
    /// Optional integration with Keep Clarity's settings panel. If
    /// KeepClarity.dll isn't installed, every method here is a no-op and
    /// Sovereign Boons runs unchanged (prefs still readable from the
    /// MelonPreferences cfg). If KC IS installed, our prefs render with rich
    /// labels, tooltips, sliders, per-bucket groups, and VisibleWhen gating so
    /// sub-prefs hide when their master toggle is off.
    ///
    /// All access to Keep Clarity is reflective — no compile-time reference,
    /// so this file ships standalone without adding KeepClarity.dll as a hard
    /// build dependency.
    ///
    /// Pattern is the canonical KC integration template
    /// (see WardenOfTheWilds/KeepClarityIntegration.cs).
    /// </summary>
    internal static class KeepClarityIntegration
    {
        private static bool _resolved;
        private static bool _present;
        private static MethodInfo? _registerMod;
        private static MethodInfo? _registerEntry;
        private static Type? _settingsMetaType;

        private const string ModId = "SovereignBoons";
        private const string ModDisplayName = "Sovereign Boons";

        // Bucket group strings — used as the SettingsMeta.Group field.
        internal const string GroupEconomy   = "Economy";
        internal const string GroupWorkforce = "Workforce";
        internal const string GroupBuildings = "Buildings";
        internal const string GroupWeather   = "Weather";
        internal const string GroupCombat    = "Combat";
        internal const string GroupMisc      = "Misc";

        public static void TryRegisterAll()
        {
            if (!ResolveApi()) return;
            try
            {
                RegisterMod();
                RegisterEntries();
                MelonLogger.Msg("[SovereignBoons] Registered with Keep Clarity settings panel");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[SovereignBoons] Keep Clarity registration failed: {ex.Message}");
            }
        }

        private static bool ResolveApi()
        {
            if (_resolved) return _present;
            _resolved = true;

            var apiType = Type.GetType("FFUIOverhaul.Settings.SettingsAPI, KeepClarity");
            if (apiType == null) { _present = false; return false; }
            _settingsMetaType = Type.GetType("FFUIOverhaul.Settings.SettingsMeta, KeepClarity");
            if (_settingsMetaType == null) { _present = false; return false; }

            _registerMod = apiType.GetMethod("RegisterMod", BindingFlags.Public | BindingFlags.Static);
            foreach (var m in apiType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                if (m.Name == "Register" && m.IsGenericMethodDefinition) { _registerEntry = m; break; }

            _present = _registerMod != null && _registerEntry != null;
            return _present;
        }

        private static void RegisterMod()
        {
            _registerMod!.Invoke(null, new object?[] {
                ModId,
                ModDisplayName,
                "Power-spike pack: cherry-picked features from community mods to make your settlement stronger. " +
                "Every boon is OFF by default. Folded with credit from the original community modders — see README " +
                "for the provenance table.",
                /*version*/ "0.1.0",
                /*iconResourcePath*/ null,
                /*accentRgb — royal purple (sovereign theme)*/ new[] { 0.45f, 0.30f, 0.65f, 1f },
                /*order*/ 30
            });
        }

        internal static object NewMeta(string? label = null, string? tooltip = null,
            object? min = null, object? max = null, string? group = null,
            bool restartRequired = false, int order = 0, Func<bool>? visibleWhen = null)
        {
            var m = Activator.CreateInstance(_settingsMetaType!);
            void Set(string field, object? value)
            {
                var f = _settingsMetaType!.GetField(field);
                if (f != null) f.SetValue(m, value);
            }
            Set("Label", label);
            Set("Tooltip", tooltip);
            Set("Min", min);
            Set("Max", max);
            Set("Group", group);
            Set("RestartRequired", restartRequired);
            Set("Order", order);
            Set("VisibleWhen", visibleWhen);
            return m!;
        }

        internal static void Reg<T>(string category, MelonPreferences_Entry<T> entry, object meta)
        {
            if (!_present) return;
            var closed = _registerEntry!.MakeGenericMethod(typeof(T));
            closed.Invoke(null, new object?[] { ModId, ModDisplayName, category, entry, meta });
        }

        private static void RegisterEntries()
        {
            // ===== Crown's Bounty (Economy) =====
            Reg(GroupEconomy, Config.EnableCrownsBounty,
                NewMeta("Crown's Bounty",
                        "Multiply gold from tax-collection events. Honest to the name — " +
                        "sales/refunds/event rewards are untouched."));
            Reg(GroupEconomy, Config.CrownsBountyTaxMultiplier,
                NewMeta("Tax Multiplier", "Vanilla = 1.0.", min: 1.0f, max: 10.0f,
                        visibleWhen: () => Config.EnableCrownsBounty.Value));

            // ===== Wealthy Caravans (Economy) =====
            Reg(GroupEconomy, Config.EnableWealthyCaravans,
                NewMeta("Wealthy Caravans",
                        "Beefier traveling merchants + higher trading-post stock cap.",
                        restartRequired: false));
            Reg(GroupEconomy, Config.WealthyCaravansGoldMul,
                NewMeta("Gold Multiplier", "Multiplier on merchant gold pool.", min: 1.0f, max: 10.0f,
                        visibleWhen: () => Config.EnableWealthyCaravans.Value));
            Reg(GroupEconomy, Config.WealthyCaravansGoodsMul,
                NewMeta("Goods Multiplier", "Multiplier on goods counts.", min: 1.0f, max: 10.0f,
                        visibleWhen: () => Config.EnableWealthyCaravans.Value));
            Reg(GroupEconomy, Config.WealthyCaravansBuyAnything,
                NewMeta("Buy Anything",
                        "Merchants will purchase any item in their goods list OR already in storage.",
                        visibleWhen: () => Config.EnableWealthyCaravans.Value));
            Reg(GroupEconomy, Config.WealthyCaravansMaxStock,
                NewMeta("Trading Post Max Stock",
                        "Vanilla = 100.", min: 100, max: 5000,
                        visibleWhen: () => Config.EnableWealthyCaravans.Value));

            // ===== King's Highway (Workforce) =====
            Reg(GroupWorkforce, Config.EnableKingsHighway,
                NewMeta("King's Highway", "Faster travel on roads + slower aggressive animals.",
                        restartRequired: false));
            Reg(GroupWorkforce, Config.KingsHighwayBoostRoadSpeed,
                NewMeta("Boost Road Speed",
                        "Multiply the on-road speed bonus.",
                        visibleWhen: () => Config.EnableKingsHighway.Value));
            Reg(GroupWorkforce, Config.KingsHighwayRoadSpeedMul,
                NewMeta("Road Speed Multiplier",
                        "Vanilla = 1.0.", min: 1.0f, max: 3.0f,
                        visibleWhen: () => Config.EnableKingsHighway.Value && Config.KingsHighwayBoostRoadSpeed.Value));
            Reg(GroupWorkforce, Config.KingsHighwaySlowAggressiveAnimals,
                NewMeta("Slow Aggressive Animals",
                        "Wolves/bears/etc. move slower — easier to outrun, easier to hunt.",
                        visibleWhen: () => Config.EnableKingsHighway.Value));
            Reg(GroupWorkforce, Config.KingsHighwayAggressiveAnimalMul,
                NewMeta("Aggressive Animal Speed Mul",
                        "Vanilla = 1.0. Lower = slower predators.", min: 0.5f, max: 1.0f,
                        visibleWhen: () => Config.EnableKingsHighway.Value && Config.KingsHighwaySlowAggressiveAnimals.Value));

            // ===== Swift Feet (Workforce) =====
            Reg(GroupWorkforce, Config.EnableSwiftFeet,
                NewMeta("Swift Feet",
                        "Faster villagers + beefier transport wagons.",
                        restartRequired: true));
            Reg(GroupWorkforce, Config.SwiftFeetShoeBonus,
                NewMeta("Villager Shoe Bonus",
                        "Replaces Character._shoeBonusBase. Vanilla = 0.15. Affects raiders too.",
                        min: 0.10f, max: 2.00f,
                        visibleWhen: () => Config.EnableSwiftFeet.Value));
            Reg(GroupWorkforce, Config.SwiftFeetWagonSpeed,
                NewMeta("Wagon Move Speed", "TransportWagon._movementSpeed override.",
                        min: 4.0f, max: 20.0f,
                        visibleWhen: () => Config.EnableSwiftFeet.Value));
            Reg(GroupWorkforce, Config.SwiftFeetWagonCapacity,
                NewMeta("Wagon Carry Capacity", "TransportWagon.carryCapacity override.",
                        min: 150, max: 1000,
                        visibleWhen: () => Config.EnableSwiftFeet.Value));

            // ===== Eager Hands (Workforce) =====
            Reg(GroupWorkforce, Config.EnableEagerHands,
                NewMeta("Eager Hands",
                        "Lower age cutoffs for the labor pool + School enrollment range.",
                        restartRequired: false));
            Reg(GroupWorkforce, Config.EagerHandsChildAge,
                NewMeta("Child Cutoff Age", "Vanilla = 15.", min: 5, max: 18,
                        visibleWhen: () => Config.EnableEagerHands.Value));
            Reg(GroupWorkforce, Config.EagerHandsAdolescentAge,
                NewMeta("Adolescent Cutoff Age", "Vanilla = 25.", min: 12, max: 30,
                        visibleWhen: () => Config.EnableEagerHands.Value));
            Reg(GroupWorkforce, Config.EagerHandsSchoolMinAge,
                NewMeta("School Min Enrollment Age", "Vanilla = 5.", min: 3, max: 10,
                        visibleWhen: () => Config.EnableEagerHands.Value));
            Reg(GroupWorkforce, Config.EagerHandsSchoolMaxAge,
                NewMeta("School Max Enrollment Age",
                        "Vanilla = 10. Set very low to effectively disable schooling.",
                        min: 5, max: 60,
                        visibleWhen: () => Config.EnableEagerHands.Value));

            // ===== Spring's Vigor (Buildings) =====
            Reg(GroupBuildings, Config.EnableSpringsVigor,
                NewMeta("Spring's Vigor",
                        "Faster Well recharge + bigger Well capacity.",
                        restartRequired: true));
            Reg(GroupBuildings, Config.SpringsVigorRechargeMult,
                NewMeta("Recharge Multiplier",
                        "Multiplier on Well.waterGainPerSecond. Vanilla = 0.01 (×1.0).",
                        min: 1.0f, max: 10.0f,
                        visibleWhen: () => Config.EnableSpringsVigor.Value));
            Reg(GroupBuildings, Config.SpringsVigorCapacityMult,
                NewMeta("Capacity Multiplier",
                        "Multiplier on Well.maxWater. Vanilla = 50 (×1.0).",
                        min: 1.0f, max: 10.0f,
                        visibleWhen: () => Config.EnableSpringsVigor.Value));

            // ===== Levy's Arms (Combat) =====
            Reg(GroupCombat, Config.EnableLevysArms,
                NewMeta("Levy's Arms",
                        "Hotkey-driven militia. Arms every eligible villager with combat config + stat buff.",
                        restartRequired: false));
            Reg(GroupCombat, Config.LevysArmsArmKey,
                NewMeta("Arm Hotkey", "Unity KeyCode name (B, F4, etc.).",
                        visibleWhen: () => Config.EnableLevysArms.Value));
            Reg(GroupCombat, Config.LevysArmsUnarmKey,
                NewMeta("Unarm Hotkey", "Unity KeyCode name.",
                        visibleWhen: () => Config.EnableLevysArms.Value));
            Reg(GroupCombat, Config.LevysArmsStatMagnitude,
                NewMeta("Stat Magnitude",
                        "Applied to every Perc stat field. 100 = +100%; 1000 matches source's 'powerful' preset.",
                        min: 0f, max: 1000f,
                        visibleWhen: () => Config.EnableLevysArms.Value));

            // ===== Steadfast Resolve (Misc) =====
            Reg(GroupMisc, Config.EnableSteadfastResolve,
                NewMeta("Steadfast Resolve",
                        "Achievements unlock even with non-default settings or mods.",
                        restartRequired: true));

            // ===== Long Reach (Buildings) =====
            Reg(GroupBuildings, Config.EnableLongReach,
                NewMeta("Long Reach", "Per-building work-radius multipliers.", restartRequired: true));
            Reg(GroupBuildings, Config.LongReachWorkCampPct,
                NewMeta("Work Camp +%",      "WorkCamp work radius.",         min: -50f, max: 200f, visibleWhen: () => Config.EnableLongReach.Value));
            Reg(GroupBuildings, Config.LongReachHunterPct,
                NewMeta("Hunter +%",         "HunterBuilding radius.",        min: -50f, max: 200f, visibleWhen: () => Config.EnableLongReach.Value));
            Reg(GroupBuildings, Config.LongReachFishingPct,
                NewMeta("Fishing +%",        "FishingShack radius.",          min: -50f, max: 200f, visibleWhen: () => Config.EnableLongReach.Value));
            Reg(GroupBuildings, Config.LongReachArboristPct,
                NewMeta("Arborist +%",       "ArboristBuilding radius.",      min: -50f, max: 200f, visibleWhen: () => Config.EnableLongReach.Value));
            Reg(GroupBuildings, Config.LongReachMarketPct,
                NewMeta("Market +%",         "Market planning radius.",       min: -50f, max: 200f, visibleWhen: () => Config.EnableLongReach.Value));
            Reg(GroupBuildings, Config.LongReachForagerPct,
                NewMeta("Forager Shack +%",  "ForagerShack foraging radius.", min: -50f, max: 200f, visibleWhen: () => Config.EnableLongReach.Value));
            Reg(GroupBuildings, Config.LongReachRatCatcherPct,
                NewMeta("Rat Catcher +%",    "RatCatcher work radius.",       min: -50f, max: 200f, visibleWhen: () => Config.EnableLongReach.Value));

            // ===== Civic Pride (Buildings) =====
            Reg(GroupBuildings, Config.EnableCivicPride,
                NewMeta("Civic Pride", "Multiply DecorativeBuilding desirability radius/bonus.", restartRequired: true));
            Reg(GroupBuildings, Config.CivicPrideRadiusMul,
                NewMeta("Radius Multiplier", "Vanilla = 1.0.", min: 0.5f, max: 10f, visibleWhen: () => Config.EnableCivicPride.Value));
            Reg(GroupBuildings, Config.CivicPrideBonusMul,
                NewMeta("Bonus Multiplier",  "Vanilla = 1.0.", min: 0.5f, max: 10f, visibleWhen: () => Config.EnableCivicPride.Value));

            // ===== Hoarded Stores (Buildings) =====
            Reg(GroupBuildings, Config.EnableHoardedStores,
                NewMeta("Hoarded Stores", "Per-storage-type capacity multiplier.", restartRequired: true));
            Reg(GroupBuildings, Config.HoardedStoresRootCellarEnable,    NewMeta("Root Cellar — Apply",    "Apply capacity multiplier to Root Cellars.",   visibleWhen: () => Config.EnableHoardedStores.Value));
            Reg(GroupBuildings, Config.HoardedStoresRootCellarMul,       NewMeta("Root Cellar — Mul",      "Capacity multiplier.",      min: 1f, max: 50f, visibleWhen: () => Config.EnableHoardedStores.Value && Config.HoardedStoresRootCellarEnable.Value));
            Reg(GroupBuildings, Config.HoardedStoresGranaryEnable,       NewMeta("Granary — Apply",        "Apply capacity multiplier to Granaries.",      visibleWhen: () => Config.EnableHoardedStores.Value));
            Reg(GroupBuildings, Config.HoardedStoresGranaryMul,          NewMeta("Granary — Mul",          "Capacity multiplier.",      min: 1f, max: 50f, visibleWhen: () => Config.EnableHoardedStores.Value && Config.HoardedStoresGranaryEnable.Value));
            Reg(GroupBuildings, Config.HoardedStoresStorehouseEnable,    NewMeta("Storehouse — Apply",     "Apply capacity multiplier to Storehouses.",    visibleWhen: () => Config.EnableHoardedStores.Value));
            Reg(GroupBuildings, Config.HoardedStoresStorehouseMul,       NewMeta("Storehouse — Mul",       "Capacity multiplier.",      min: 1f, max: 50f, visibleWhen: () => Config.EnableHoardedStores.Value && Config.HoardedStoresStorehouseEnable.Value));
            Reg(GroupBuildings, Config.HoardedStoresStorageDepotEnable,  NewMeta("Storage Depot — Apply",  "Apply capacity multiplier to Storage Depots.", visibleWhen: () => Config.EnableHoardedStores.Value));
            Reg(GroupBuildings, Config.HoardedStoresStorageDepotMul,     NewMeta("Storage Depot — Mul",    "Capacity multiplier.",      min: 1f, max: 50f, visibleWhen: () => Config.EnableHoardedStores.Value && Config.HoardedStoresStorageDepotEnable.Value));
            Reg(GroupBuildings, Config.HoardedStoresStockyardEnable,     NewMeta("Stockyard — Apply",      "Apply capacity multiplier to Stockyards.",     visibleWhen: () => Config.EnableHoardedStores.Value));
            Reg(GroupBuildings, Config.HoardedStoresStockyardMul,        NewMeta("Stockyard — Mul",        "Capacity multiplier.",      min: 1f, max: 50f, visibleWhen: () => Config.EnableHoardedStores.Value && Config.HoardedStoresStockyardEnable.Value));
            Reg(GroupBuildings, Config.HoardedStoresTreasuryEnable,      NewMeta("Treasury — Apply",       "Apply capacity multiplier to Treasuries.",     visibleWhen: () => Config.EnableHoardedStores.Value));
            Reg(GroupBuildings, Config.HoardedStoresTreasuryMul,         NewMeta("Treasury — Mul",         "Capacity multiplier.",      min: 1f, max: 50f, visibleWhen: () => Config.EnableHoardedStores.Value && Config.HoardedStoresTreasuryEnable.Value));
            Reg(GroupBuildings, Config.HoardedStoresMarketEnable,        NewMeta("Market — Apply",         "Apply capacity multiplier to Markets.",        visibleWhen: () => Config.EnableHoardedStores.Value));
            Reg(GroupBuildings, Config.HoardedStoresMarketMul,           NewMeta("Market — Mul",           "Capacity multiplier.",      min: 1f, max: 50f, visibleWhen: () => Config.EnableHoardedStores.Value && Config.HoardedStoresMarketEnable.Value));

            // ===== Greater Halls (Buildings) =====
            Reg(GroupBuildings, Config.EnableGreaterHalls,
                NewMeta("Greater Halls",
                        "Per-building +Workers / +Residents add-on for ~46 building types. " +
                        "Grouped by Livestock / Production / Resource Sites / Field Work / Civic / Residential.",
                        restartRequired: true));
            foreach (var bp in Boons.GreaterHalls.Iterate())
            {
                Reg(GroupBuildings, bp.Entry,
                    NewMeta($"{bp.Name} +Workers",
                            $"Extra worker/resident slots for {bp.Name}. Category: {bp.Category}.",
                            min: bp.Min, max: bp.Max,
                            visibleWhen: () => Config.EnableGreaterHalls.Value));
            }

            // ===== Hallowed Reliquary (Buildings) =====
            Reg(GroupBuildings, Config.EnableHallowedReliquary,
                NewMeta("Hallowed Reliquary",
                        "Spirituality bonus multiplier + Unchain Relics from priest count.",
                        restartRequired: false));
            Reg(GroupBuildings, Config.HallowedReliquaryBonusMul,
                NewMeta("Spirituality Bonus Mul", "Vanilla = 1.0.", min: 0.5f, max: 3.0f,
                        visibleWhen: () => Config.EnableHallowedReliquary.Value));
            Reg(GroupBuildings, Config.HallowedReliquaryUnchainRelics,
                NewMeta("Unchain Relics from Priest Count",
                        "1 priest activates every assigned relic. Off = vanilla (priest count gates active relics).",
                        visibleWhen: () => Config.EnableHallowedReliquary.Value));

            // ===== Bountiful Fields (Buildings) =====
            Reg(GroupBuildings, Config.EnableBountifulFields,
                NewMeta("Bountiful Fields",
                        "Per-crop tuning + farming globals.", restartRequired: true));
            Reg(GroupBuildings, Config.BountifulFieldsGridsPerFarmerMul,
                NewMeta("Grids per Farmer Mul", "Vanilla = 1.0.", min: 0.5f, max: 2.0f,
                        visibleWhen: () => Config.EnableBountifulFields.Value));
            Reg(GroupBuildings, Config.BountifulFieldsMaintenanceDays,
                NewMeta("Maintenance Length (days)",
                        "Range 45..90. -1 = no change.", min: -1, max: 90,
                        visibleWhen: () => Config.EnableBountifulFields.Value));

            foreach (var cp in Boons.BountifulFields.Iterate())
            {
                var crop = cp.Crop;
                var e = cp.Entries;
                System.Func<bool> applyOn = () => Config.EnableBountifulFields.Value && e.Apply.Value;
                Reg(GroupBuildings, e.Apply,
                    NewMeta($"{crop} — Apply", $"Master switch for {crop} overrides.",
                            visibleWhen: () => Config.EnableBountifulFields.Value));
                Reg(GroupBuildings, e.Fertility,
                    NewMeta($"{crop} — Fertility %",  "Range -10..10. 255 = no change.", min: -10, max: 255, visibleWhen: applyOn));
                Reg(GroupBuildings, e.PlantingDays,
                    NewMeta($"{crop} — Planting Days", "Range 5..10. -1 = no change.",   min: -1,  max: 10,  visibleWhen: applyOn));
                Reg(GroupBuildings, e.MatureDays,
                    NewMeta($"{crop} — Mature Days",   "Range 25..150. -1 = no change.", min: -1,  max: 150, visibleWhen: applyOn));
                Reg(GroupBuildings, e.WeedLevel,
                    NewMeta($"{crop} — Weed Level",    "Multiplier on weed rate.",        min: -10f, max: 999f, visibleWhen: applyOn));
                Reg(GroupBuildings, e.FrostTolerance,
                    NewMeta($"{crop} — Frost Tol.",    "0 (immune) – 10 (vulnerable). -1 = no change.", min: -1, max: 10, visibleWhen: applyOn));
                Reg(GroupBuildings, e.HeatTolerance,
                    NewMeta($"{crop} — Heat Tol.",     "0 (immune) – 10 (vulnerable). -1 = no change.", min: -1, max: 10, visibleWhen: applyOn));
            }

            // ===== Temperate Skies (Weather) =====
            Reg(GroupWeather, Config.EnableTemperateSkies,
                NewMeta("Temperate Skies", "Suppress extreme weather events.", restartRequired: false));
            Reg(GroupWeather, Config.TemperateSkiesDisableBlizzard,
                NewMeta("Disable Blizzard", "Remove Blizzard from extreme weather rolls.",
                        visibleWhen: () => Config.EnableTemperateSkies.Value));
            Reg(GroupWeather, Config.TemperateSkiesDisableHeatwave,
                NewMeta("Disable Heatwave", "Remove Heatwave from extreme weather rolls.",
                        visibleWhen: () => Config.EnableTemperateSkies.Value));
            Reg(GroupWeather, Config.TemperateSkiesDisableAllExtreme,
                NewMeta("Disable ALL Extreme", "Force chanceOfExtremeWeather to 0.",
                        visibleWhen: () => Config.EnableTemperateSkies.Value));
            Reg(GroupWeather, Config.TemperateSkiesDisableDrought,
                NewMeta("Disable Drought", "Flat-zero curve override for chanceOfDrought.",
                        visibleWhen: () => Config.EnableTemperateSkies.Value));
        }
    }
}
