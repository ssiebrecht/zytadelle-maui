using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>Reach: targeting radius in metres.</summary>
public static class RangeGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 100;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; } = 50;

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; } = 30;

    public static UpgradeValueCurve ValueGrowth { get; } = new(50, 20, 0.6, 0.4);

    public static PriceCurve Price { get; } = new(41.9660, 6.18089, 2.56923);
}
