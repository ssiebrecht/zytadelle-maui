namespace Zytadelle.Benchmarks;

/// <summary>
/// What <see cref="Zytadelle.Core.Tests.Determinism.CheapestAffordablePolicy"/> pays every decision
/// point: nineteen <see cref="Purchase.Offer"/> calls, each a <see cref="RunOffer"/> allocation, to
/// find the cheapest affordable gene. This is the cost the future simulator's policies pay on every
/// purchase decision, independent of the tick loop itself.
/// </summary>
[MemoryDiagnoser]
public class CheapestOfferScan
{
    private World _world = null!;

    [GlobalSetup]
    public void Setup() => _world = new Scenario(Build.MidOffense, 1, 1u, int.MaxValue).CreateWorld();

    [Benchmark]
    public double ScanNineteen()
    {
        var best = 0.0;
        foreach (var def in UpgradeCatalog.All)
        {
            var offer = Purchase.Offer(_world, def.Id);
            if (offer.Affordable && offer.Cost is { } cost) best += cost;
        }
        return best;
    }
}
