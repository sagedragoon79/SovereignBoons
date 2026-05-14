# Sovereign Boons — Implementation Plan

Draft 1 — 2026-05-13 (pre-verification). Will be amended once `decompile_verification.md` lands at `C:\Users\saged\source\repos\Other\List of Power Spike Mods\decompile_verification.md`.

## Goal

Cherry-pick features from 20 community power-spike mods into a single, opt-in, KC-integrated mod. Every boon is OFF by default. Source authors credited in `_research/source_mods.md`.

## Scope decisions (locked in tonight)

| Source mod | Decision | Rationale |
|---|---|---|
| AnimalSpawner (Mono) | **Out** — defer to SeedVault | Creator hotkey tool, not a settlement upgrade. Constellation plan puts creator tools in SeedVault. |
| MineralSpawner (Mono) | **Out** — defer to SeedVault | Same as above. |
| MoveResource (Mono) | **Out** — defer to SeedVault | Same + overlaps Tended Wilds / Forageable Transplantation (user's own mods). |
| BasicWeaponEquipment | **In, but Mono re-impl** | Il2Cpp DLL won't load on Mono — design is foldable but the patch + UI must be re-written from scratch. Highest implementation cost in the pack. |
| All 16 others | **In** | Fit the power-spike thesis. Phasing below. |

Net scope: **17 boons across 6 buckets.**

## Phasing

Boons are ordered by **lowest-risk / highest-clarity first** so we get an early v0.2 release with real value, then build up. Within a phase, items are individually pickable — user can flip the "x" in the catalog spreadsheet to defer any of them.

### Phase 1 — "Quick wins" (target v0.2.0)
Tiny mods, single-purpose, low risk. Each is roughly 30–80 lines of C# including KC registration.

1. **FFEnableAchievements** (Misc) — one-line direct field write. ~10 lines. _Smallest possible boon. Verifies the scaffold end-to-end._
2. **Fast Villagers** (Workforce) — 2 Awake postfix patches, 3 prefs. ~50 lines.
3. **Forced Child Labor** (Workforce) — 2 Awake postfix patches, 4 prefs. ~60 lines.
4. **Tax Gold Gain** (Economy) — 1 AddGain prefix, 1 pref. ~30 lines. _Verify `ExpenseManager+Gain` struct layout (`tender`, `amount`) — pending decompile verification._
5. **Faster Water Recharge** (Buildings) — 1 Well.Awake patch, 2 prefs. ~40 lines. _Verify Well field names._

Phase 1 release: **5 boons, all default-off, fully KC-integrated.** This is shippable on its own.

### Phase 2 — "VC family core" (target v0.3.0)
The VC mods share author + patterns and pull-fold cleanly together. Group these into one PR.

6. **Building Radius Adjust** (Buildings) — 7 building Awake postfixes, 7 percent-multiplier prefs. ~120 lines.
7. **Worker Slots** (Buildings) — ~45 building Awake postfixes via dispatcher, 45 add-on prefs. ~250 lines. _Biggest VC mod by pref count. Consider one master toggle + a JSON-encoded sub-map to avoid pref bloat in the cfg._
8. **No Blizzard / Drought / Heatwave** (Weather) — 3 Weather patches, 4 toggles. ~80 lines.
9. **User Storage Config** (Buildings) — Per-type StorageBuilding lifecycle hook, 7 building configs (each with Apply/Mul/Min/Max). ~200 lines. _Most complex VC mod; per-item category limits need careful Item.itemIDByName lookup._

Phase 2 release: **+4 boons (9 total).**

### Phase 3 — "Economy + movement" (target v0.4.0)

10. **Traveling Merchant Plus** (Economy) — TradeWagon.Init + IsBuyingItem + TradeManager.MaxStock. 3 prefs + a "buy anything" toggle. ~100 lines. _Verify `TradeWagon.Init` signature and `TradeManager.MaxTradingPostStockCount` typeof (pending decompile)._
11. **Rapid Roads** (Workforce) — 5 patches, hardcoded values — convert to prefs (road bonus, off-road penalty, ram/catapult off-road penalty, animal slow). ~120 lines. _Verify `AIGridNode.RecalculateRoadSpeedBonus`, `BatteringRam`/`Catapult` off-road getter names (pending decompile)._

### Phase 4 — "Buildings deep cuts" (target v0.5.0)

12. **Desirability Buildings Control** (Buildings) — DecorativeBuilding.Awake prefix, 2 multipliers (radius, bonus). ~50 lines.
13. **Modify Temple** (Buildings) — Temple.Awake + UISubWidgetTempleControls.Init + UI rewire for extra slot row. ~200 lines including UI work. _Tier-2 complexity due to UI clone of relic slot prefab and second-row layout._
14. **Configurable Crop Fields** (Buildings) — Per-crop tuning of 12 crop types + globals. Uses FieldRefAccess into AgricultureManager. ~300 lines. _Biggest non-VCWorkerSlots fold; consider whether to expose every per-crop tunable or collapse to a smaller set._

### Phase 5 — "Time + combat" (target v0.6.0)

15. **Season Tweaker** (Weather) — Days_Per_Month scaling + crop-day scaling via dynamic Harmony patch on `CropFieldMaintenance.maintenanceLengthInDays`. ~150 lines. _Must run with MelonPriority(-1) so ConfigurableCropFields applies its values first, like the original. **Document the interop ordering note** in the boon's source-file header._
16. **Basic Weapon Equipment** (Combat) — **Full re-implementation for Mono.** UI buttons (per-villager + town-center "arm all"), Villager.ChangeOccupation hook, ItemStats application. ~300 lines incl. UI. _Highest risk in the pack — re-derive everything from the Il2Cpp decompile._

## Cross-cutting concerns

### Save/load
Several mods write to fields that may or may not persist through save/reload. Pattern from existing repos: apply on `Awake` for per-building patches (re-runs on load), use `ApplyBuildingDataDelayedPass` for `BuildingData` mods. Verify per-boon during impl.

### Foreign-mod detection
Per `Plugin.DetectForeignMods`, if a user has the standalone source mod loaded (e.g. `VC_BuildingRadiusAdjust_FF.dll`), the corresponding boon defers. Each fold appends its source assembly name to the watched list.

### Naming
Each boon gets a **two-word medieval/descriptive name** for the KC UI (per `reference_ep_voice.md` precedent from EP). Working candidates:
- FFEnableAchievements → **Steadfast Resolve** (achievements stay yours)
- Fast Villagers → **Swift Feet** _or_ **Fleet Footing**
- Forced Child Labor → **Early Bloom** _or_ **Eager Hands**
- Tax Gold Gain → **Crown's Bounty**
- Faster Water Recharge → **Spring's Vigor**
- Building Radius → **Long Reach**
- Worker Slots → **Greater Halls**
- No Blizzard/Drought → **Temperate Skies**
- Storage Config → **Hoarded Stores**
- Traveling Merchant → **Wealthy Caravans**
- Rapid Roads → **King's Highway**
- Desirability → **Crown Jewels**
- Modify Temple → **Hallowed Reliquary**
- Configurable Crops → **Bountiful Fields**
- Season Tweaker → **Steady Calendar**
- Basic Weapon Equipment → **Levy's Arms** _or_ **Citizens-at-Arms**

(Final names: user picks tomorrow.)

### Versioning checkpoint
Tag a release at the end of each phase. Phase-1 release goes to GitHub draft (no Workshop push until user signs off).

## Pre-impl gates (must pass before Phase 1 starts)

- [ ] `decompile_verification.md` complete (background agent in flight as of 2026-05-13).
- [ ] User reviews catalog xlsx; cherries flagged with "x" in column A.
- [ ] User picks names (or accepts working candidates above).
- [ ] License posture decided per source author (see `source_mods.md`).
- [ ] GitHub repo created (`sagedragoon79/SovereignBoons` private until v0.2 release).

## Estimate

If everything checks out in verification:
- **Phase 1 (5 boons):** ~2–3 evenings of work.
- **Phase 2 (VC family):** ~3–4 evenings.
- **Phase 3 (Economy + Roads):** ~2 evenings.
- **Phase 4 (Buildings deep cuts):** ~4–5 evenings (Temple UI is the slow part).
- **Phase 5 (Season + Combat re-impl):** ~3–4 evenings (BasicWeaponEquipment re-impl is the slow part).

**Total: ~14–18 evenings of work to fold all 16 in-scope mods.** v0.2 (Phase 1) is the natural first ship point at the end of week one.

## Open questions for tomorrow

1. **Worker Slots pref bloat** — 45+ entries in one cfg file is unwieldy. Options: (a) ship as-is, (b) collapse into one JSON-encoded string entry, (c) only expose the most-used 10 and leave the rest hidden. _Recommend (c) for v0.3, expand if requested._
2. **Naming** — accept the working candidates above, or pick fresh?
3. **License outreach** — DM each source author for re-use blessing now, or only when public release nears?
4. **Workshop strategy** — single Workshop entry that grows, or one per phase? _Recommend single entry, update notes per phase._
