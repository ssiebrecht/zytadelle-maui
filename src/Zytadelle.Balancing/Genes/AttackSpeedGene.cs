using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>Secretion Rate: multiplier on the cell's base fire rate.</summary>
public static class AttackSpeedGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 99;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; }

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; } = 1;

    public static UpgradeValueCurve ValueGrowth { get; } = new(4.95, 20, 0.6, 0.1);

    public static PriceCurve Price { get; } = new(3, 5.10836, 2.63677);
}
