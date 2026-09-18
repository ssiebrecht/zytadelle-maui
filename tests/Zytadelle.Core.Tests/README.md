# Zytadelle.Core.Tests

Golden-determinism and regression tests for `Zytadelle.Core`. These exist so the performance
refactor in the repo's plan can change *how* the simulation runs without ever changing *what* it
produces: same seed, same Gene Lab levels, same purchase sequence, same `World` at every tick,
forever.

## What is actually pinned

`Determinism/WorldProbe.cs` reads the exact fields listed in its own doc comment, in that exact
order, and nothing else. That field list is the contract. A refactor that changes how `Enemy`,
`Projectile` or `World` store their data only ever changes how `WorldProbe` *reads* them - never
what it reads or the order it reads it in. If a value the sim actually depends on turns out to be
missing from the probe, add it and bump `WorldProbe.Version`, regenerate every golden in the same
commit, and say why in the commit message.

`Determinism/RenderProbe.cs` is a second, separate hash over everything `WorldProbe` deliberately
leaves out because the simulation itself never reads it back: the Fx ring, the income windows, the
exact bytes `FrameEncoder` would send to the browser. A change that is invisible to `WorldProbe` but
would still show up on screen gets caught here instead.

## Running

```powershell
dotnet test tests/Zytadelle.Core.Tests -c Release
```

Long-running scenarios (a 120-cycle Stress run, a 210-cycle MidOffense run - tens of thousands of
ticks each) are tagged so normal iteration can skip them:

```powershell
dotnet test tests/Zytadelle.Core.Tests -c Release --filter "Category!=Long"
```

## Regenerating the goldens

Needed after a *deliberate* balance or engine change - never to make a red test green without
understanding why it went red first.

```powershell
$env:ZYTADELLE_UPDATE_GOLDEN = '1'
dotnet test tests/Zytadelle.Core.Tests -c Release
Remove-Item Env:ZYTADELLE_UPDATE_GOLDEN
dotnet test tests/Zytadelle.Core.Tests -c Release   # must be green - an unverified golden is worse than none
```

Both passes, always. The first only writes; the second is the actual proof that the run reproduces
itself. Commit the changed files under `Golden/` in the same commit as whatever caused them to
change, with a message that says why.

## On a mismatch

`GoldenFile` reports the first checkpoint that disagrees, which lands within
`ScenarioRunner.CheckpointStride` (300) ticks of the real divergence - close enough to bisect by eye
in `Sim/`. It is not a rounding difference: every comparison is on raw bits
(`BitConverter.DoubleToInt64Bits`), so "close" is exactly as wrong as "very different".

## Platform pin

`Math.Sin`, `Math.Cos` and `Math.Pow` (used by the enemy-scaling curves and the spawn-mix curves)
route through the OS's native libm, not a portable .NET implementation. These goldens were
generated on Windows x64 and are only guaranteed bit-identical there. Running them on Linux/macOS or
on Arm64 is expected to diverge on transcendental-function results, not on a bug.

## Thread safety

`Determinism/ThreadSafetyTests.cs` runs one scenario on eight threads at once and checks every
thread produced the identical trajectory hash. This is what actually stands behind the plan's
"many parallel `World`s, `*Balance` statics read-only" model for the future simulator - not just an
assertion in a design doc.
