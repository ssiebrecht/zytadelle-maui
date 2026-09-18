namespace Zytadelle.Core.Upgrades;

/// <summary>
/// A level per gene. Dense on purpose - the browser build used a partial map, but a missing key
/// always meant level 0, so an array of nineteen is the same thing without the lookups.
/// </summary>
public sealed class Levels
{
    private readonly int[] _v = new int[GeneIds.Count];

    public Levels() { }

    public Levels(Levels other) => Array.Copy(other._v, _v, _v.Length);

    public int this[GeneId id]
    {
        get => _v[(int)id];
        set => _v[(int)id] = value;
    }

    public bool IsEmpty => _v.All(v => v == 0);

    /// <summary>Only the genes that actually carry a level, in catalog order.</summary>
    public IEnumerable<(GeneId Id, int Level)> NonZero()
    {
        for (var i = 0; i < _v.Length; i++)
            if (_v[i] > 0)
                yield return ((GeneId)i, _v[i]);
    }
}

/// <summary>
/// The effective value of every gene. Recomputed only on a purchase, never per tick - twenty-one
/// doubles that the whole simulation reads.
/// </summary>
public sealed class Stats
{
    private readonly double[] _v = new double[GeneIds.Count];

    /// <summary>Effective stats from Gene Lab levels plus the levels bought during this culture.</summary>
    public static Stats From(Levels lab, Levels run)
    {
        var s = new Stats();
        foreach (var id in GeneIds.All) s._v[(int)id] = GeneRegistry.ValueAt(id, TotalLevel(lab, run, id));
        return s;
    }

    public static int TotalLevel(Levels lab, Levels run, GeneId id) => lab[id] + run[id];

    public double this[GeneId id] => _v[(int)id];

    public double Damage => _v[(int)GeneId.Damage];
    public double AttackSpeed => CellBalance.EffectiveAttackSpeed(_v[(int)GeneId.AttackSpeed]);
    public double CritChance => _v[(int)GeneId.CritChance];
    public double CritDamage => _v[(int)GeneId.CritDamage];
    public double Range => _v[(int)GeneId.Range];
    public double MultishotChance => _v[(int)GeneId.MultishotChance];
    public double MultishotTargets => _v[(int)GeneId.MultishotTargets];
    public double BounceChance => _v[(int)GeneId.BounceChance];
    public double BounceTargets => _v[(int)GeneId.BounceTargets];
    public double BounceRange => _v[(int)GeneId.BounceRange];
    public double Health => _v[(int)GeneId.Health];
    public double Regen => _v[(int)GeneId.Regen];
    public double DefPct => _v[(int)GeneId.DefPct];
    public double DefAbs => _v[(int)GeneId.DefAbs];
    public double Thorns => _v[(int)GeneId.Thorns];
    public double LifeSteal => _v[(int)GeneId.LifeSteal];
    public double AtpBonus => _v[(int)GeneId.AtpBonus];
    public double AtpPerCycle => _v[(int)GeneId.AtpPerCycle];
    public double StartAtp => _v[(int)GeneId.StartAtp];
    public double DnaPerKill => _v[(int)GeneId.DnaPerKill];
    public double DnaPerCycle => _v[(int)GeneId.DnaPerCycle];
}
