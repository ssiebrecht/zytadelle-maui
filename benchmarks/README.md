# Zytadelle.Benchmarks

Measures `Zytadelle.Core`'s engine with BenchmarkDotNet. See the root `CLAUDE.md` for the actual
commands (`--quick`, `--loop`, `--compare`) - this file just tracks *why* the numbers moved.

## Why this exists

A future headless balancing simulator (`Zytadelle.Sim`, not built yet) will run millions of ticks
across many parallel `World` instances to search the balance space. There, single-thread
throughput and allocation count matter directly: every allocation is GC pressure shared across
every other World running at the same time. `FullRunToDeath`'s bytes/op is the number that maps
most directly onto that future workload - one complete culture, purchase policy included, run to
death or its cycle cap, which is the unit of work the simulator repeats.

## Baselines

Each `baseline/<date>-<sha>/` directory is a `--filter '*' --exporters json` run, frozen at the
commit named in its own directory name - `<sha>` is the last commit whose `src/` changes the
numbers in it reflect, not necessarily the commit that added the directory.

- **`20260918-ff12ae3`** - pre-refactor. Frozen before any of the performance plan's work landed
  (Phase 0 only added test/benchmark infrastructure, no `src/` changes), so this is what the engine
  cost before Enemy/Projectile were classes-in-a-List, before any per-cycle caching, before the
  purchase-policy scan stopped allocating a record per gene.
- **`20260918-332b830`** - Phase 1 complete (steps 1-9; step 10's only mandatory item, the
  `FrameEncoder` `[ThreadStatic]` scratch, had already landed in Phase 0; its optional `Cell`-struct
  part was skipped as not worth the complexity for one allocation per `World`).

## Phase 1 complete vs. pre-refactor

```
dotnet run --project benchmarks/Zytadelle.Benchmarks -c Release -- --compare benchmarks/baseline/20260918-ff12ae3 benchmarks/baseline/20260918-332b830
```

| benchmark | base ns | new ns | time Δ | base B | new B | bytes Δ |
|---|---:|---:|---:|---:|---:|---:|
| CheapestOfferScan.ScanNineteen | 491.1 | 195.9 | -60.1% | 944 | 32 | -96.6% |
| FullRunToDeath(BounceMulti) | 4,981,343.0 | 3,524,564.5 | -29.2% | 2,605,544 | 145,984 | -94.4% |
| FullRunToDeath(Empty) | 15,596.4 | 8,968.5 | -42.5% | 15,216 | 57,480 | +277.8% |
| FullRunToDeath(MidOffense) | 2,494,263.9 | 1,677,553.5 | -32.7% | 1,535,760 | 114,416 | -92.5% |
| SpawnBurst.Spawn120 | 8,907.4 | 2,852.6 | -68.0% | 14,400 | 0 | -100.0% |
| StatsFromBench.Run | 136.5 | 114.0 | -16.5% | 200 | 200 | 0.0% |
| TickEmptyDish.Steps600 | 12,332.5 | 6,969.2 | -43.5% | 0 | 0 | 0.0% |
| TickFullField.Steps600 | 1,247,100.0 | 908,700.0 | -27.1% | 35,160 | 0 | -100.0% |

Every ongoing per-tick and per-spawn/per-shot allocation is gone (`SpawnBurst`, `TickFullField`:
0 bytes). `FullRunToDeath`'s two builds that actually play for more than one cycle
(`MidOffense`, `BounceMulti`) show the compounding effect of both: the struct conversion (step 8)
*and* the purchase-policy scan no longer allocating a `RunOffer` per gene per decision (step 9) -
which turned out to be the larger of the two for these builds, since a real culture makes many
purchase decisions across its life.

`FullRunToDeath(Empty)`'s +277.8% is not a regression: Empty dies in cycle 1 at Tier 2, so its
total allocation is a few tens of KB, dominated by the two `StructList` backing arrays' one-time
upfront sizing (`Enemies` capacity `MaxEnemies+1`, `Projectiles` capacity `256` - roughly 40 KB
combined). That is a fixed, one-time cost per `World`, paid once regardless of how long the culture
lives; every build that lives past its first cycle amortizes it into nothing, which is exactly what
the table's other five-to-six-figure-tick rows show.

`CheapestOfferScan`'s remaining 32 B is `UpgradeCatalog.All`'s own `foreach` cost (declared
`IReadOnlyList<UpgradeDef>`, so the array's enumerator is boxed through the interface) - pre-existing
and unrelated to the purchase-path work in step 9, simply invisible until the far larger cost that
used to dwarf it was removed.

## Profiling recipe

`--loop stress` is a stable, long-running target (unlike a single BDN invocation) to attach a
profiler to:

```powershell
dotnet run --project benchmarks/Zytadelle.Benchmarks -c Release -- --loop stress
```

While it runs, in another shell:

```powershell
dotnet-counters monitor --process-id <pid> --counters System.Runtime[alloc-rate,gen-0-gc-count,time-in-gc]
dotnet-trace collect --process-id <pid> --profile cpu-sampling --format speedscope
```

After the Phase 1 refactor, the expected top self-time frames are `Combat.UpdateEnemies`,
`Combat.UpdateProjectiles`, `Targeting.NearestInRange`/`NearestFrom` and `Math.Sqrt` - and no
`List<T>` frames anywhere in the tick path, since `Enemies`/`Projectiles` are `StructList<T>` now.
An `alloc-rate` at or near zero while the loop is in its steady state (past the initial spawn burst
to `MaxEnemies`) is the actual pass/fail signal for this plan's stated goal; `dotnet-counters`
catches a live regression that a one-shot benchmark's per-op average can hide.
