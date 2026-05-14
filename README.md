# Sovereign Boons

A power-spike mod for [Farthest Frontier](https://store.steampowered.com/app/1044720/Farthest_Frontier/). Sovereign Boons is the **"make my settlement stronger"** pillar of the FF mod constellation — a curated, opt-in collection of features cherry-picked from existing community mods and unified under one toggle list.

## Status

**0.1.0-dev** — scaffolded 2026-05-13. No boons folded yet. See `_research/IMPLEMENTATION_PLAN.md` for the build order.

## Design

- **Every boon is OFF by default.** Players opt in to what they want.
- **One file per boon** in `src/Boons/<Name>.cs`, self-contained.
- **Soft-integrated with Keep Clarity** — if KC is installed, all toggles appear in its settings panel with proper sliders, tooltips, and grouped buckets. Without KC, prefs are still readable from the MelonPreferences `.cfg`.
- **Foreign-mod detection** — if the player has the original standalone source mod loaded, the matching boon stays off even if its toggle is on, to avoid double-patching.
- **Credit preserved** — every boon's source file leads with a `// Folded from <Mod> by <Author>` header.

## Buckets

- **Economy** — tax/income, traveling merchants
- **Workforce** — villager speed, child labor age, road speed
- **Buildings** — work radii, water recharge, temple slots, worker slots, storage, desirability, crops
- **Weather** — extreme-weather suppression, days-per-month
- **Combat** — basic weapon equipment (Mono re-impl)
- **Misc** — achievement unlock with mods, etc.

## Source mods

See `_research/source_mods.md` for the full provenance table.

## Build

```powershell
dotnet build src\SovereignBoons.csproj -c Debug -p:Platform=x64
# or for release:
dotnet build src\SovereignBoons.csproj -c Release -p:Platform=x64
```

Output: `bin\<config>\SovereignBoons.dll`. Auto-staged to `<game>\Farthest Frontier (Mono)\Mods\` on every successful build (MelonLoader releases the file handle, so the copy works even while the game is running).

## License

To be decided. Source mods retain their original licenses — see provenance table.
