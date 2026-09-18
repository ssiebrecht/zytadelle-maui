namespace Zytadelle.Benchmarks;

/// <summary>
/// <see cref="Stats.From"/> runs once per purchase (via <c>World.RefreshStats</c>) and once at
/// <c>World.Create</c> - never per tick, but the in-culture shop's whole feel depends on it not
/// stalling when a policy buys repeatedly.
/// </summary>
[MemoryDiagnoser]
public class StatsFromBench
{
    private Levels _lab = null!;
    private Levels _run = null!;

    [GlobalSetup]
    public void Setup()
    {
        _lab = new Levels();
        _run = new Levels();
        foreach (var id in GeneIds.All)
        {
            _lab[id] = 10;
            _run[id] = 5;
        }
    }

    [Benchmark]
    public Stats Run() => Stats.From(_lab, _run);
}
