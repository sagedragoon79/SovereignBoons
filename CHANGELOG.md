# Changelog

All notable changes to Sovereign Boons.

## [Unreleased] — 0.1.0-dev

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
