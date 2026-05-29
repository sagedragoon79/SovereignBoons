# Changelog

All notable changes to Sovereign Boons.

## v1.0.1 (2026-05-29)

Maintenance + Dog/Cat DLC support, plus Emergency Militia polish.

### Buildings
- **Greater Halls** — added three building types (now covers 49): **Dog Kennel** and **Cat Kennel** (Dog/Cat DLC; ride the LivestockBuilding dispatcher) and the **Firewood Splitter** (`WoodCutterBuilding`, which needed its own `Awake` patch since it derives from `Building`, not `EnterableBuilding`). Added coverage notes to the MineralSiteMine / Clay Pit / Sand Pit tooltips — MineralSiteMine is one class for *all* Iron/Gold/Coal mines (surface and deep); the pit entries cover both surface and deep variants.
- **Domain Expansion** — added **Doghouse Guard Radius ×** (Dog/Cat DLC). Scales the dog's guard/defend radius (vanilla 60) via an `Awake` prefix so the selection ring is sized correctly (no double-ring).

### Combat
- **Emergency Militia** — default hotkeys changed to **Ctrl+A** (mobilize) / **Alt+A** (stand down), replacing `B`/`N` (B was too easy to hit by accident; N is the vanilla road-placement key). Panel labels renamed Mobilize / Stand Down.
- Toast now reads **"Mobilized N Villagers (M Armed)"** — M is a one-time count of weapons in storage (Weapon + SimpleWeapon) at trigger time, so it reflects how many militia can actually equip. No polling/scan. (All mobilized villagers fight regardless; the weapon is an upgrade.)

### Notes
- Existing configs keep their saved `B`/`N` hotkeys — change them in the KC panel (or clear the two lines from `UserData/SovereignBoons.cfg`) to pick up the new Ctrl+A / Alt+A defaults.

---

## v1.0.0 (2026-05-14) — initial release

Power-spike pack for Farthest Frontier. 14 boons across 6 buckets, every one OFF by default, every one curated from a community source mod with credit preserved. Soft-integrates with [Keep Clarity](https://github.com/sagedragoon79/KeepClarity)'s in-game settings panel for tooltips, sliders, grouped buckets, and nested indentation; works without KC installed too (read prefs from `UserData/SovereignBoons.cfg`).

### Economy
- **Crown's Bounty** — multiply gold from tax-collection events (only — sales, refunds, and event rewards are untouched). Folded from TaxGoldgainMono (coos).
- **Wealthy Caravans** — beefier traveling merchants: more gold, more goods, optional buy-anything, higher trading-post stock cap. Folded from TravelingMerchantPlus (coos).

### Workforce
- **Swift Feet** — faster villagers + beefier transport wagons. Folded from FastVillagers (Krasipeace).
- **Eager Hands** — lower child/adolescent age cutoffs and school enrollment range. Folded from ForcedChildLabor (Krasipeace).
- **King's Highway** — faster road travel + slower predators. Inspired by Rapid Roads (Olleus); dropped the source's off-road penalties since SB is power-spike-only.

### Buildings
- **Wetter Wells** — faster Well recharge + bigger capacity. Folded from VC_FasterWaterRecharge (VC).
- **Domain Expansion** — per-building work-radius multipliers for 7 buildings (WorkCamp, Hunter, Fishing, Arborist, Market, ForagerShack, RatCatcher). Folded from VC_BuildingRadiusAdjust (VC); SB wires up two prefs the source declared but never patched.
- **Civic Pride** — multiply DecorativeBuilding desirability radius and bonus. Folded from VC_DesirabilityBuildingsControl (VC).
- **Hallowed Reliquary** — Temple spirituality bonus multiplier + Unchain Relics (one priest activates every assigned relic). Inspired by VC_ModifyTemple (VC); SB's decoupling approach skips the UniverseLib UI dep the source mod needed.
- **Hoarded Stores** — per-storage-type capacity multiplier for 7 storage types. Folded from VC_UserStorageConfig (VC). Vanilla FF now ships per-category min/max quotas natively, so SB only folds the capacity-multiplier piece.
- **Greater Halls** — per-building +Workers / +Residents add-on across 46 building types, grouped by Livestock / Production / Resource Sites / Field Work / Civic / Residential. Folded from VC_ModifyWorkerSlots (VC).
- **Bountiful Fields** — per-crop tuning of fertility/days/weed-injection/frost-die%/heat-die% for all 12 vanilla crops, plus globals (grids-per-farmer, maintenance length). Defaults match real vanilla values per crop (no `-1` sentinels). `Log Vanilla Values` pref dumps the canonical table to MelonLoader.log on demand. Folded from VC_ConfigurableCropFields (VC).

### Weather
- **Temperate Skies** — independently suppress Blizzard / Heatwave / All-extreme / Drought. Folded from VC_NoBlizzardAndDrought (VC); polarity inverted from source so the tooltips are readable.

### Combat
- **Emergency Militia** — hotkey-driven militia summon. Default `B` arms every eligible villager (skipping Hunter / Guard / Soldier / Child) with militia combat config + tunable ItemStats buff; `N` unarms. Mono re-implementation of BasicWeaponEquipment (donimuzur, original Il2Cpp-only).

### Misc
- **Achieve Cheese** — achievements unlock even with non-default settings or mods. Folded from FFEnableAchievements (idontcare).

### Architecture
- KC SettingsAPI soft-dep wired with 6 buckets (Economy / Workforce / Buildings / Weather / Combat / Misc) and nested indentation; master toggles ordered above their sub-prefs in the panel.
- Per-boon foreign-mod detection: if the source standalone mod is loaded, the corresponding boon defers automatically (no double-patching).
- Every cfg entry's tooltip explicitly states the default value, range, and power-spike direction.
- `SovereignBoons.Boons.LevysArms.IsArmed(Villager)` — public reflection target for sibling mods (used by Essential Provisions' Self Preservation to skip flee logic for armed militia).

### Diverged from source mods
- **King's Highway**: dropped Rapid Roads' three off-road penalty patches. Power-spike pack, not a balance pack.
- **Hallowed Reliquary**: replaced VC_ModifyTemple's "extra slots + UI rewire" with "Unchain Relics from priest count" — same end state, zero UniverseLib dependency.
- **Bountiful Fields**: tooltip text fixed across all per-crop knobs (source mod's pref descriptions were wrong about vanilla values; SB dumps real vanilla via `Log Vanilla Values`).
- **SeasonTweaker**: not folded. Decompile verification revealed the source mod's primary mechanics are broken against current FF — `TimeManager.DAYS_PER_MONTH` is a `const int`, `Cropfield` doesn't own the day fields, and the maintenance patch targets the wrong class. Bountiful Fields covers the functional pieces.

### Credits
Every fold credits its source author. Full provenance in `_research/source_mods.md`. Special thanks to **VC** (8 source mods), **coos** (2), **Krasipeace** (2), and **idontcare**, **Modder**, **donimuzur**, **Olleus** (1 each).

---

## Pre-release history

The phase-by-phase development log below is kept for archaeology.

## [Unreleased] — 0.5.0-dev (Phase 5 — Combat)

### Added
- **Emergency Militia** (Combat) — Hotkey-driven militia summon. Press a configurable key to arm every eligible villager (skipping Hunters, Guards, Soldiers, and Children) with militia combat config + a tunable ItemStats buff. Mono re-implementation of BasicWeaponEquipment (donimuzur, original was Il2Cpp-only). Default keys: `B` to arm, `N` to unarm. Default stat magnitude: 100 (+100% on every Perc field; the source mod's "powerful" preset was 1000). Buff re-applies on occupation change while armed.

### Not folded — SeasonTweaker (was tentatively planned as Steady Calendar)
- Decompile + Assembly-CSharp verification revealed that SeasonTweaker's primary mechanics don't actually function against current FF:
  - `TimeManager.DAYS_PER_MONTH` is a **`const int`** — compile-time inlined, can't be modified at runtime. The source's `TimeManager.DaysInMonth` write silently no-ops.
  - `Cropfield` doesn't have `daysToMature` / `daysToRot` as instance fields — those live on `VegetableFieldsRecord` (Bountiful Fields already covers this).
  - The maintenance-length patch targets `CropFieldMaintenance` but the real class is `CropfieldMaintenance` (lowercase f), and the property is on `AgricultureManager` (Bountiful Fields' `MaintenanceDays` pref already exposes this).
- The remaining functional piece — scaling `SeasonalComponentBase` subclass day-windows — is about season ordering, not a power-spike feature.
- Decision: not folded. Bountiful Fields covers everything functional. If a global "make all crops grow faster" knob is wanted later, it can be added as a small extension to Bountiful Fields.

### Notes
- All entries default OFF; tunables hidden behind master toggle via KC `VisibleWhen`.
- Foreign-mod kill switches for both source mods.
- 0w/0e build; auto-staged.

### Public API — Emergency Militia interop
- `SovereignBoons.Boons.LevysArms.IsArmed(Villager v)` returns `bool`. Stable signature for sibling mods that need to know which villagers are currently armed by SB. Used by Essential Provisions' Self Preservation to skip flee logic for our militia.

### Limitations of Emergency Militia (v0.6)
- Armed state does not persist across save/load — press the Arm hotkey again after loading.
- itemRequester re-weapon-fetch logic deferred to v0.7. Villagers fight with whatever weapon they already carry; if they have nothing, they fight with fists (but with the huge stat buff, they're still surprisingly tough).
- Unarm reverts ItemStats and meleeAttack flag but leaves `teamDef` set to `guardTowerTeamDefinition` — save reload fully resets if needed.

## [Unreleased] — 0.4.0-dev (Phase 3 — Economy + Roads)

### Added — 2 boons folded
- **Wealthy Caravans** (Economy) — Beefier traveling merchants: more gold, more goods, optional buy-anything, higher trading-post stock cap. Folded from TravelingMerchantPlus (coos). Defaults tamed from source's 5× to 2× (user can crank up). Buy-Anything is its own toggle so the gold/goods buff can ship without unlocking it.
- **King's Highway** (Workforce) — Faster travel on roads + slower aggressive animals. Inspired by Rapid Roads (Olleus). **Diverged from source design** — dropped the off-road penalties for villagers, battering rams, and catapults (Sovereign Boons is a power-spike pack, not a balance/penalty pack). Kept the two patches that favor the player: road-speed boost on `AIGridNode.RecalculateRoadSpeedBonus` and slower `AggressiveAnimal.movementSpeed`.

### Notes
- All entries default OFF; tunables hidden behind master toggles via KC `VisibleWhen`.
- Foreign-mod kill switches active for both sources.
- 0w/0e build; auto-staged.

## [Unreleased] — 0.3.0-dev (Phase 2 — VC family)

### Added — 7 boons folded
- **Domain Expansion** (Buildings) — Per-building work-radius multipliers for 7 buildings (WorkCamp, Hunter, Fishing, Arborist, Market, **ForagerShack, RatCatcher**). Folded from VC_BuildingRadiusAdjust (VC). **Extended beyond source** — the source mod declared Forage/RatCatcher prefs but never wired them; Sovereign Boons wires both.
- **Civic Pride** (Buildings) — Multiplies DecorativeBuilding desirability radius and bonus. Folded from VC_DesirabilityBuildingsControl (VC).
- **Temperate Skies** (Weather) — Independently suppress Blizzard / Heatwave / All-extreme / Drought. Folded from VC_NoBlizzardAndDrought (VC). **Inverted polarity** — `Disable<X>` toggles are easier to read than the source's confusing `Active=false`.
- **Hoarded Stores** (Buildings) — Per-storage-type capacity multiplier for 7 storage types (RootCellar, Granary, Storehouse, StorageDepot, Stockyard, Treasury, Market). Folded from VC_UserStorageConfig (VC). The source mod also exposed per-item-category min/max quotas; vanilla FF has built that in natively since the source was authored, so Sovereign Boons doesn't duplicate it.
- **Greater Halls** (Buildings) — Per-building +Workers / +Residents add-on for 46 building types across 6 categories (Livestock / Production / Resource Sites / Field Work / Civic / Residential). Folded from VC_ModifyWorkerSlots (VC). Replaced source's custom IntCfg struct with flat `MelonPreferences_Entry<int>` per building.
- **Hallowed Reliquary** (Buildings) — Spirituality bonus multiplier + **Unchain Relics**: a single priest activates every assigned relic in the Temple. Inspired by VC_ModifyTemple (VC). **Diverged from source design** — instead of adding extra relic slots beyond vanilla 3 (which requires a UniverseLib UI rewire), we decoupled relic activation from priest count by prefix-patching `Temple.AdjustRelicsBasedOnPriestCount`. Same end state ("my Temple is stronger") with zero UniverseLib dependency.
- **Bountiful Fields** (Buildings) — All 12 vanilla crops × 6 tunables each (Fertility, PlantingDays, MatureDays, WeedLevel, Frost, Heat) + globals (GridsPerFarmerMul, MaintenanceDays). Folded from VC_ConfigurableCropFields (VC). Per-crop Apply toggle so individual crop overrides can be flipped without unsetting the master.

### Notes
- All 7 boons default OFF; all tunables hidden behind their master toggle via KC `VisibleWhen`.
- Foreign-mod kill switches active for every source.
- 0w/0e build; auto-staged to Mods folder.
- Avoided `System.ValueTuple` dependency (not in net46) — used plain class types for dispatch records.

## [Unreleased] — 0.2.0-dev (Phase 1)

### Added — 5 boons folded
- **Achieve Cheese** (Misc) — Achievements unlock with custom settings/mods. Folded from FFEnableAchievements (idontcare).
- **Swift Feet** (Workforce) — Faster villagers + beefier transport wagons. Folded from FastVillagers (Krasipeace).
- **Eager Hands** (Workforce) — Lower child/adolescent labor cutoffs + School enrollment range. Folded from Forced Child Labor (Krasipeace). Uses single static-field write instead of source mod's per-instance Awake patch.
- **Crown's Bounty** (Economy) — Multiplies gold from tax-collection events only. Folded from TaxGoldgainMono (coos). **Narrower than source** — sales/refunds/trade gains untouched, honest to the boon name.
- **Wetter Wells** (Buildings) — Faster Well recharge + bigger Well capacity. Folded from VC_FasterWaterRecharge (VC).

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
