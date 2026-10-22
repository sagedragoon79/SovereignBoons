// Original Sovereign Boons feature (not folded from a source mod).
//
// Removes the "you can only build N of these" cap that some buildings carry
// (BuildingData.maxAllowed, e.g. the monument limit).
//
// Verified targets (decompiled Assembly-CSharp, 2026-06-10 build):
//   - BuildingData.enforceMaxAllowed / maxAllowed are public fields.
//   - Every limit check gates on `maxAllowed > 0 && enforceMaxAllowed`:
//       * BuildManager.Start registers capped buildings into
//         buildingNameToMaxAllowedDict (drives BuildingIsMaxed + the toolbar
//         button greying via UIToolbarBuildingButtonManager.OnMaxAllowed).
//       * BuildingPlacer.OnPlaced / BuildingsWindow.BeginBuildingPlacement use
//         it to drop placement stickiness at the cap.
//   - FF's own "Free Buildings" custom setting skips the same registration,
//     so an uncapped town is a path the game already supports.
//   - FFModManager.PostLoadFFMods (only caller: top of BuildManager.Start)
//     can create BuildingData for data mods, so we apply AFTER it and BEFORE
//     the registration loop.
//
// BuildingData lives on a ScriptableObject that survives scene loads, so the
// original flags are recorded and restored when the boon is turned off.

using System.Collections.Generic;
using HarmonyLib;

namespace SovereignBoons.Boons
{
    /// <summary>
    /// Lets the player build any number of buildings that vanilla limits to
    /// one (or a few) per settlement.
    /// </summary>
    internal static class RoyalCharter
    {
        // BuildingData -> original enforceMaxAllowed, for buildings we uncapped.
        private static readonly Dictionary<BuildingData, bool> _originals = new Dictionary<BuildingData, bool>();
        private static bool _loggedThisScene;

        public static void Reset() => _loggedThisScene = false;

        private static void Apply()
        {
            var all = GlobalAssets.buildingSetupData?.buildingData;
            if (all == null) return;

            if (!Config.EnableRoyalCharter.Value)
            {
                // Restore anything we lifted earlier this session.
                if (_originals.Count == 0) return;
                foreach (var kv in _originals)
                    if (kv.Key != null) kv.Key.enforceMaxAllowed = kv.Value;
                Plugin.Log.Msg($"[Royal Charter] Disabled — restored build limits on {_originals.Count} building(s).");
                _originals.Clear();
                return;
            }

            var lifted = new List<string>();
            foreach (var bd in all)
            {
                if (bd == null || bd.maxAllowed <= 0) continue;
                if (!_originals.ContainsKey(bd)) _originals[bd] = bd.enforceMaxAllowed;
                if (bd.enforceMaxAllowed)
                {
                    bd.enforceMaxAllowed = false;
                }
                lifted.Add($"{bd.identifier} (max {bd.maxAllowed})");
            }

            if (!_loggedThisScene)
            {
                _loggedThisScene = true;
                Plugin.Log.Msg(lifted.Count == 0
                    ? "[Royal Charter] No build-limited buildings found."
                    : $"[Royal Charter] Build limits lifted on {lifted.Count} building(s): {string.Join(", ", lifted.ToArray())}");
            }
        }

        // Normal path: data mods are loaded, the limit registration hasn't run yet.
        [HarmonyPatch(typeof(FFModManager), nameof(FFModManager.PostLoadFFMods))]
        internal static class Patch_PostLoadFFMods
        {
            private static void Postfix()
            {
                try { Apply(); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Royal Charter] Apply failed: {ex.Message}"); }
            }
        }

        // Belt-and-braces: if a future FF build moves PostLoadFFMods, this still
        // runs before BuildManager.Start's registration loop.
        [HarmonyPatch(typeof(BuildManager), "Start")]
        internal static class Patch_BuildManagerStart
        {
            private static void Prefix()
            {
                try { Apply(); }
                catch (System.Exception ex) { Plugin.Log.Warning($"[Royal Charter] Apply failed: {ex.Message}"); }
            }

            // Monument-gated items (the "extravagant" decor). Start has just built
            // buildingNameToPrereqsDict: key = prerequisite identifier string
            // ("A|B" = any of), value = the Prerequisite slots waiting on it.
            // Marking a slot met fires the game's own unlock callback when it was
            // the item's last unmet requirement — the same thing that happens when
            // a monument finishes, and what vanilla ClearBuildingPrereqs does for
            // Free Buildings. Removing the key keeps a demolished monument from
            // re-locking them.
            private static void Postfix(BuildManager __instance)
            {
                if (!Config.EnableRoyalCharter.Value || !Config.RoyalCharterUnlockMonumentDecor.Value) return;
                try
                {
                    var dict = Traverse.Create(__instance)
                        .Field("buildingNameToPrereqsDict")
                        .GetValue<Dictionary<string, List<Prerequisite>>>();
                    if (dict == null) return;

                    var keys = new List<string>();
                    foreach (var key in dict.Keys)
                        if (IsMonumentRequirement(key)) keys.Add(key);

                    var unlocked = new List<string>();
                    foreach (var key in keys)
                    {
                        foreach (var p in dict[key])
                        {
                            p.met = true;
                            var group = Traverse.Create(p).Field("group").GetValue<object>();
                            var id = group == null ? null : Traverse.Create(group).Field("identifier").GetValue<string>();
                            if (!string.IsNullOrEmpty(id) && !unlocked.Contains(id!)) unlocked.Add(id!);
                        }
                        dict.Remove(key);
                    }

                    Plugin.Log.Msg(unlocked.Count == 0
                        ? "[Royal Charter] No monument-gated buildings found."
                        : $"[Royal Charter] Monument requirement lifted for: {string.Join(", ", unlocked.ToArray())}");
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.Warning($"[Royal Charter] Monument unlock failed: {ex.Message}");
                }
            }

            private static bool IsMonumentRequirement(string key)
            {
                if (string.IsNullOrEmpty(key)) return false;
                foreach (var part in key.Split('|'))
                    if (part.StartsWith("Monument", System.StringComparison.Ordinal)) return true;
                return false;
            }
        }
    }
}
