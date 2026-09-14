namespace Zytadelle.Balancing;

/// <summary>ATP and DNA income: per kill, per cycle, and the decay on stale drops.</summary>
public static class EconomyBalance
{
    /// <summary>Cycle at which ATP per kill slows from one step every ten cycles to one every twenty.</summary>
    public static double AtpKillSlowCycle { get; set; } = 200;

    /// <summary>Cycles per ATP step before the slowdown.</summary>
    public static double AtpKillStepEarly { get; set; } = 10;

    /// <summary>Cycles per ATP step after the slowdown.</summary>
    public static double AtpKillStepLate { get; set; } = 20;

    /// <summary>The kicker the whole payout picks up once the slowdown starts.</summary>
    public static double AtpKillLateFactor { get; set; } = 1.25;

    /// <summary>A pathogen older than this many cycles drops decayed DNA.</summary>
    public static double DnaDecayCycles { get; set; } = 3;

    /// <summary>What a decayed drop is worth.</summary>
    public static double DnaDecayMult { get; set; } = 0.5;

    /// <summary>Base ATP per kill: 1, +1 every ten cycles to cycle 200, then +1 every twenty.</summary>
    public static double BaseAtpPerKill(double cycle) =>
        cycle <= AtpKillSlowCycle
            ? 1 + Math.Floor(cycle / AtpKillStepEarly)
            : (1 + AtpKillSlowCycle / AtpKillStepEarly + Math.Floor((cycle - AtpKillSlowCycle) / AtpKillStepLate))
              * AtpKillLateFactor;

    public static double AtpForKill(double cycle, double atpBonus, double infectionAtpMult) =>
        BaseAtpPerKill(cycle) * atpBonus * infectionAtpMult;

    /// <summary>
    /// DNA a kill drops. Basics carry a sliver rather than nothing, so the guard is on
    /// "no drop at all", not on "worth less than one".
    /// </summary>
    public static double DnaForKill(double enemyDna, int spawnCycle, int cycle, double dnaPerKill, double infectionDnaMult)
    {
        if (enemyDna <= 0) return 0;
        var decay = cycle - spawnCycle > DnaDecayCycles ? DnaDecayMult : 1;
        return enemyDna * dnaPerKill * infectionDnaMult * decay;
    }

    /// <summary>Cycle-end payout. The ATP bonus is already folded into <paramref name="atpPerCycle"/>.</summary>
    public static (double AtpGained, double DnaGained) CycleEnd(double atpPerCycle, double dnaPerCycle, double infectionDnaMult) =>
        (atpPerCycle, dnaPerCycle * infectionDnaMult);
}
