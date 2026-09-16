using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>Diffusion Range: how far a toxin diffuses, in metres.</summary>
public static class BounceRangeGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 60;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; } = 200;

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; } = 20;

    public static UpgradeValueCurve ValueGrowth { get; } = new(60, 20, 0.6, 0.5, 0.20, 3);

    public static PriceCurve Price { get; } = new(10, 18.0121, 5.55655);
}
