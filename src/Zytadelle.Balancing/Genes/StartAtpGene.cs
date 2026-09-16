using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>ATP Reserve: ATP available at culture start. Gene Lab only, never bought in a culture.</summary>
public static class StartAtpGene
{
    /// <summary>Highest level the gene can reach; the value curve is normalised to it.</summary>
    public static int Cap { get; set; } = 99;

    /// <summary>DNA the Gene Lab charges to unlock the gene. 0 = available from the start.</summary>
    public static int UnlockDna { get; set; } = 40;

    /// <summary>Value at level 0, before any purchase.</summary>
    public static double BaseValue { get; set; } = 10;

    public static UpgradeValueCurve ValueGrowth { get; } = new(990, 1, 0, 0, waveAmplitude: 0);

    public static PriceCurve Price { get; } = new(15, 2.22327, 3.25416);
}
