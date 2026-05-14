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

            // ===== Steadfast Resolve (Misc) =====
            Reg(GroupMisc, Config.EnableSteadfastResolve,
                NewMeta("Steadfast Resolve",
                        "Achievements unlock even with non-default settings or mods.",
                        restartRequired: true));
        }
    }
}
