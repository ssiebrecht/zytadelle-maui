using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>Rupture Damage: rupture damage multiplier.</summary>
public static class CritDamageGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 150;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; }

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; } = 1.2;

    public static UpgradeValueCurve ValueGrowth { get; } = new(15, 20, 0.6, 0.3, wavePeriod: 4);

    public static PriceCurve Price { get; } = new(5, 19.6415, 4.5563);
}
