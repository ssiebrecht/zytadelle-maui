namespace Zytadelle.Benchmarks;

/// <summary>
/// One complete culture, purchase policy included, run to death or its cycle cap - the actual unit
/// of work the future balancing simulator repeats millions of times. Deliberately does not go
/// through <c>ScenarioRunner</c>: that runner folds a probe into a hash every tick for the
/// determinism goldens, and that cost has nothing to do with what the simulator itself will pay.
/// Bytes/op here is the number that matters most for a parallel-worlds search.
/// </summary>
[MemoryDiagnoser]
public class FullRunToDeath
{
    private const int Tier = 2;
    private const int MaxCycles = 30;
    private const int PolicyEveryTicks = 30;

    /// <summary>Stress is excluded: it never buys and never dies, so it would just always hit the cap
    /// with no purchase-policy activity - already covered by <see cref="TickFullField"/>.</summary>
    [Params(Build.Empty, Build.MidOffense, Build.BounceMulti)]
    public Build Build { get; set; }

    [Benchmark]
    public RunSummary Run()
    {
        var w = new Scenario(Build, Tier, 1u, MaxCycles).CreateWorld();
        var policy = new CheapestAffordablePolicy(PolicyEveryTicks);
        while (!w.Dead && w.Cycle <= MaxCycles)
        {
            policy.Apply(w);
            Step.Run(w, SimulationBalance.FixedDt);
        }
        return RunSummary.Of(w);
    }
}
