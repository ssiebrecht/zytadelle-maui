using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>Phagocytosis: % of damage dealt to a pathogen returned to the cell as integrity.</summary>
public static class LifeStealGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 150;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; } = 150;

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; }

    public static UpgradeValueCurve ValueGrowth { get; } = new(5.0, 20, 0.6, 0.55);

    public static PriceCurve Price { get; } = new(583.798, 20, 2.6);
}
