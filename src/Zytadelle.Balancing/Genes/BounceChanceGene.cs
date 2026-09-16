using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>Diffusion Chance: chance a toxin diffuses onward after a hit.</summary>
public static class BounceChanceGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 85;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; } = 150;

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; }

    public static UpgradeValueCurve ValueGrowth { get; } = new(0.68, 20, 0.6, 0.6);

    public static PriceCurve Price { get; } = new(10, 6.37758, 2.92615);
}
