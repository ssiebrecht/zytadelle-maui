namespace Zytadelle.Balancing;

/// <summary>
/// The nineteen genes, in catalog order. The order is load-bearing: it is the order the Gene Lab
/// and the in-culture shop list them in, and the order lifetime spend is summed in.
/// </summary>
public enum UpgradeId
{
    // Offense
    Damage,
    AttackSpeed,
    CritChance,
    CritDamage,
    Range,
    MultishotChance,
    MultishotTargets,
    BounceChance,
    BounceTargets,
    BounceRange,
    // Barrier
    Health,
    Regen,
    DefPct,
    DefAbs,
    // Metabolism
    AtpBonus,
    AtpPerCycle,
    StartAtp,
    DnaPerKill,
    DnaPerCycle,
}

public static class UpgradeIds
{
    /// <summary>Every gene, in catalog order.</summary>
    public static readonly UpgradeId[] All = Enum.GetValues<UpgradeId>();

    public const int Count = 19;
}
