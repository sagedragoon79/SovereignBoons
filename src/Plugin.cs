using System;
using System.Collections.Generic;
using MelonLoader;

[assembly: MelonInfo(typeof(SovereignBoons.Plugin), "Sovereign Boons", "0.5.0", "sagedragoon79")]
[assembly: MelonGame("Crate Entertainment", "Farthest Frontier")]

namespace SovereignBoons
{
    /// <summary>
    /// Sovereign Boons — power-spike pillar of the FF mod constellation.
    ///
    /// Design rules:
    ///   - Every boon is OFF by default. Players opt in to what they want.
    ///   - One file per boon in src/Boons/<Name>.cs, self-contained.
    ///   - Per-boon kill switch at top of each Postfix/Prefix:
    ///         if (!Config.X.Value) return;
    ///   - Foreign-mod detection: if the player has the standalone source mod
    ///     loaded, the matching boon stays off even if its toggle is on.
    ///   - All prefs registered with KC SettingsAPI (soft-dep) for rich panel UX.
    /// </summary>
    public class Plugin : MelonMod
    {
        public static Plugin Instance { get; private set; } = null!;
        public static MelonLogger.Instance Log => Instance.LoggerInstance;

        public static readonly HashSet<string> LoadedForeignMods =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public override void OnInitializeMelon()
        {
            Instance = this;

            Config.Initialize();
            DetectForeignMods();

            KeepClarityIntegration.TryRegisterAll();

            // Boons that apply at init (no scene needed):
            Boons.SteadfastResolve.Apply();

            LoggerInstance.Msg("Sovereign Boons 0.5.0 initialized");
        }

        private void DetectForeignMods()
        {
            // Watched assembly names — populated as boons are folded in. Each
            // new boon appends the original source mod's assembly name here so
            // that boon's kill switch can defer when the original is loaded.
            string[] watchedAssemblies = {
                // VC family
                "VC_BuildingRadiusAdjust",
                "VC_ConfigurableCropFields",
                "VC_DesirabilityBuildingsControl",
                "VC_FasterWaterRecharge",
                "VC_ModifyTemple",
                "VC_ModifyWorkerSlots",
                "VC_NoBlizzardAndDrought",
                "VC_UserStorageConfig",
                // Workforce / movement
                "FastVillagers",
                "ForcedChildLabor",
                "Rapid Roads",
                // Economy
                "TaxGoldgainMono",
                "TravelingMerchantPlusMono",
                // Misc
                "FFEnableAchievements",
                "SeasonTweaker",
                // Combat (Il2Cpp original — Mono re-impl differs in name)
                "BasicWeaponEquipment",
            };

            var watched = new HashSet<string>(watchedAssemblies, StringComparer.OrdinalIgnoreCase);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = asm.GetName().Name;
                if (watched.Contains(name))
                {
                    LoadedForeignMods.Add(name);
                    LoggerInstance.Msg($"Detected peer mod assembly '{name}' — corresponding boon(s) will be skipped.");
                }
            }
        }

        public static bool IsForeignModLoaded(params string[] names)
        {
            foreach (var n in names)
                if (LoadedForeignMods.Contains(n)) return true;
            return false;
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName != "Map")
            {
                Boons.HallowedReliquary.Reset();
                Boons.CivicPride.Reset();
                Boons.LevysArms.Reset();
                return;
            }

            // Apply static-field writes that need a fresh map context:
            Boons.EagerHands.ApplyStatics();
            Boons.BountifulFields.Apply();
        }

        public override void OnUpdate()
        {
            // Hallowed Reliquary captures vanilla ReligionManager bonus and applies
            // its multiplier once GameManager exists. Self-throttles via _applied flag.
            Boons.HallowedReliquary.TryApplyBonusOnce();

            // Levy's Arms hotkey poll.
            Boons.LevysArms.OnUpdate();
        }
    }
}
