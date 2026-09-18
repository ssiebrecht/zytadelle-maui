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

    /// <summary>
    /// The cost to buy this gene right now, or null if it is maxed or not buyable with ATP at all.
    /// Deliberately independent of whether the gene has been unlocked yet - same as
    /// <see cref="Offer"/>'s own Cost field, which carries that null-ness without folding in its
    /// separate Visible field. A policy that scans every gene for the cheapest one used to pay one
    /// <see cref="RunOffer"/> allocation per gene just to read two of its fields; this is that same
    /// lookup with nothing else attached.
    /// </summary>
    public static double? CostOf(World w, GeneId id)
    {
        var def = UpgradeCatalog.Def(id);
        if (!def.BuyableInRun) return null;
        var level = Stats.TotalLevel(w.Lab, w.Run, id);
        return GeneRegistry.IsMaxed(id, level) ? null : def.Gene.CostAt(level);
    }

    /// <summary>
    /// Same three-way check <see cref="Buy"/> always ran - Visible, then Cost, then Atp - just
    /// without building a <see cref="RunOffer"/> first. Not built on <see cref="CostOf"/>: that
    /// helper's null deliberately does not see Visible, but Locked has to be checked (and returned)
    /// before Maxed, so the two conditions cannot share one collapsed null without losing that order.
    /// </summary>
    public static BuyResult TryBuy(World w, GeneId id)
    {
        var def = UpgradeCatalog.Def(id);
        var visible = def.BuyableInRun && (def.UnlockDna == 0 || w.Unlocked.Contains(id));
        if (!visible) return BuyResult.Locked;

        var level = Stats.TotalLevel(w.Lab, w.Run, id);
        if (GeneRegistry.IsMaxed(id, level)) return BuyResult.Maxed;

        var cost = def.Gene.CostAt(level);
        if (w.Atp < cost) return BuyResult.Poor;

        w.Atp -= cost;
        w.Run[id]++;
        w.RunBuys++;
        w.RefreshStats();
        return BuyResult.Bought;
    }

    public static BuyResult Buy(World w, GeneId id) => TryBuy(w, id);
}
