using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>Rupture Chance: chance of a rupturing hit.</summary>
public static class CritChanceGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 79;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; }

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; } = 0.01;

    public static UpgradeValueCurve ValueGrowth { get; } = new(0.79, 20, 0.6, 0.2);

    public static PriceCurve Price { get; } = new(2, 4.81437, 2.65188);
}
