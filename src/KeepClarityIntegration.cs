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
            // Boons add their Reg<T>(...) calls here as they land, grouped by
            // the Group* constants above.
            //
            // Example shape (uncomment when first boon lands):
            //
            // Reg(GroupEconomy, Config.EnableTaxBoost,
            //     NewMeta("Tax Boost", "Multiply gold income from tax events.",
            //             restartRequired: false));
        }
    }
}
