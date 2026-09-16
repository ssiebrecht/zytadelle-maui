using Zytadelle.Core.Upgrades;

namespace Zytadelle.Core.Persistence;

/// <summary>
/// Lifetime DNA investment in the Gene Lab, derived rather than counted: the sum of every bought
/// level plus every unlock price. There is no refund and no respec - buying is the only way DNA
/// leaves a save. This is the investment at CURRENT prices, not historical spend: a rebalance
/// also revalues existing levels and their mission-reward basis. Saves need no level migration.
///
/// The prefix sums are a second cache of the price curves, so they follow
/// <see cref="BalanceRevision"/>: a retuned price track would otherwise keep being summed at the
/// shape it had before, and every mission reward is a percentage of this number.
/// </summary>
public static class Spend
{
    private static readonly Dictionary<GeneId, List<double>> Prefix = [];

    /// <summary>Revision the prefix sums were built at; a retune past it throws them away.</summary>
    private static int _revision;

    /// <summary>DNA sunk into one gene to reach <paramref name="level"/>.</summary>
    public static double OnGene(GeneId id, int level)
    {
        if (level <= 0) return 0;
        if (_revision != BalanceRevision.Current)
        {
            Prefix.Clear();
            _revision = BalanceRevision.Current;
        }
        if (!Prefix.TryGetValue(id, out var acc)) Prefix[id] = acc = [0];
        var price = GeneRegistry.Get(id).Price;
        for (var l = acc.Count; l <= level; l++) acc.Add(acc[l - 1] + price.CostAt(l - 1));
        return acc[level];
    }

    public static double Total(SaveData s)
    {
        var sum = 0.0;
        foreach (var def in UpgradeCatalog.All)
        {
            sum += OnGene(def.Id, s.Lab[def.Id]);
            if (def.UnlockDna > 0 && s.Unlocked.Contains(def.Id)) sum += def.UnlockDna;
        }
        return sum;
    }
}
