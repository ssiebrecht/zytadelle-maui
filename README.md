# Zytadelle

An idle cell-defense game. Play a culture (a run) that plays out in 35-second cycles until the
cell dies. Built with .NET MAUI Blazor Hybrid, Windows-only for now.

## Gameplay

Each run is a single culture defending itself for 35 seconds. When the cell dies, the DNA it
earned banks into permanent levels in the **Gene Lab** — the progression layer that carries over
between runs and makes each successive culture stronger.

## Prerequisites

- .NET 10 SDK
- Windows (WebView2 Runtime — usually already present on current Windows 10/11)

## Running it

```
dotnet run --project src/Zytadelle.App    # the game
dotnet build                              # build everything
```

## Architecture

Three projects, in dependency order:

| Project | Purpose |
|---|---|
| `Zytadelle.Balancing` | Every tuning number in the game (constants, curves, modifiers) — the only source of numbers with balancing meaning. No logic beyond curve math. |
| `Zytadelle.Core` | The game engine: simulation, persistence, missions, upgrades. Deliberately has no balancing knowledge of its own — it reads everything from `Zytadelle.Balancing`. |
| `Zytadelle.App` | The game itself, MAUI Blazor Hybrid. |

Rendering in `Zytadelle.App` is split across a boundary: C# encodes world state as bytes
(`FrameEncoder`), and JavaScript in `wwwroot/js/` decodes and draws it to canvas (`arena.js`).
The byte layout is mirrored by hand on both sides, so a change to one always needs a matching
change to the other.

`Zytadelle.Balancing` is the single source of truth for every gameplay number — nothing balancing-
related lives in `Core` or `App`. Its XML doc comments aren't just documentation: they're read as
input hints wherever a tuning tool discovers these numbers by reflection.

## Upgrade curves

Permanent (DNA) and temporary (ATP) prices are independent power laws:

```
cost(n) = round(FirstCost * (1 + n / Ramp)^Exponent)
```

`n` counts purchases in the corresponding track, starting at zero. ATP prices reset each culture;
permanent levels do not raise them. Opening discounts are baked into `FirstCost`: no price resets,
brackets or extra tail segments. Integer rounding can give equal adjacent prices, never a drop.

Both tracks contribute to the same capped total level. The value increment at purchase index `n`
has weight `(1 + n / Ramp)^Exponent * (1 + amplitude * sin(2π * (n / period + phase)))`.
The prefix sum is normalized to `GainAtCap`, then added to `ValueBase0`. This keeps the previous
cap values, including probability limits, while permitting accelerating increments. Target-count
genes remain exactly +1 per level. Value weights are cached and safely shared by campaign workers.

Price powers exceed increment powers, so marginal value per resource declines in the long term.
Value waves are normally ±18%, phase-shifted between genes, with 10-level periods (24 for the large
stats). The exceptionally steep DNA tracks use shorter waves: 4 levels for critical damage and
3 levels, ±20% for bounce range. These produce actual local efficiency rises despite rising prices.

Caps, unlock costs and starting stats are retained. Last-purchase costs stay within 2% of the
previous tuning; most tracks also retain approximately the level-25 cost. Intermediate values are
intentionally rebalanced. In particular regeneration has a different ramp, and bounce-range DNA
costs ramp much earlier instead of jumping by enormous factors near the cap. Existing saved levels
are retained and revalued under the new curves. Derived lifetime investment, and thus the mission
reward basis, also uses current prices.
