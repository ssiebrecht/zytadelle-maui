using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>ATP Yield: multiplier on all ATP earned.</summary>
public static class AtpBonusGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 200;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; } = 40;

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; } = 1;

    public static UpgradeValueCurve ValueGrowth { get; } = new(2.00, 1, 0, 0, waveAmplitude: 0);

    public static PriceCurve Price { get; } = new(55.3757, 6.46861, 2.56918);
}
