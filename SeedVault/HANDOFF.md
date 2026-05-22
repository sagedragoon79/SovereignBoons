# SeedVault Handoff Summary

**Repo:** `C:\Users\saged\source\repos\SeedVault\` (no GitHub yet — not pushed)
**Version:** 0.1.0 (in development, P2 partial)
**Last updated:** 2026-05-01

## Goal
A Pangu-style seed previewer + searchable seed database for Farthest Frontier, with a better-looking GUI than Pangu. v1 scope: preview + DB only (no terrain editor — that's v2). Future: companion website to share seeds via mod export.

## Decisions locked
- **Pangu coexistence:** detect Pangu at init; if loaded, disable preview, keep DB features
- **Storage:** JSON in `UserData/SeedVault/seeds.json` + 5 rolling backups; no PNG thumbnails (regen on demand)
- **Tags:** suggested chips + freeform input
- **Per-entry fields:** seed (encoded — already contains terrainSeed/theme/mountain/water), mapSize, themeId, mountain/water values, pacifist, 4 difficulty levels, name, tags[], notes, savedAt
- **UI split:** IMGUI for in-game overlay (small, infrequent); proper Canvas/TMP planned for new-settlement panel (P3+)
- **Config (`[SeedVault]` in MelonPreferences.cfg):**
  - `Enabled` (master kill switch, true) — pattern user wants on all their mods
  - `ToggleOverlayKey` (F8, configurable)
  - `EnablePreview` (true, auto-forced false if Pangu loaded)
  - `AutoBackupOnSave` (true)

## Phase progress
- **P1 — Skeleton + DB ✅** csproj, MelonMod entry, Pangu detect via `MelonMod.RegisteredMelons`, config, DB load on init. **Tested, working.**
- **P2 — In-game overlay ⚠️ partial** F8 hotkey, IMGUI panel showing live seed/params, bookmark dialog with name/tag-chips/freeform-tags/notes. **Loads and captures correctly but has an outstanding bug — see below.**
- **P3 — New-settlement DB panel** not started
- **P4 — Preview worker** (port Pangu's template-scene pattern) not started
- **P5 — Polish, snippet import/export, release** not started

## Files in repo
```
SeedVault.csproj         (.NET 4.7.2, x64, post-build copies to Mods/)
Properties/AssemblyInfo.cs
src/SeedVaultMod.cs      MelonMod entry, OnUpdate hotkey, OnGUI, scene gating
src/Data/SeedEntry.cs    POCO + container
src/Data/SeedDatabase.cs load/save with atomic write + rolling backups + search
src/Data/JsonCodec.cs    hand-rolled JSON ser/deser (replaced JsonUtility)
src/Game/SeedReader.cs   captures live state from SettingsManager statics
src/UI/InGameOverlay.cs  IMGUI panel + modal bookmark dialog
```

## Game integration findings
All map state lives on `SettingsManager` static properties:
- `activeMapSeed` (string, encoded — contains seed+theme+mountain+water via `SettingsManager.SeedToSettings`/`SettingsToSeed`)
- `mapSizeValue` (`TerrainGeneratorController.Size` enum)
- `mapTheme` (`Terrain2Theme` — has `themeID` byte field, confirmed working)
- `mapMountainValue`, `mapWaterValue` (float 0–1)
- `pacifistMode`, `raiderDifficultyValue`, `animalDifficultyValue`, `diseaseDifficultyValue`, `startingResourcesDifficultyValue`
- FF has no starting-season option (removed from schema)

## Bugs fixed during P2
1. **`MelonUtils.UserDataDirectory` is obsolete-as-error** in MelonLoader 0.7.0-beta → switched to `MelonLoader.Utils.MelonEnvironment.UserDataDirectory`
2. **`JsonUtility.ToJson` silently dropped `List<SeedEntry>`** — JSON file kept showing `{ "schemaVersion": 1 }` only despite three successful `Add()` calls. Replaced with hand-rolled `JsonCodec`. Built but **NOT YET TESTED IN-GAME** — needs verification on next launch.

## Outstanding bug — needs first attention next session
**Symptom:** after clicking Save in the bookmark dialog, cursor spins and Windows shows "close app or wait". Log shows the bookmark line printed, so save did complete — the hang is somewhere in the IMGUI/modal teardown. Last attempted fix (uncompiled, build was interrupted):
- Defer the actual `Database.Add` to next `OnUpdate` via a `_pendingSave` field
- Set `_dialogOpen = false` immediately on click
- Reset `_dialogRect` to recenter on next open

Code change is in the repo but `dotnet build` failed with the post-build copy locked (game still running). Next session: close FF, run `dotnet build SeedVault.csproj -c Release -p:Platform=x64`, retest.

## Known unrelated observations
- Pangu_FF.dll is in Mods/ but didn't load under MelonLoader 0.7.0-beta in either test session — not our problem, just means the Pangu-coexistence path is untested
- Transparency tweak applied (alpha 0.55 panel / 0.7 modal background) at user request

## Next steps in order
1. Close FF, build, verify entries actually persist to JSON now
2. Verify Save no longer hangs the cursor
3. Initialize git repo + push to `github.com/sagedragoon79/SeedVault` (deferred from after P1)
4. P3: new-settlement screen panel — needs a Harmony hook on whatever UI shows the seed input. Pangu's decomp at `C:\Users\saged\ClaudeCodeLocalSessions\pangu_decomp.cs` shows reflection-based polling of the seed input field as a reference pattern.
5. P4: port Pangu's `EnsureSeedPreviewWorkerReady` / `TryGenerateHeightNoiseForSeedCoroutine` template-scene pattern for real preview rendering
6. P5: import/export snippet, README/CHANGELOG, v1.0.0 release

## Reference paths
- Pangu decomp (already saved): `C:\Users\saged\ClaudeCodeLocalSessions\pangu_decomp.cs` (11199 lines)
- MelonLoader log: `G:\SteamLibrary\steamapps\common\Farthest Frontier\Farthest Frontier (Mono)\MelonLoader\Latest.log`
- DB path: `G:\SteamLibrary\steamapps\common\Farthest Frontier\Farthest Frontier (Mono)\UserData\SeedVault\seeds.json`
