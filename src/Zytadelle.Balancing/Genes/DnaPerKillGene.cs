using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>DNA / Kill: multiplier on DNA drops.</summary>
public static class DnaPerKillGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 150;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; } = 100;

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; } = 1;

    public static UpgradeValueCurve ValueGrowth { get; } = new(1.5, 1, 0, 0, waveAmplitude: 0);

    public static PriceCurve Price { get; } = new(291.972, 7.32003, 2.56466);
}
