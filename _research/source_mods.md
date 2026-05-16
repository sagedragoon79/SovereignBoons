# Source Mods — Provenance

Every Sovereign Boons feature is folded from a community mod with credit preserved. This table tracks who made what and which boon (if any) maps to it.

Catalog reference: `C:\Users\saged\source\repos\Other\List of Power Spike Mods\_catalog.md` (markdown) and `SovereignBoons_PowerSpike_Catalog.xlsx` (pickable).

| # | Source Mod | Version | Author | DLL | SB Bucket | Boon Name | Status |
|---|------------|---------|--------|-----|-----------|-----------|--------|
| 1 | AnimalSpawner (Mono) | 1.3.0 | paboyafx | AnimalSpawnerMono.dll | — | — | **Deferred → SeedVault** (creator tool) |
| 2 | BasicWeaponEquipment | 3.1.0 | donimuzur | BasicWeaponEquipment.dll | Combat | **Emergency Militia** | Folded (Phase 5) — Mono re-impl. Hotkey-driven (no UI prefab injection). itemRequester weapon-fetch deferred to v0.7. |
| 3 | Fast Villagers | 1.0.4 | Krasipeace | FastVillagers_FF.dll | Workforce | **Swift Feet** | Folded (Phase 1) |
| 4 | FFEnableAchievements | 1.0.0 | idontcare | FFEnableAchievements_FF.dll | Misc | **Achieve Cheese** | Folded (Phase 1) |
| 5 | Forced Child Labor | 1.0.3 | Krasipeace | ForcedChildLabor_FF.dll | Workforce | **Eager Hands** | Folded (Phase 1) |
| 6 | MineralSpawner (Mono) | 1.5.3 | paboyafx | MineralSpawnerMono.dll | — | — | **Deferred → SeedVault** (creator tool) |
| 7 | MoveResource (Mono) | 1.5.1 | paboyafx | MoveResourceMod.dll | — | — | **Deferred → SeedVault** (also overlaps Tended Wilds / FT) |
| 8 | Rapid Roads | 1.0.0 | Olleus | Rapid Roads_FF.dll | Workforce | **King's Highway** | Folded (Phase 3) — narrowed to player-favoring patches (road boost + animal slow); off-road penalties dropped since they nerf the player |
| 9 | SeasonTweaker | 1.2.0 | Modder | SeasonTweaker.dll | — | — | **Not folded.** Source mod's primary targets are broken against current FF: `DAYS_PER_MONTH` is a const, `Cropfield.daysToMature` doesn't exist (lives on data record), maintenance patch hits wrong class. Bountiful Fields already covers the functional pieces. |
| 10 | TaxGoldGainMono | 1.1.0 | coos | TaxGoldgainMono.dll | Economy | **Crown's Bounty** | Folded (Phase 1) — narrowed to TaxCollection gain type only |
| 11 | TravelingMerchantPlus | 1.1.1 | coos | TravelingMerchantPlusMono.dll | Economy | **Wealthy Caravans** | Folded (Phase 3) — defaults tamed (5×→2×); Buy-Anything as a separate toggle |
| 12 | VC_BuildingRadiusAdjust | 1.2 | VC | VC_BuildingRadiusAdjust_FF.dll | Buildings | **Domain Expansion** | Folded (Phase 2) — extended to 7 buildings (added ForagerShack + RatCatcher patches the source declared but never wired) |
| 13 | VC_ConfigurableCropFields | 1.7 | VC | VC_ConfigurableCropFields_FF.dll | Buildings | **Bountiful Fields** | Folded (Phase 2) — all 12 crops, 6 tunables each, plus globals |
| 14 | VC_DesirabilityBuildingsControl | 1.0 | VC | VC_DesirabilityBuildingsControl_FF.dll | Buildings | **Civic Pride** | Folded (Phase 2) |
| 15 | VC_FasterWaterRecharge | 1.0 | VC | VC_FasterWaterRecharge_FF.dll | Buildings | **Wetter Wells** | Folded (Phase 1) |
| 16 | VC_ModifyTemple | 1.4 | VC | VC_ModifyTemple_FF.dll | Buildings | **Hallowed Reliquary** | Folded (Phase 2) — **Diverged from source design.** Replaced "extra slots + UI rewire" with "Unchain Relics" (1 priest activates all relics) to skip the UniverseLib UI dep. BonusMul kept. |
| 17 | VC_ModifyWorkerSlots | 1.3 | VC | VC_ModifyWorkerSlots.dll | Buildings | **Greater Halls** | Folded (Phase 2) — 46 buildings, +Workers add-on per type |
| 18 | VC_NoBlizzardAndDrought | 1.1 | VC | VC_NoBlizzardAndDrought.dll | Weather | **Temperate Skies** | Folded (Phase 2) — polarity inverted (Disable<X> instead of confusing "Active=false means remove") |
| 19 | VC_UserStorageConfig | 1.3 | VC | VC_UserStorageConfig_FF.dll | Buildings | **Hoarded Stores** | Folded (Phase 2) — capacity multiplier per type. Source's per-category Min/Max quotas not folded: **vanilla FF now ships that natively**, so the source mod's feature is redundant. |

## Author summary

- **VC (8 mods):** clean family, shared validator/UI patterns, UniverseLib dep. Fold as one cohesive bucket.
- **paboyafx (3 mods):** creator hotkey overlays — deferred to SeedVault per the constellation plan.
- **Krasipeace (2 mods):** tiny Awake-postfix multipliers. Quick wins.
- **coos (2 mods):** economy multipliers. Pair as one "Economy" group.
- **Olleus / idontcare / Modder / donimuzur (1 each):** Rapid Roads, FFEnableAchievements, SeasonTweaker, BasicWeaponEquipment.

## License posture (placeholder)

Per-author license review pending. None of the source mods include explicit re-use clauses in their DLL metadata — will reach out to authors before public release if needed. Steam Workshop / Nexus listings should be checked for terms.
