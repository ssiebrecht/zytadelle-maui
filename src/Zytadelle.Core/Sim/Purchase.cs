using Zytadelle.Balancing;
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
    /// The price index is the level bought during *this culture*, not the total. Gene Lab levels do
    /// not make in-culture purchases more expensive, which also means the two half-price starter
    /// levels are granted a second time inside every culture. The maxed check does use the total.
    /// </summary>
    public static RunOffer Offer(World w, UpgradeId id)
    {
        var def = UpgradeCatalog.Def(id);
        var level = Stats.TotalLevel(w.Lab, w.Run, id);
        var atpCurve = def.Curves.Atp;
        var visible = atpCurve is not null && (def.UnlockDna == 0 || w.Unlocked.Contains(id));
        var maxed = UpgradeBalance.IsMaxed(id, level);
        double? cost = maxed || atpCurve is null ? null : atpCurve.CostAt(w.Run[id]);
        return new RunOffer(def, level, cost, cost is not null && w.Atp >= cost, visible);
    }

    public static BuyResult Buy(World w, UpgradeId id)
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
