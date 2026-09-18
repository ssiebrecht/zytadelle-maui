namespace Zytadelle.Benchmarks;

/// <summary>
/// One field's worth of spawns back to back - roughly what a full wave costs today: one <c>Enemy</c>
/// allocation per call, plus the per-spawn curve reads (<c>Weights</c>, <c>BasicHpAt</c>,
/// <c>BasicAtkAt</c>) the plan's per-cycle cache targets.
/// </summary>
[MemoryDiagnoser]
public class SpawnBurst
{
    private World _world = null!;

    [GlobalSetup]
    public void Setup() => _world = new Scenario(Build.Empty, 1, 1u, int.MaxValue).CreateWorld();

    [Benchmark]
    public void Spawn120()
    {
        _world.Enemies.Clear();
        for (var i = 0; i < 120; i++) Spawning.Spawn(_world, Spawning.PickKind(_world));
    }
}
