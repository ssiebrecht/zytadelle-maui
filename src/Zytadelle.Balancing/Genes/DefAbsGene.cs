using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>Cell Wall: flat damage blocked per hit, applied after Resistance.</summary>
public static class DefAbsGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 5000;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; } = 75;

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; }

    public static UpgradeValueCurve ValueGrowth { get; } = new(92117351, 15, 2, 0.75, wavePeriod: 24);

    public static PriceCurve Price { get; } = new(2, 3.71341, 2.20738);
}
