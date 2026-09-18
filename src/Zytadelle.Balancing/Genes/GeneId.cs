namespace Zytadelle.Balancing.Genes;

/// <summary>
/// The twenty-one genes, in catalog order. The order is load-bearing: it is the order the Gene Lab
/// and the in-culture shop list them in, and the order lifetime spend is summed in. The member
/// names are the keys of the save file, so they never change.
/// </summary>
public enum GeneId
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
    Thorns,
    LifeSteal,
    // Metabolism
    AtpBonus,
    AtpPerCycle,
    StartAtp,
    DnaPerKill,
    DnaPerCycle,
}

public static class GeneIds
{
    /// <summary>Every gene, in catalog order.</summary>
    public static readonly GeneId[] All = Enum.GetValues<GeneId>();

    /// <summary>How many genes there are. Derived, so adding one to the enum is the only edit.</summary>
    public static int Count => All.Length;
}
