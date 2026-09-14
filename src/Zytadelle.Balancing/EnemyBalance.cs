namespace Zytadelle.Balancing;

/// <summary>How a shooting pathogen behaves. Its stop distance follows the player's Reach.</summary>
/// <param name="RangeFrac">Fraction of the cell's Reach it stops at.</param>
/// <param name="Windup">Seconds from reaching the fire line to the first shot.</param>
/// <param name="ProjectileSpeed">APPROX - not a documented number.</param>
public sealed record RangedDef(double RangeFrac, double Windup, double ProjectileSpeed);

/// <summary>One pathogen archetype. Health and damage are multiples of the basic of the cycle.</summary>
/// <param name="Kind">Which pathogen this is. Identity, not balance.</param>
/// <param name="Hp">Multiplier on the basic pathogen's HP of the current cycle.</param>
/// <param name="Atk">Multiplier on the basic pathogen's damage of the current cycle.</param>
/// <param name="Speed">Metres per second, before the global tempo knob and the per-spawn jitter.</param>
/// <param name="AttackInterval">Seconds between its attacks.</param>
/// <param name="Radius">Metres. Both the hit box and the drawn size.</param>
/// <param name="Dna">DNA it drops when it dies, before the per-kill and tier multipliers.</param>
/// <param name="Ranged">Set only for the pathogen that shoots; null means it closes to melee.</param>
public sealed record EnemyDef(
    [property: BalanceIdentity] EnemyKind Kind,
    double Hp,
    double Atk,
    double Speed,
    double AttackInterval,
    double Radius,
    double Dna,
    RangedDef? Ranged = null);

/// <summary>The five pathogens and the global tempo knob on top of their published speeds.</summary>
public static class EnemyBalance
{
    /// <summary>Scales every published speed at spawn time. The deviation is deliberate.</summary>
    public static double SpeedMult { get; set; } = 0.95;

    /// <summary>Per-spawn speed jitter, so a wave does not arrive as one wall.</summary>
    public static double SpeedJitterMin { get; set; } = 0.92;

    public static double SpeedJitterMax { get; set; } = 1.08;

    private static IReadOnlyDictionary<EnemyKind, EnemyDef> _all = Build();

    public static IReadOnlyDictionary<EnemyKind, EnemyDef> All
    {
        get => _all;
        set => _all = value;
    }

    public static EnemyDef Def(EnemyKind kind) => _all[kind];

    private static Dictionary<EnemyKind, EnemyDef> Build() => new()
    {
        [EnemyKind.Basic] = new(EnemyKind.Basic, Hp: 1, Atk: 1, Speed: 8.6, AttackInterval: 2, Radius: 2.2, Dna: 0.01),
        [EnemyKind.Fast] = new(EnemyKind.Fast, Hp: 1, Atk: 1, Speed: 21, AttackInterval: 2, Radius: 1.9, Dna: 2.5),
        [EnemyKind.Tank] = new(EnemyKind.Tank, Hp: 5, Atk: 1, Speed: 4.3, AttackInterval: 2, Radius: 3.6, Dna: 5),
        [EnemyKind.Ranged] = new(EnemyKind.Ranged, Hp: 1, Atk: 1, Speed: 8.6, AttackInterval: 2.5, Radius: 2.2, Dna: 2.5,
            Ranged: new RangedDef(RangeFrac: 27.4 / 30, Windup: 0.5, ProjectileSpeed: 18)),
        [EnemyKind.Boss] = new(EnemyKind.Boss, Hp: 20, Atk: 1, Speed: 2.58, AttackInterval: 2, Radius: 5.5, Dna: 25),
    };
}
