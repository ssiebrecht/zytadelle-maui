namespace Zytadelle.Balancing;

/// <summary>Geometry of the dish. Everything the simulation measures is in metres.</summary>
public static class ArenaBalance
{
    /// <summary>Radius of the spawn circle; pathogens appear on it.</summary>
    public static double ArenaRadius { get; set; } = 82;

    /// <summary>Radius of the player cell: projectile origin and the stop distance for melee.</summary>
    public static double CellRadius { get; set; } = 4;

    /// <summary>Speed of the cell's toxins. APPROX - not a documented number.</summary>
    public static double ProjectileSpeed { get; set; } = 60;

    /// <summary>Hard cap on living pathogens; spawning above it is a no-op.</summary>
    public static int MaxEnemies { get; set; } = 120;
}
