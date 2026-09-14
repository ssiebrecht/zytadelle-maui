using Zytadelle.Balancing;

namespace Zytadelle.Core.Upgrades;

/// <summary>
/// A level per gene. Dense on purpose - the browser build used a partial map, but a missing key
/// always meant level 0, so an array of nineteen is the same thing without the lookups.
/// </summary>
public sealed class Levels
{
    private readonly int[] _v = new int[UpgradeIds.Count];

    public Levels() { }

    public Levels(Levels other) => Array.Copy(other._v, _v, _v.Length);

    public int this[UpgradeId id]
    {
        get => _v[(int)id];
        set => _v[(int)id] = value;
    }

    public bool IsEmpty => _v.All(v => v == 0);

    /// <summary>Only the genes that actually carry a level, in catalog order.</summary>
    public IEnumerable<(UpgradeId Id, int Level)> NonZero()
    {
        for (var i = 0; i < _v.Length; i++)
            if (_v[i] > 0)
                yield return ((UpgradeId)i, _v[i]);
    }
}

/// <summary>
/// The effective value of every gene. Recomputed only on a purchase, never per tick - nineteen
/// doubles that the whole simulation reads.
/// </summary>
public sealed class Stats
{
    private readonly double[] _v = new double[UpgradeIds.Count];

    /// <summary>Effective stats from Gene Lab levels plus the levels bought during this culture.</summary>
    public static Stats From(Levels lab, Levels run)
    {
        var s = new Stats();
        foreach (var id in UpgradeIds.All) s._v[(int)id] = UpgradeBalance.ValueAt(id, TotalLevel(lab, run, id));
        return s;
    }

    public static int TotalLevel(Levels lab, Levels run, UpgradeId id) => lab[id] + run[id];

    public double this[UpgradeId id] => _v[(int)id];

    public double Damage => _v[(int)UpgradeId.Damage];
    public double AttackSpeed => _v[(int)UpgradeId.AttackSpeed];
    public double CritChance => _v[(int)UpgradeId.CritChance];
    public double CritDamage => _v[(int)UpgradeId.CritDamage];
    public double Range => _v[(int)UpgradeId.Range];
    public double MultishotChance => _v[(int)UpgradeId.MultishotChance];
    public double MultishotTargets => _v[(int)UpgradeId.MultishotTargets];
    public double BounceChance => _v[(int)UpgradeId.BounceChance];
    public double BounceTargets => _v[(int)UpgradeId.BounceTargets];
    public double BounceRange => _v[(int)UpgradeId.BounceRange];
    public double Health => _v[(int)UpgradeId.Health];
    public double Regen => _v[(int)UpgradeId.Regen];
    public double DefPct => _v[(int)UpgradeId.DefPct];
    public double DefAbs => _v[(int)UpgradeId.DefAbs];
    public double AtpBonus => _v[(int)UpgradeId.AtpBonus];
    public double AtpPerCycle => _v[(int)UpgradeId.AtpPerCycle];
    public double StartAtp => _v[(int)UpgradeId.StartAtp];
    public double DnaPerKill => _v[(int)UpgradeId.DnaPerKill];
    public double DnaPerCycle => _v[(int)UpgradeId.DnaPerCycle];
}
