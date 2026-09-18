# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Zytadelle: idle cell-defense game. One culture (run) plays out in 35-second cycles until the cell
dies, then banks DNA that flows into permanent levels in the Gene Lab between runs. Built with
.NET MAUI Blazor Hybrid, Windows-only for now.

## Commands

```
dotnet run --project src/Zytadelle.App                       # the game
dotnet build                                                 # build everything (Zytadelle.slnx)
dotnet test tests/Zytadelle.Core.Tests -c Release             # golden-determinism + regression tests
dotnet test tests/Zytadelle.Core.Tests -c Release --filter "Category!=Long"   # skip the two long-running goldens
dotnet run --project benchmarks/Zytadelle.Benchmarks -c Release -- --quick [build]   # ~5s throughput/alloc read, no BenchmarkDotNet wait
```

`Zytadelle.Core.Tests/Determinism` pins the simulation bit-for-bit: same seed, same Gene Lab levels,
same purchases → the same `World` forever, so the performance refactor tracked in the repo's plan can
change *how* the tick runs without ever changing *what* it produces. See its README for the
regenerate-goldens workflow (`ZYTADELLE_UPDATE_GOLDEN=1`) and the Windows-x64 platform pin. Never
regenerate a golden to make a red test green without first understanding why it went red.

`Zytadelle.Benchmarks` measures the same engine with BenchmarkDotNet (`dotnet run ... -c Release`,
no args, for the full suite) plus two non-BDN dev-loop modes: `--quick [build]` for a fast read while
iterating, `--loop [build]` as a stable long-running target to attach `dotnet-trace`/`dotnet-counters`
to. `--compare <baseDir> <diffDir>` diffs two BDN `--exporters json` runs.

Debug env vars for `Zytadelle.App` (Debug builds only, read in `GameHost.Initialise`):
- `ZYTADELLE_STRESS=1` — starts a maxed-out defensive culture at top speed, for frame-budget checks.
- `ZYTADELLE_VIEW=<results|genome|missions|records>` — opens straight into that hub screen with DNA seeded in.

## Architecture

Three projects, in dependency order:

- **`Zytadelle.Balancing`** — every tuning number in the game (constants, curves, modifiers), and
  nothing else. No logic beyond curve math. This is the *only* source of numbers with balancing
  meaning; XML doc comments on its members are not just docs — they're read as input hints wherever
  a tuning tool discovers these numbers by reflection (`GenerateDocumentationFile` is on for that
  reason). Split by topic into folders that are also namespaces: `Enemies/` (kinds, defs, scaling,
  spawn mix), `Infections/`, `Missions/` (one enum/record per file, `MissionBalance` holds numbers +
  the rule table), `Curves/`, `Genes/`. Every gene is its own **public static class**
  (`DamageGene.Cap`, `DamageGene.Price.Exponent`) so a reflection-based tool shows it as its own
  group with stable paths; `GeneRegistry` wraps them in `GeneDefinition` adapters (live delegates,
  not copies) for Core. Logic that reads *save-shaped* data (best-cycle records) does not belong
  here — it lives in `Zytadelle.Core/Progression/InfectionProgress.cs`. A member is excluded from a
  reflection walk only via `[BalanceIdentity]`: identity numbers (an infection's tier) and views
  over numbers reached elsewhere (`GeneRegistry.All`) — the walk does not descend into a marked
  member. Consumers get the Balancing namespaces as global usings from their csproj.
- **`Zytadelle.Core`** — the game engine: simulation (`Sim/`), persistence (`Persistence/`),
  missions, upgrades. Deliberately has no balancing knowledge of its own — it reads everything from
  `Zytadelle.Balancing`. `Sim/Step.cs` is the fixed-timestep tick; read its doc comment for the
  update order and the death-mid-tick contract (time doesn't advance, entities aren't compacted,
  income windows aren't written — the results screen draws exactly the breach frame).
  `Snapshot/FrameEncoder.cs` packs the renderable part of `World` into a little-endian byte buffer;
  its layout is mirrored by hand in `Zytadelle.App/wwwroot/js/arena.js` and the two must change
  together.
- **`Zytadelle.App`** — the game itself, MAUI Blazor Hybrid. `Game/GameHost.cs` owns the save, the
  current `World`, and the frame clock: the browser side drives `Tick(nowMs)` every animation frame,
  which advances the sim by fixed steps (`SimulationBalance.FixedDt`) scaled by `Save.Speed` and
  returns the packed snapshot. UI updates (`UiChanged`) are throttled to ~10/s plus once per player
  action (`Announce()`), decoupled from the per-frame sim/render loop so Blazor doesn't re-render on
  every tick.

`Zytadelle.App` is `net10.0-windows10.0.19041.0`, unpackaged (`WindowsPackageType=None` — MSIX
packaging of .NET 10 Blazor Hybrid has an open regression), `InvariantGlobalization=true` (every
number that reaches CSS or the save is formatted invariantly, so making the whole app invariant
removes the decimal-comma-slip class of bug).

Two more projects sit alongside these three, both reading only `Core` + `Balancing`: `tests/Zytadelle.Core.Tests`
(golden-determinism and regression tests) and `benchmarks/Zytadelle.Benchmarks` (BenchmarkDotNet).
Neither is part of the shipping product's dependency chain above.

## Working across the App ↔ JS boundary

Rendering is split: C# encodes world state as bytes (`FrameEncoder`), JS in
`Zytadelle.App/wwwroot/js/` (`arena.js` plus `render/*.js`) decodes and draws to canvas. Any change
to `FrameEncoder`'s byte layout (field order, sizes, `HeaderSize`/`EnemySize`/`ProjectileSize`/`FxSize`)
requires a matching change in `arena.js`'s decoder — they will silently desync otherwise.

## Working with balancing numbers

Don't add gameplay constants to `Zytadelle.Core` or `Zytadelle.App` — they belong in
`Zytadelle.Balancing` so tuning tools can discover them by reflection. A new static double/int/bool
field there is automatically discoverable unless it's a derived/computed property (no setter,
excluded naturally) or an identity/label value (mark it `[BalanceIdentity]`).
