namespace Zytadelle.Balancing.Enemies;

/// <summary>The five pathogens, the global tempo knob on top of their published speeds, and their shots.</summary>
public static class EnemyBalance
{
    /// <summary>Scales every published speed at spawn time. The deviation is deliberate.</summary>
    public static double SpeedMult { get; set; } = 0.95;

    /// <summary>Per-spawn speed jitter, so a wave does not arrive as one wall.</summary>
    public static double SpeedJitterMin { get; set; } = 0.92;

    public static double SpeedJitterMax { get; set; } = 1.08;

    /// <summary>Seconds a pathogen's projectile lives before it is culled.</summary>
    public static double ProjectileLifetime { get; set; } = 6;

    /// <summary>Speed of a pathogen's projectile when its <see cref="RangedDef"/> does not say.</summary>
    public static double ProjectileFallbackSpeed { get; set; } = 18;

    public static IReadOnlyDictionary<EnemyKind, EnemyDef> All { get; } = new Dictionary<EnemyKind, EnemyDef>
    {
        [EnemyKind.Basic] = new(EnemyKind.Basic, Hp: 1, Atk: 1, Speed: 8.6, AttackInterval: 2, Radius: 2.2, Dna: 0.01),
        [EnemyKind.Fast] = new(EnemyKind.Fast, Hp: 1, Atk: 1, Speed: 21, AttackInterval: 2, Radius: 1.9, Dna: 2.5),
        [EnemyKind.Tank] = new(EnemyKind.Tank, Hp: 5, Atk: 1, Speed: 4.3, AttackInterval: 2, Radius: 3.6, Dna: 5),
        [EnemyKind.Ranged] = new(EnemyKind.Ranged, Hp: 1, Atk: 1, Speed: 8.6, AttackInterval: 2.5, Radius: 2.2, Dna: 2.5,
            Ranged: new RangedDef(RangeFrac: 27.4 / 30, Windup: 0.5, ProjectileSpeed: 18)),
        [EnemyKind.Boss] = new(EnemyKind.Boss, Hp: 20, Atk: 1, Speed: 2.58, AttackInterval: 2, Radius: 5.5, Dna: 25),
    };

    /// <summary>Same instances as <see cref="All"/>, indexed by <see cref="EnemyKind"/> for the
    /// spawn hot path; marked so the reflection walk does not present these numbers a second time.</summary>
    [BalanceIdentity]
    public static EnemyDef[] Defs { get; } = BuildDefs();

    private static EnemyDef[] BuildDefs()
    {
        var defs = new EnemyDef[All.Count];
        foreach (var (kind, def) in All) defs[(int)kind] = def;
        return defs;
    }

    static EnemyBalance()
    {
        for (var i = 0; i < Defs.Length; i++)
            if (Defs[i].Kind != (EnemyKind)i)
                throw new InvalidOperationException(
                    $"EnemyBalance.Defs[{i}] is {Defs[i].Kind}, expected {(EnemyKind)i} - All must stay in EnemyKind order.");
    }

    public static EnemyDef Def(EnemyKind kind) => Defs[(int)kind];
}
