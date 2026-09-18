namespace Zytadelle.Benchmarks;

/// <summary>
/// The tick floor: an infection with the enemy cap set to zero, so nothing ever spawns, nothing ever
/// dies, and no purchase runs. What is left is pure per-tick bookkeeping - the two timers, the
/// income-window sampling, the no-op compaction checks. Every other benchmark's cost sits on top of
/// this number.
/// </summary>
[MemoryDiagnoser]
public class TickEmptyDish
{
    private World _world = null!;

    [GlobalSetup]
    public void Setup()
    {
        SimulationBalance.MaxEnemies = 0;
        _world = new Scenario(Build.Empty, 1, 1u, int.MaxValue).CreateWorld();
    }

    [Benchmark]
    public void Steps600()
    {
        for (var i = 0; i < 600; i++) Step.Run(_world, SimulationBalance.FixedDt);
    }
}
