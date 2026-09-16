namespace Zytadelle.Balancing;

/// <summary>Damage rules and the timings the combat step reads.</summary>
public static class CombatBalance
{
    /// <summary>Outgoing damage multiplier a pathogen spawns with, before it has landed a hit.</summary>
    public static double HeatupBase { get; set; } = 1;

    /// <summary>Each attack an enemy lands heats it up: +4 % on its own outgoing damage, forever.</summary>
    public static double HeatupPerHit { get; set; } = 0.04;

    /// <summary>Resistance % applies first, then Cell Wall; the remainder reaches integrity.</summary>
    public static double ApplyIncoming(double raw, double defPct, double defAbs) =>
        Math.Max(0, raw * (1 - defPct) - defAbs);

    /// <summary>Outgoing damage of one toxin. The counterpart to <see cref="ApplyIncoming"/>.</summary>
    public static double ApplyCrit(double raw, double critDamage, bool crit) =>
        crit ? raw * critDamage : raw;

    /// <summary>
    /// Toxins released beyond the first when Burst fires. Burst counts the primary shot, so a gene
    /// value of 2 means one extra toxin - the opposite convention to <see cref="BounceHops"/>.
    /// </summary>
    public static int MultishotExtraShots(double multishotTargets) =>
        Math.Max(0, (int)Math.Floor(multishotTargets) - 1);

    /// <summary>
    /// Pathogens a toxin may hop on to after its first hit. Diffusion counts only the hops, so a
    /// gene value of 1 means one ricochet - the opposite convention to
    /// <see cref="MultishotExtraShots"/>.
    /// </summary>
    public static int BounceHops(double bounceTargets) =>
        Math.Max(0, (int)Math.Floor(bounceTargets));
}
