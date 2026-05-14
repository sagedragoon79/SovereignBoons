# Source Mods — Provenance

Every Sovereign Boons feature is folded from a community mod with credit preserved. This table tracks who made what and which boon (if any) maps to it.

Catalog reference: `C:\Users\saged\source\repos\Other\List of Power Spike Mods\_catalog.md` (markdown) and `SovereignBoons_PowerSpike_Catalog.xlsx` (pickable).

| # | Source Mod | Version | Author | DLL | SB Bucket | Boon Name | Status |
|---|------------|---------|--------|-----|-----------|-----------|--------|
| 1 | AnimalSpawner (Mono) | 1.3.0 | paboyafx | AnimalSpawnerMono.dll | — | — | **Deferred → SeedVault** (creator tool) |
| 2 | BasicWeaponEquipment | 3.1.0 | donimuzur | BasicWeaponEquipment.dll | Combat | _TBD_ | **Blocked** (Il2Cpp — needs Mono re-impl) |
| 3 | Fast Villagers | 1.0.4 | Krasipeace | FastVillagers_FF.dll | Workforce | _TBD_ | Pending pick |
| 4 | FFEnableAchievements | 1.0.0 | idontcare | FFEnableAchievements_FF.dll | Misc | _TBD_ | Pending pick |
| 5 | Forced Child Labor | 1.0.3 | Krasipeace | ForcedChildLabor_FF.dll | Workforce | _TBD_ | Pending pick |
| 6 | MineralSpawner (Mono) | 1.5.3 | paboyafx | MineralSpawnerMono.dll | — | — | **Deferred → SeedVault** (creator tool) |
| 7 | MoveResource (Mono) | 1.5.1 | paboyafx | MoveResourceMod.dll | — | — | **Deferred → SeedVault** (also overlaps Tended Wilds / FT) |
| 8 | Rapid Roads | 1.0.0 | Olleus | Rapid Roads_FF.dll | Workforce | _TBD_ | Pending pick |
| 9 | SeasonTweaker | 1.2.0 | Modder | SeasonTweaker.dll | Weather | _TBD_ | Pending pick |
| 10 | TaxGoldGainMono | 1.1.0 | coos | TaxGoldgainMono.dll | Economy | _TBD_ | Pending pick |
| 11 | TravelingMerchantPlus | 1.1.1 | coos | TravelingMerchantPlusMono.dll | Economy | _TBD_ | Pending pick |
| 12 | VC_BuildingRadiusAdjust | 1.2 | VC | VC_BuildingRadiusAdjust_FF.dll | Buildings | _TBD_ | Pending pick |
| 13 | VC_ConfigurableCropFields | 1.7 | VC | VC_ConfigurableCropFields_FF.dll | Buildings | _TBD_ | Pending pick |
| 14 | VC_DesirabilityBuildingsControl | 1.0 | VC | VC_DesirabilityBuildingsControl_FF.dll | Buildings | _TBD_ | Pending pick |
| 15 | VC_FasterWaterRecharge | 1.0 | VC | VC_FasterWaterRecharge_FF.dll | Buildings | _TBD_ | Pending pick |
| 16 | VC_ModifyTemple | 1.4 | VC | VC_ModifyTemple_FF.dll | Buildings | _TBD_ | Pending pick |
| 17 | VC_ModifyWorkerSlots | 1.3 | VC | VC_ModifyWorkerSlots.dll | Buildings | _TBD_ | Pending pick |
| 18 | VC_NoBlizzardAndDrought | 1.1 | VC | VC_NoBlizzardAndDrought.dll | Weather | _TBD_ | Pending pick |
| 19 | VC_UserStorageConfig | 1.3 | VC | VC_UserStorageConfig_FF.dll | Buildings | _TBD_ | Pending pick |

## Author summary

- **VC (8 mods):** clean family, shared validator/UI patterns, UniverseLib dep. Fold as one cohesive bucket.
- **paboyafx (3 mods):** creator hotkey overlays — deferred to SeedVault per the constellation plan.
- **Krasipeace (2 mods):** tiny Awake-postfix multipliers. Quick wins.
- **coos (2 mods):** economy multipliers. Pair as one "Economy" group.
- **Olleus / idontcare / Modder / donimuzur (1 each):** Rapid Roads, FFEnableAchievements, SeasonTweaker, BasicWeaponEquipment.

## License posture (placeholder)

Per-author license review pending. None of the source mods include explicit re-use clauses in their DLL metadata — will reach out to authors before public release if needed. Steam Workshop / Nexus listings should be checked for terms.
