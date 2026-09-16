using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>Toxicity: toxin damage per hit.</summary>
public static class DamageGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 6000;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; }

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; } = 3;

    public static UpgradeValueCurve ValueGrowth { get; } = new(80401268, 50, 2, 0, wavePeriod: 24);

    public static PriceCurve Price { get; } = new(5, 6.8762, 2.33605);
}
