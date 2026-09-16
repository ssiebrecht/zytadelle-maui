namespace Zytadelle.Balancing;

/// <summary>
/// Hidden properties of the player cell. These affect the simulation but are not presented as gene
/// values in the UI; every displayed base value belongs to its individual gene instead.
/// </summary>
public static class CellBalance
{
    public static double Radius { get; set; } = 4;

    /// <summary>
    /// Toxins fired per second at a displayed Secretion Rate of 1. The gene value is a multiplier,
    /// so changing this never changes the value shown on the gene card.
    /// </summary>
    public static double BaseAttackSpeed { get; set; } = 2;

    public static double ProjectileSpeed { get; set; } = 60;
    public static double ProjectileLifetime { get; set; } = 3;
    public static double RicochetHopLifetime { get; set; } = 1;

    public static double EffectiveAttackSpeed(double geneMultiplier) => BaseAttackSpeed * geneMultiplier;
}
