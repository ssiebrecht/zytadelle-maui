using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>Repair: integrity restored per second.</summary>
public static class RegenGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 5000;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; }

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; }

    public static UpgradeValueCurve ValueGrowth { get; } = new(1387157900, 1.5, 2, 0.5, wavePeriod: 24);

    public static PriceCurve Price { get; } = new(68570.3, 4.67449, 2.22894);
}
