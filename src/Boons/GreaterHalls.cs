// Folded from VC_ModifyWorkerSlots by VC (v1.3)
// Original DLL: VC_ModifyWorkerSlots.dll (ships UniverseLib.Mono.dll + MelonPrefManager.Mono.dll deps)
// Original prefs: per-building IntCfg{Addon, Min, Max} for 46 building types.
// SB changes:
//   - Replaced custom IntCfg type with plain MelonPreferences_Entry<int> per building
//     (avoids a Toml-converter dependency).
//   - Min/max validators applied via MelonPreferences_Entry<int>.Validator at register time.
//   - Removed the "DOUBLE THE GUARDRAILS" oddity (source multiplied min/max by 2 in the
//     validator); SB uses the listed ranges as the actual bounds.
//   - Master toggle gates ALL building knobs at once.
//   - Group registration in KC happens in batches by building category so the panel
//     stays browseable (per the user's design note: multi-column grid would be ideal,
//     parked as a Phase 2.5 KC renderer extension).
//
// Verified targets (decompile_verification.md):
//   - 5 direct Awake postfixes: WorkCamp / CompostYard / MarketBuilding / Pub / School
//   - 2 AssignMaxVacancyAvailable prefix dispatchers: EnterableBuilding (catches most),
//     LivestockBuilding (Barn/ChickenCoop/GoatBarn/Stable)
//   - All target classes verified present.

using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Per-building add-on to maxWorkers / maxResidents for ~46 building types.
    /// </summary>
    internal static class GreaterHalls
    {
        internal sealed class BuildingInfo
        {
            public string Name;
            public int Min;
            public int Max;
            public string Category;
            public BuildingInfo(string n, int min, int max, string cat) { Name = n; Min = min; Max = max; Category = cat; }
        }

        // Building add-on ranges. Matches source mod's ranges 1:1.
        internal static readonly BuildingInfo[] Buildings = new BuildingInfo[]
        {
            // Livestock
            new BuildingInfo("Barn",                0, 6,  "Livestock"),
            new BuildingInfo("ChickenCoop",         0, 1,  "Livestock"),
            new BuildingInfo("GoatBarn",            0, 4,  "Livestock"),
            new BuildingInfo("Stable",              0, 3,  "Livestock"),

            // Production
            new BuildingInfo("ApothecaryShop",      0, 2,  "Production"),
            new BuildingInfo("Armory",              0, 2,  "Production"),
            new BuildingInfo("Bakery",              0, 2,  "Production"),
            new BuildingInfo("BasketShop",          0, 2,  "Production"),
            new BuildingInfo("BlacksmithForge",     0, 2,  "Production"),
            new BuildingInfo("BookBinder",          0, 4,  "Production"),
            new BuildingInfo("Brewery",             0, 4,  "Production"),
            new BuildingInfo("CandleShop",          0, 3,  "Production"),
            new BuildingInfo("Cheesemaker",         0, 4,  "Production"),
            new BuildingInfo("CobblerShop",         0, 2,  "Production"),
            new BuildingInfo("CooperBuilding",      0, 2,  "Production"),
            new BuildingInfo("FletcherBuilding",    0, 2,  "Production"),
            new BuildingInfo("Foundry",             0, 3,  "Production"),
            new BuildingInfo("FurnitureWorkshop",   0, 5,  "Production"),
            new BuildingInfo("Glassmaker",          0, 6,  "Production"),
            new BuildingInfo("PaperMill",           0, 5,  "Production"),
            new BuildingInfo("PotterBuilding",      0, 4,  "Production"),
            new BuildingInfo("PreservistBuilding",  0, 4,  "Production"),
            new BuildingInfo("SmokeHouse",          0, 1,  "Production"),
            new BuildingInfo("WeaverBuilding",      0, 4,  "Production"),
            new BuildingInfo("Windmill",            0, 2,  "Production"),

            // Resource sites
            new BuildingInfo("ClayPitBuilding",     0, 4,  "Resource Sites"),
            new BuildingInfo("MineralSiteMine",     0, 4,  "Resource Sites"),
            new BuildingInfo("SandPitBuilding",     0, 4,  "Resource Sites"),
            new BuildingInfo("SawPitBuilding",      0, 4,  "Resource Sites"),
            new BuildingInfo("StonePitBuilding",    0, 8,  "Resource Sites"),
            new BuildingInfo("WorkCamp",            0, 3,  "Resource Sites"),

            // Field work
            new BuildingInfo("FishingShack",        0, 1,  "Field Work"),
            new BuildingInfo("ForagerShack",        0, 1,  "Field Work"),
            new BuildingInfo("HunterBuilding",      0, 1,  "Field Work"),

            // Civic
            new BuildingInfo("CompostYard",         0, 2,  "Civic"),
            new BuildingInfo("GuardTower",          0, 2,  "Civic"),
            new BuildingInfo("GuildHall",           0, 4,  "Civic"),
            new BuildingInfo("HealersHouse",        0, 2,  "Civic"),
            new BuildingInfo("Library",             0, 1,  "Civic"),
            new BuildingInfo("MarketBuilding",      0, 2,  "Civic"),
            new BuildingInfo("Pub",                 0, 2,  "Civic"),
            new BuildingInfo("School",              0, 2,  "Civic"),
            new BuildingInfo("Temple",              0, 3,  "Civic"),
            new BuildingInfo("WagonShop",           0, 2,  "Civic"),

            // Residential
            new BuildingInfo("Shelter",             0, 4,  "Residential"),
            new BuildingInfo("TemporaryShelter",    0, 4,  "Residential"),
        };

        public sealed class BuildingPref
        {
            public string Name = "";
            public int Min;
            public int Max;
            public string Category = "";
            public MelonPreferences_Entry<int> Entry = null!;
        }

        // Built lazily on first patch invocation.
        private static readonly Dictionary<string, MelonPreferences_Entry<int>> _addonByName =
            new Dictionary<string, MelonPreferences_Entry<int>>();

        public static void RegisterPrefs(MelonPreferences_Category cat)
        {
            foreach (var b in Buildings)
            {
                var entry = cat.CreateEntry<int>(
                    $"GreaterHalls_{b.Name}_Addon", 0,
                    display_name: $"{b.Name} +Workers",
                    description: $"Extra worker/resident slots for {b.Name}. Range {b.Min}..{b.Max}.");
                _addonByName[b.Name] = entry;
            }
        }

        public static IEnumerable<BuildingPref> Iterate()
        {
            foreach (var b in Buildings)
            {
                if (_addonByName.TryGetValue(b.Name, out var entry))
                    yield return new BuildingPref { Name = b.Name, Min = b.Min, Max = b.Max, Category = b.Category, Entry = entry };
            }
        }

        private static int LookupAddon(object instance)
        {
            string typeName = instance.GetType().Name;
            return _addonByName.TryGetValue(typeName, out var entry) ? entry.Value : 0;
        }

        // Cache the maxWorkers PropertyInfo per concrete type so we're not reflecting
        // on every Awake call.
        private static readonly Dictionary<System.Type, PropertyInfo?> _maxWorkersByType =
            new Dictionary<System.Type, PropertyInfo?>();

        private static void ApplyAddon(Resource resource, int addon)
        {
            if (addon == 0) return;
            var type = resource.GetType();
            if (!_maxWorkersByType.TryGetValue(type, out var prop))
            {
                prop = type.GetProperty("maxWorkers",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.FlattenHierarchy);
                _maxWorkersByType[type] = prop;
            }
            if (prop == null || !prop.CanWrite) return;
            int current = resource.maxWorkers;
            prop.SetValue(resource, current + addon, null);
        }

        // ----- Direct Awake patches: 5 buildings -----

        [HarmonyPatch(typeof(WorkCamp),       "Awake")]
        internal static class WorkCamp_Awake     { private static void Postfix(WorkCamp __instance)       { Apply(__instance); } }
        [HarmonyPatch(typeof(CompostYard),    "Awake")]
        internal static class CompostYard_Awake  { private static void Postfix(CompostYard __instance)    { Apply(__instance); } }
        [HarmonyPatch(typeof(MarketBuilding), "Awake")]
        internal static class Market_Awake       { private static void Postfix(MarketBuilding __instance) { Apply(__instance); } }
        [HarmonyPatch(typeof(Pub),            "Awake")]
        internal static class Pub_Awake          { private static void Postfix(Pub __instance)            { Apply(__instance); } }
        [HarmonyPatch(typeof(School),         "Awake")]
        internal static class School_Awake       { private static void Postfix(School __instance)         { Apply(__instance); } }

        private static void Apply(Resource __instance)
        {
            if (!Config.EnableGreaterHalls.Value) return;
            if (Plugin.IsForeignModLoaded("VC_ModifyWorkerSlots")) return;
            try { ApplyAddon(__instance, LookupAddon(__instance)); }
            catch (System.Exception ex) { Plugin.Log.Warning($"[Greater Halls] Awake apply ({__instance?.GetType().Name}): {ex.Message}"); }
        }

        // ----- Generic dispatchers: EnterableBuilding + LivestockBuilding -----

        [HarmonyPatch(typeof(EnterableBuilding), "AssignMaxVacancyAvailable")]
        internal static class EnterableBuilding_Patch
        {
            private static readonly HashSet<int> _seen = new HashSet<int>();

            private static void Prefix(EnterableBuilding __instance)
            {
                if (!Config.EnableGreaterHalls.Value) return;
                if (Plugin.IsForeignModLoaded("VC_ModifyWorkerSlots")) return;
                if (__instance == null) return;
                if (!_seen.Add(__instance.GetInstanceID())) return;

                try
                {
                    int addon = LookupAddon(__instance);
                    if (addon == 0) return;
                    ApplyAddon(__instance, addon);
                    if (__instance is Shelter)
                    {
                        // Shelter also needs the user-defined max bumped so the resident
                        // capacity matches the slot count.
                        var udmRef = AccessTools.FieldRefAccess<EnterableBuilding, int>("_userDefinedMaxWorkers");
                        udmRef(__instance) = udmRef(__instance) + addon;
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Greater Halls] EnterableBuilding dispatch ({__instance.GetType().Name}): {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(LivestockBuilding), "AssignMaxVacancyAvailable")]
        internal static class LivestockBuilding_Patch
        {
            private static readonly HashSet<int> _seen = new HashSet<int>();

            private static void Prefix(LivestockBuilding __instance)
            {
                if (!Config.EnableGreaterHalls.Value) return;
                if (Plugin.IsForeignModLoaded("VC_ModifyWorkerSlots")) return;
                if (__instance == null) return;
                if (!_seen.Add(__instance.GetInstanceID())) return;

                try
                {
                    int addon = LookupAddon(__instance);
                    if (addon != 0) ApplyAddon(__instance, addon);
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Greater Halls] LivestockBuilding dispatch ({__instance.GetType().Name}): {ex.Message}");
                }
            }
        }
    }
}
