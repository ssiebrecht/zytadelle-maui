using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>Membrane: maximum cell integrity.</summary>
public static class HealthGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 6000;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; }

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; } = 5;

    public static UpgradeValueCurve ValueGrowth { get; } = new(1422568295, 15, 2, 0.25, wavePeriod: 24);

    public static PriceCurve Price { get; } = new(5, 6.32804, 2.26511);
}
