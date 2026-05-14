# Changelog

All notable changes to Sovereign Boons.

## [Unreleased] — 0.2.0-dev (Phase 1)

### Added — 5 boons folded
- **Steadfast Resolve** (Misc) — Achievements unlock with custom settings/mods. Folded from FFEnableAchievements (idontcare).
- **Swift Feet** (Workforce) — Faster villagers + beefier transport wagons. Folded from FastVillagers (Krasipeace).
- **Eager Hands** (Workforce) — Lower child/adolescent labor cutoffs + School enrollment range. Folded from Forced Child Labor (Krasipeace). Uses single static-field write instead of source mod's per-instance Awake patch.
- **Crown's Bounty** (Economy) — Multiplies gold from tax-collection events only. Folded from TaxGoldgainMono (coos). **Narrower than source** — sales/refunds/trade gains untouched, honest to the boon name.
- **Spring's Vigor** (Buildings) — Faster Well recharge + bigger Well capacity. Folded from VC_FasterWaterRecharge (VC).

### Notes
- All 5 boons default OFF; every tunable is gated on its master toggle via KC `VisibleWhen`.
- Foreign-mod kill switches in place: if you have the standalone source mod loaded, the matching boon defers.
- Build clean (0 warn / 0 err); auto-staged to Mods folder.

## [Initial scaffold] — 0.1.0-dev

### Added
- Repo scaffolded (`src/Plugin.cs`, `src/Config.cs`, `src/KeepClarityIntegration.cs`, `src/Boons/`).
- KC SettingsAPI soft-dep wired with six buckets (Economy / Workforce / Buildings / Weather / Combat / Misc).
- `_research/IMPLEMENTATION_PLAN.md` — phased plan for folding 17 in-scope source mods.
- `_research/source_mods.md` — credit/provenance table.
- Catalog spreadsheet at `Other/List of Power Spike Mods/SovereignBoons_PowerSpike_Catalog.xlsx`.

### Research
- Full decompiles persisted for all 20 mods at `Other/List of Power Spike Mods/decompiled/`.
- Verification report at `Other/List of Power Spike Mods/decompile_verification.md` — every patch target cross-checked against current Assembly-CSharp.
- Recovered 5 patch attributes that ilspycmd failed to decode (RapidRoads x4 + TravelingMerchantPlus x1).
- Discovered SeasonTweaker's maintenance patch is a silent no-op in modern FF; Sovereign Boons will patch the correct target.
- BasicWeaponEquipment confirmed Il2Cpp-only; Mono port spec written.
