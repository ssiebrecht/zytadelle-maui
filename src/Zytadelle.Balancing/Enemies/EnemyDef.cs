namespace Zytadelle.Balancing.Enemies;

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
