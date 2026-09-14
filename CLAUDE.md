# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Zytadelle: idle cell-defense game. One culture (run) plays out in 35-second cycles until the cell
dies, then banks DNA that flows into permanent levels in the Gene Lab between runs. Built with
.NET MAUI Blazor Hybrid, Windows-only for now.

## Commands

```
dotnet run --project src/Zytadelle.App    # the game
dotnet run --project src/Zytadelle.Lab    # Balance Lab
dotnet build                              # build everything (Zytadelle.slnx)
```

No test projects exist in the repo yet.

Debug env vars for `Zytadelle.App` (Debug builds only, read in `GameHost.Initialise`):
- `ZYTADELLE_STRESS=1` — starts a maxed-out defensive culture at top speed, for frame-budget checks.
- `ZYTADELLE_VIEW=<results|genome|missions|records>` — opens straight into that hub screen with DNA seeded in.

## Architecture

Five projects, in dependency order:

- **`Zytadelle.Balancing`** — every tuning number in the game (constants, curves, modifiers), and
  nothing else. No logic beyond curve math. This is the *only* source of numbers with balancing
  meaning; XML doc comments on its members are not just docs — the Balance Lab reads them at
  runtime as input hints (`GenerateDocumentationFile` is on for that reason). A member is excluded
  from the Lab's reflection walk only via `[BalanceIdentity]` (e.g. an infection's tier number) —
  see `BalanceIdentityAttribute.cs`.
- **`Zytadelle.Core`** — the game engine: simulation (`Sim/`), persistence (`Persistence/`),
  missions, upgrades. Deliberately has no balancing knowledge of its own — it reads everything from
  `Zytadelle.Balancing`. `Sim/Step.cs` is the fixed-timestep tick; read its doc comment for the
  update order and the death-mid-tick contract (time doesn't advance, entities aren't compacted,
  income windows aren't written — the results screen draws exactly the breach frame).
  `Snapshot/FrameEncoder.cs` packs the renderable part of `World` into a little-endian byte buffer;
  its layout is mirrored by hand in `Zytadelle.App/wwwroot/js/arena.js` and the two must change
  together.
- **`Zytadelle.Lab.Engine`** — plain C#, no UI. Tuning-set diffing (`Tuning/`) and balance search
  (`Search/`): a Cross-Entropy priority search or an exhaustive grid over a chosen gene subset,
  simulating campaigns and returning the best builds. `Tuning/TuningSchema.cs` discovers every
  tunable number in `Zytadelle.Balancing` by reflection (public static member, leaf is
  double/int/bool with a setter, walk stays inside the project) rather than a hand-maintained list —
  read its doc comment before touching how a field is read or written back, especially around
  `Bracket`/`LogCurve` (readonly record structs — reflection writes back innermost-first).
- **`Zytadelle.Lab`** — MAUI Blazor Hybrid UI over `Zytadelle.Lab.Engine`. Every Balancing number is
  a form field, generated from `TuningSchema`, not manually listed. Produces a diff + C# snippet to
  manually apply back into `Zytadelle.Balancing`.
- **`Zytadelle.App`** — the game itself, MAUI Blazor Hybrid. `Game/GameHost.cs` owns the save, the
  current `World`, and the frame clock: the browser side drives `Tick(nowMs)` every animation frame,
  which advances the sim by fixed steps (`SimulationBalance.FixedDt`) scaled by `Save.Speed` and
  returns the packed snapshot. UI updates (`UiChanged`) are throttled to ~10/s plus once per player
  action (`Announce()`), decoupled from the per-frame sim/render loop so Blazor doesn't re-render on
  every tick.

Both `Zytadelle.App` and `Zytadelle.Lab` are `net10.0-windows10.0.19041.0`, unpackaged
(`WindowsPackageType=None` — MSIX packaging of .NET 10 Blazor Hybrid has an open regression),
`InvariantGlobalization=true` (every number that reaches CSS or the save is formatted invariantly,
so making the whole app invariant removes the decimal-comma-slip class of bug). `Zytadelle.Lab`
links its fonts from `Zytadelle.App/wwwroot/fonts` rather than duplicating them.

## Working across the App ↔ JS boundary

Rendering is split: C# encodes world state as bytes (`FrameEncoder`), JS in
`Zytadelle.App/wwwroot/js/` (`arena.js` plus `render/*.js`) decodes and draws to canvas. Any change
to `FrameEncoder`'s byte layout (field order, sizes, `HeaderSize`/`EnemySize`/`ProjectileSize`/`FxSize`)
requires a matching change in `arena.js`'s decoder — they will silently desync otherwise.

## Working with balancing numbers

Don't add gameplay constants to `Zytadelle.Core` or `Zytadelle.App` — they belong in
`Zytadelle.Balancing` so the Lab can discover and tune them. A new static double/int/bool field
there is automatically a Lab knob unless it's a derived/computed property (no setter, excluded
naturally) or an identity/label value (mark it `[BalanceIdentity]`).
