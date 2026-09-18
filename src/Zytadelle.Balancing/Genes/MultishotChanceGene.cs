using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>Granule Chance: chance of a granule burst.</summary>
public static class MultishotChanceGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 100;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; } = 150;

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; }

    public static UpgradeValueCurve ValueGrowth { get; } = new(0.495, 20, 0.6, 0.5);

    public static PriceCurve Price { get; } = new(44.6360, 4.68662, 2.60443);
}
