namespace Zytadelle.Balancing;

/// <summary>Damage rules and the timings the combat step reads.</summary>
public static class CombatBalance
{
    /// <summary>Each attack an enemy lands heats it up: +4 % on its own outgoing damage, forever.</summary>
    public static double HeatupPerHit { get; set; } = 0.04;

    /// <summary>Minimum lifetime a toxin is granted after a diffusion hop. APPROX.</summary>
    public static double RicochetHopLife { get; set; } = 1;

    /// <summary>Seconds a pathogen shows its hit flash.</summary>
    public static double EnemyFlash { get; set; } = 0.12;

    /// <summary>Seconds the cell shows its hit flash.</summary>
    public static double CellFlash { get; set; } = 0.1;

    /// <summary>Lifetime of a toxin fired by the cell.</summary>
    public static double CellProjectileLife { get; set; } = 3;

    /// <summary>Lifetime of a shot fired at the cell.</summary>
    public static double EnemyProjectileLife { get; set; } = 6;

    /// <summary>Speed used when a shooting pathogen carries no ranged profile.</summary>
    public static double FallbackEnemyProjectileSpeed { get; set; } = 18;

    /// <summary>Resistance % applies first, then Cell Wall; the remainder reaches integrity.</summary>
    public static double ApplyIncoming(double raw, double defPct, double defAbs) =>
        Math.Max(0, raw * (1 - defPct) - defAbs);
}
