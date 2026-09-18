using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>Resistance %: fraction of incoming damage removed, applied before Cell Wall.</summary>
public static class DefPctGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 100;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; } = 75;

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; }

    public static UpgradeValueCurve ValueGrowth { get; } = new(0.50, 20, 0.6, 0.7);

    public static PriceCurve Price { get; } = new(48.7117, 4.35608, 2.37785);
}
