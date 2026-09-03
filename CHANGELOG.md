# Changelog

All notable changes to Sovereign Boons.

## v1.1.2 (2026-07-26)

### Fixed — crop rotations vanishing on reload (data loss)
- **Root cause:** vanilla validates every scheduled crop bar on load — if a crop's
  `planting + mature + rot` day total no longer fits the bar's saved width, it **wipes the entire
  year's rotation** (valid bars included). With Bountiful Fields day-overrides active (typically
  *lowering* MatureDays), bars scheduled under the shortened values could be re-validated against
  vanilla's longer totals — BF's overrides applied at scene-init, which can land *after* the
  load-time validation — so affected years blanked on every reload. This was a latent Bountiful
  Fields bug that predates Bountiful Crops.
- **Fix 1 (ordering):** per-crop overrides now also apply immediately after game data loads —
  before any save deserializes — so bars always validate against the same numbers they were
  scheduled with.
- **Fix 2 (wipe protection):** when Bountiful Fields or Bountiful Crops is enabled, a bar that
  genuinely no longer fits (you changed the sliders after scheduling) is now **kept** — it simply
  harvests late or rots, which the game already handles — instead of destroying its whole year.
  The log notes each protected bar.
- Rotations already blanked by the old behavior are gone from those saves (the wipe was saved);
  re-schedule them once — they'll survive from now on.

### Under the hood
- Drover's Market (sell livestock to traders) source is included but OFF by default and not yet
  announced — coming in a future release after testing.

---

## v1.1.1 (2026-07-26)

### Fixed
- **Bountiful Crops picker layout** — with all eight crops enabled, the last icons could overflow
  past the panel edge (three icons crammed into one row, or an extra row hanging below the panel
  background). Crop icons now fill rows two-wide, overflow into dedicated rows inserted above the
  maintenance/clover row, and the popup background grows one row-height per added row so everything
  stays inside the panel. Fully automatic for any future roster size.

---

## v1.1.0 (2026-07-26)

### Added — 🌾 Bountiful Crops: EIGHT new farmable crops
The biggest content addition in SB's history: a full custom-crop engine (proven end-to-end in a
standalone spike, then folded in under Bountiful Fields). One master toggle
(`Bountiful Crops — 8 New Crops`, OFF by default) adds eight new crops to the crop-field planting
picker, each with its own soil personality, climate profile, harvest chain, and hand-drawn icon
(embedded in the DLL; a same-named `SBCrop_*.png` in Mods overrides for custom art):

| Crop | Harvest | Yield | Soil | Personality |
|---|---|---|---|---|
| **Peppers** | Spice | 10:1 (dried & ground) | sandiest in game (0.60–0.85) | frost-doomed luxury; FF's first domestic Spice source |
| **Monk's Comfort** | Medicinal Roots | 3:1 | clay (0.15–0.40) | hardy monastery herb; first cultivated healer supply |
| **Hemp** | Fibers | 1:1 | clay | fast fiber via the native flax chain |
| **Soybeans** | Nuts | 1:1 | mid (Bean companion) | gentlest crop on soil (nitrogen-fixing legume) |
| **Corn** | Grain | 1:1 | mid (Wheat companion) | the tender late-window grain; thirstiest grain. It's got the juice! |
| **Purple Willow** | Willow | 1:1 | clay | frost-proof wetland osier; hates drought & heat |
| **Cremini** | Mushrooms | 1:1 | anywhere damp (wide plateau) | fastest crop (45d); wilts in heat |
| **Herb Garden** | Herbs | 3:1 | mid (Clover companion) | **restores fertility while growing** — the cover crop that pays |

- **Field ecology by design:** sandy fields rotate Flax↔Peppers, clay fields rotate Monk's
  Comfort↔Hemp↔Willow, grain fields run Wheat/Rye early → Corn late (same soil, opposite windows,
  no shared diseases), Cremini fills any damp gap, Herb Garden rests the soil.
- **Full Bountiful Fields integration:** all eight appear in the per-crop tuning sliders
  (fertility/days/weeds/frost/heat) alongside the twelve vanilla crops — twenty crops tunable.
- **Item rename:** the Flax *item* is now displayed as **"Fibers"** everywhere (storage, trade,
  tooltips) since both Flax and Hemp produce it. Crop names unchanged.
- **Save-safe toggle:** turning Bountiful Crops OFF only removes the picker icons — crops already
  planted in a save keep growing, harvesting, and loading normally.
- ⚠️ **Uninstall note:** clear/harvest Bountiful Crops from your fields before removing Sovereign
  Boons entirely — a save referencing the crops without the mod installed will lose those fields.

### Under the hood (for fellow modders)
Custom crops require patching three separate hardcoded walls in FF: the harvest-bucket routing
(`PlantResource`'s six-item check), the farmer task table (`GetFarmerSearchDefs`' item-bound
definitions), and the Hungarian matcher's first-def-wins binding (fixed via a commit-time def swap).
Plus load-safety (`GetCropPrefab` has no null check on save-load) and a vanilla window crash
(`OnPlantDataChanged` indexes drag prefabs by typeID unbounded). All documented in the repo.

---

## v1.0.5 (2026-05-29)

### Fixed
- **Achieve Cheese** now keeps the in-game achievements window usable with custom settings, not just the Steam-side unlocks. The game gates achievements two inconsistent ways: the unlock/stat paths honor `SettingsManager.allowCustomSettingsForAchievements` (which the boon set), but `UIAchievements.OnOpened` gates purely on `AreCustomGameOptionsSet(cheats:true)` and never reads that field — so with custom settings the achievements window greyed itself out even though achievements were unlocking. The boon now also patches `AreCustomGameOptionsSet` to return false for the achievement-context calls (all of which pass `allowAchievementsWithCheats: true`), uniformly neutralizing the custom-options gate across both the unlock paths and the UI window. Start-screen "Custom map" display calls (which use the `false` default) are left untouched. *Verified: window is disabled with the boon off, browsable with it on, under custom settings.*

---

## v1.0.4 (2026-05-29)

Adversarial-review hardening pass (19 confirmed findings fixed) plus a Domain Expansion placement-preview fix. Verified in-game before release.

### Critical — save safety
- **Hallowed Reliquary no longer bricks saves.** Vanilla `Temple.Load`/`PostRelocate` do an unguarded indexed write against the persisted `relicSlotCooldowns` count, so an expanded count made saves unloadable once Bonus Slots was reduced / the boon disabled / the mod removed. Fixed three ways: cooldowns are now sized to `max(6, slots)` (was a doubled `newMax*2`), `Temple.Save` trims them to vanilla 6 so new saves stay portable, and `Temple.Load`/`PostRelocate` grow the list to a safe ceiling so already-written saves still load. *Verified: reducing slots on a 15-slot save reloads with no crash.*

### High
- **Temple relic-slot UI** now clones widgets to match the temple's live `maxRelicCount` (idempotent, with a trailing guarantee), fixing an `IndexOutOfRange` when the data layer outran the UI (early-return shortfall or a pooled widget frozen at an old slot count).
- **Unchain Relics** postfix now mirrors vanilla's `isLoading/isRelocating/isUpgrading/isShuttingDown` freeze guard, so it won't mutate slot lists mid-load/teardown.
- **Greater Halls** dispatchers now clear their de-dupe sets on map teardown (`Reset()` wired into `OnSceneWasInitialized`), so loading a different save without restarting can't drop a building's worker add-on via a recycled instance ID.

### Medium
- **Wealthy Caravans** buffs each merchant wagon exactly once — the load-path re-Init no longer compounds gold/goods every reload.
- **Bountiful Fields** global tuning (grids-per-farmer, maintenance length) is deferred to a `gameReadyToPlay` one-shot, so it applies after the AgricultureManager exists instead of silently no-op-ing at scene load.

### Low / polish
- **Hallowed Reliquary master toggle** now cascades: unchecking it zeroes its sub-options (Bonus Slots 0, Unchain/Unlock off, Bonus Mul 1.0), so re-enabling requires deliberately re-setting them.
- **Domain Expansion placement preview** now shows the *expanded* work-area circle while placing (it read the prefab's vanilla radius before, since the prefab never runs the Awake patch).
- KC-registered version is single-sourced from `Plugin.Version` (was a stale hardcoded `0.1.0`); Eager Hands restores vanilla age cutoffs when disabled; cached the Shelter FieldRef and removed a per-frame allocation in the temple sweep; reset `_vanillaBonusPerRelic` per map; corrected a stale header comment.

---

## v1.0.3 (2026-05-29)

Small Greater Halls cleanup.

### Buildings
- **Greater Halls** — added **ArboristBuilding** (Field Work, 0..2). Direct `Awake` postfix (it derives `: Building`, not `: EnterableBuilding`). The 0..2 range is slightly higher than the other Field Work entries (FishingShack / ForagerShack / HunterBuilding at 0..1) because the Arborist works a much larger 100-unit harvest radius. Greater Halls now covers **~52 building types**, with 9 direct Awake postfixes.
- **Greater Halls — alphabetical reorder.** Buildings are now sorted alphabetically **within each category** (Livestock / Production / Resource Sites / Field Work / Civic / Residential). Same buildings, same defaults — just easier to scan the KC panel. Notable shifts: DogKennel and CatKennel slot in between Barn/ChickenCoop/GoatBarn in Livestock; Brickyard and CharcoalKiln slot into the B/C run in Production; ArboristBuilding is now first under Field Work.

---

## v1.0.2 (2026-05-29)

Hallowed Reliquary now actually adds relic slots — the original v1.0.0 design was reduced scope to avoid a UniverseLib dependency, but reviewing VC's actual code revealed UniverseLib was only needed for VC's own config window, not for the slot expansion itself. v1.0.2 ports the real slot expansion (plain `Object.Instantiate`, no dependency) and adds a couple more Greater Halls entries.

### Buildings
- **Hallowed Reliquary** — now expands the Temple's relic slot count via two new Harmony patches:
  - `Temple.Awake` Prefix bumps `_maxRelicCount` and pre-pads `relicSlotCooldowns` (matches VC's `newMax*2` formula).
  - `UISubWidgetTempleControls.Init` Prefix clones the existing `UIRelicSlot` GameObject and lays the extras out in rows of up to 5 (vanilla row stays; extra rows clone the row container as siblings and stack via the parent `VerticalLayoutGroup`).
  - New config: **Bonus Relic Slots** (`HallowedReliquaryBonusSlots`, 0..13, default 4 → 6 total; max 15 total — matches VC's ceiling). Reload-required (applied in `Temple.Awake`).
  - New power-spike config: **Unlock All Relics** (`HallowedReliquaryUnlockAllRelics`, default OFF). When on, calls `ReligionManager.UnlockAllRelics()` once per map load so expanded slot counts have relics to actually fill (FF's vanilla relic pool is ~8).
- **Hallowed Reliquary — Unchain Relics bug fix** — the previous prefix-and-skip implementation mutated `relicSlots`/`disabledSlots` correctly but never invoked `onRelicsChanged`, so the Temple UI stayed stuck on the priest-capped view. Rewrote as a postfix that runs after vanilla balances (which fires its own events), then promotes whatever remains and fires `onRelicsChanged` once. Also added a `ResourceManager.templesRO` sweep in `OnUpdate` so the toggle takes effect on save-load or mid-session change without waiting for a priest change.
- **Greater Halls** — added two more `Building`-derived production buildings that needed their own `Awake` postfixes: **Brickyard** (Brickmaker) and **CharcoalKiln** (Charcoal Maker). Greater Halls now covers **~51 building types**.

### Notes
- `HallowedReliquaryBonusSlots` defaults to 4 (6 total slots) — comfortable single row. Crank to ~6 (8 total) before squeezing; the multi-row UI handles up to 13 (15 total) with the slot icons compressed across stacked rows. FF's vanilla relic pool is ~8 distinct relics, so values much above that just give you empty drawers (unless you also turn on Unlock All Relics with DLC content).
- The slot expansion does **not** require UniverseLib.

---

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
