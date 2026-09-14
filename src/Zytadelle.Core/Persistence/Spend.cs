using Zytadelle.Balancing;
using Zytadelle.Core.Upgrades;

namespace Zytadelle.Core.Persistence;

/// <summary>
/// Lifetime DNA investment in the Gene Lab, derived rather than counted: the sum of every bought
/// level plus every unlock price. There is no refund and no respec - buying is the only way DNA
/// leaves a save - so the derivation is exact and old saves need no migration.
/// </summary>
public static class Spend
{
    private static readonly Dictionary<UpgradeId, List<double>> Prefix = [];

    /// <summary>DNA sunk into one gene to reach <paramref name="level"/>.</summary>
    public static double OnGene(UpgradeId id, int level)
    {
        if (level <= 0) return 0;
        if (!Prefix.TryGetValue(id, out var acc)) Prefix[id] = acc = [0];
        var price = UpgradeBalance.Of(id).Dna;
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
