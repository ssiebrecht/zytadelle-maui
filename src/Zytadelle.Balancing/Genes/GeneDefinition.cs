using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Genes;

/// <summary>
/// One gene as the game reads it. The numbers themselves live in a static class per gene
/// (<see cref="DamageGene"/> and its siblings) so the Balance Lab finds each gene as its own group
/// with stable paths; this is the uniform view over them. The scalar accessors are delegates
/// rather than copies, so a value the Lab writes into the static class is read live.
/// </summary>
public sealed class GeneDefinition(
    GeneId id,
    Type source,
    Func<int> cap,
    Func<int> unlockDna,
    Func<double> baseValue,
    UpgradeValueCurve valueGrowth,
    PriceCurve price,
    bool buyableInRun = true)
{
    public GeneId Id => id;

    /// <summary>The static class holding the numbers. Its name is the gene's group in the Lab.</summary>
    public Type Source => source;

    /// <summary>Highest level the gene can reach.</summary>
    public int Cap => cap();

    /// <summary>DNA the Gene Lab charges to unlock the gene; 0 means available from the start.</summary>
    public int UnlockDna => unlockDna();

    /// <summary>Value at level 0.</summary>
    public double BaseValue => baseValue();

    public UpgradeValueCurve ValueGrowth => valueGrowth;

    public PriceCurve Price => price;

    /// <summary>False for a gene the in-culture shop never offers. Identity, not balance.</summary>
    public bool BuyableInRun => buyableInRun;

    /// <summary>Value at a total (permanent + temporary) level, clamped to the cap.</summary>
    public double ValueAt(int level) => BaseValue + ValueGrowth.SumTo(level, Cap);

    public bool IsMaxed(int level) => level >= Cap;

    /// <summary>Price of buying from <paramref name="level"/> to <paramref name="level"/> + 1.</summary>
    public double CostAt(int level) => Price.CostAt(level);
}
