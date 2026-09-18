namespace Zytadelle.Core.Tests.Determinism;

/// <summary>
/// A deterministic in-run buyer for the goldens: every <see cref="everyTicks"/> ticks it repeatedly
/// buys the cheapest affordable gene, in catalog order on a tie, until nothing more is affordable.
/// It draws no random numbers, so it can never perturb the simulation's own RNG sequence - only the
/// world state a purchase actually changes (Atp, Run levels, Stats) is affected.
/// </summary>
public sealed class CheapestAffordablePolicy(int everyTicks)
{
    private int _tick;

    public void Apply(World w)
    {
        if (++_tick % everyTicks != 0) return;
        while (TryBuyCheapest(w)) { }
    }

    private static bool TryBuyCheapest(World w)
    {
        GeneId? best = null;
        var bestCost = double.PositiveInfinity;
        foreach (var def in UpgradeCatalog.All)
        {
            var offer = Purchase.Offer(w, def.Id);
            if (!offer.Affordable || offer.Cost is not { } cost || cost >= bestCost) continue;
            best = def.Id;
            bestCost = cost;
        }
        return best is { } id && Purchase.Buy(w, id) == BuyResult.Bought;
    }
}
