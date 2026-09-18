using BenchmarkDotNet.Engines;

namespace Zytadelle.Benchmarks;

/// <summary>
/// The worst case: the Stress build run up to a full enemy cap (it never buys, so it stays there),
/// then stepped further. Every enemy is alive and in range, every loop in <c>Combat</c> walks the
/// whole list, and this is as expensive as one tick of the current engine gets.
///
/// <see cref="RunStrategy.ColdStart"/> is required, not cosmetic: <c>_world</c> is one shared,
/// ever-advancing culture, and BDN's default pilot stage adaptively grows the invocations-per-
/// measurement until a batch takes long enough to time - which for a cheap-looking tick means
/// hundreds of thousands of calls, driving this Stress culture (no offense, no purchases) straight
/// through to death from enemy-scaling alone. Every call after that hits <c>Step.Run</c>'s
/// <c>if (w.Dead) return;</c> and looks like a ~2 ns no-op with zero allocations - which is exactly
/// the wrong number to freeze as a baseline. ColdStart measures one un-batched invocation per
/// iteration, so the culture only ever advances by exactly 600 ticks between measurements.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 5, iterationCount: 15)]
public class TickFullField
{
    private World _world = null!;

    [GlobalSetup]
    public void Setup()
    {
        _world = new Scenario(Build.Stress, 1, 1u, int.MaxValue).CreateWorld();
        var guard = 0;
        while (_world.Enemies.Count < SimulationBalance.MaxEnemies)
        {
            Step.Run(_world, SimulationBalance.FixedDt);
            if (++guard > 200_000) throw new InvalidOperationException("Stress build never reached MaxEnemies.");
        }
    }

    [Benchmark]
    public void Steps600()
    {
        for (var i = 0; i < 600; i++) Step.Run(_world, SimulationBalance.FixedDt);
    }
}
