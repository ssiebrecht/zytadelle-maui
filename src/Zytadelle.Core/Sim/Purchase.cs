using Zytadelle.Core.Upgrades;

namespace Zytadelle.Core.Sim;

/// <summary>What the in-culture shop shows for one gene.</summary>
/// <param name="Level">Total level, Gene Lab plus this culture.</param>
/// <param name="Cost">Null when the gene is maxed or not buyable with ATP.</param>
public sealed record RunOffer(UpgradeDef Def, int Level, double? Cost, bool Affordable, bool Visible);

public enum BuyResult
{
    Bought,
    Maxed,
    Poor,
    Locked,
}

public static class Purchase
{
    /// <summary>
    /// The price index is the total level from the Gene Lab and this culture. A culture therefore
    /// starts at the ATP price for the level following its permanent level.
    /// </summary>
    public static RunOffer Offer(World w, GeneId id)
    {
        var def = UpgradeCatalog.Def(id);
        var level = Stats.TotalLevel(w.Lab, w.Run, id);
        var visible = def.BuyableInRun && (def.UnlockDna == 0 || w.Unlocked.Contains(id));
        var maxed = GeneRegistry.IsMaxed(id, level);
        double? cost = maxed || !def.BuyableInRun ? null : def.Gene.CostAt(level);
        return new RunOffer(def, level, cost, cost is not null && w.Atp >= cost, visible);
    }

    public static BuyResult Buy(World w, GeneId id)
    {
        var o = Offer(w, id);
        if (!o.Visible) return BuyResult.Locked;
        if (o.Cost is not { } cost) return BuyResult.Maxed;
        if (w.Atp < cost) return BuyResult.Poor;

        w.Atp -= cost;
        w.Run[id]++;
        w.RunBuys++;
        w.RefreshStats();
        return BuyResult.Bought;
    }
}
