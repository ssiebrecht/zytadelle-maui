using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>DNA / Cycle: DNA granted at cycle end.</summary>
public static class DnaPerCycleGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 149;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; } = 100;

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; } = 2;

    public static UpgradeValueCurve ValueGrowth { get; } = new(298, 1, 0, 0, waveAmplitude: 0);

    public static PriceCurve Price { get; } = new(6, 5.29063, 2.52158);
}
