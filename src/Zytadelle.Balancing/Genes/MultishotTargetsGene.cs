using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>Granule Count: pathogens a burst hits at once (counts the primary shot).</summary>
public static class MultishotTargetsGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 7;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; } = 250;

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; } = 2;

    public static UpgradeValueCurve ValueGrowth { get; } = new(7, 1, 0, 0, waveAmplitude: 0);

    public static PriceCurve Price { get; } = new(63, 3, 1.55);
}
