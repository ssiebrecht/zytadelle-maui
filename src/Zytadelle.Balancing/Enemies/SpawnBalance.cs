using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Enemies;

/// <summary>
/// How many pathogens a cycle sends and of which kind. Both used to be step tables over 31 wave
/// thresholds; they are cubics in ln(cycle) now, pinned so cycle 1 matches the old table exactly:
/// 10 % rate and 95 % basics at cycle 1, 22 % and 74 % at cycle 100.
/// </summary>
public static class SpawnBalance
{
    /// <summary>The in-game "spawn rate" percentage.</summary>
    public static LogCurve SpawnRate { get; set; } = new(Base: 10, Log: 0.9644, Log2: -0.157017, Log3: 0.0978885);

    /// <summary>Pathogens per point of spawn rate. APPROX, derived from the spawn diagnostics.</summary>
    public static double SpawnsPerRate { get; set; } = 2.1;

    public static LogCurve ShareFast { get; set; } = new(Base: 5, Log: -0.9378, Log2: 0.633789, Log3: -0.0454417);

    public static LogCurve ShareTank { get; set; } = new(Base: 0, Log: 2.447, Log2: -0.5696, Log3: 0.103854);

    public static LogCurve ShareRanged { get; set; } = new(Base: 0, Log: -1.008, Log2: 0.640743, Log3: -0.0405562);

    /// <summary>
    /// The order the roulette wheel walks. Deliberately not the order the shares are declared in -
    /// changing it changes which pathogen a given random draw produces.
    /// </summary>
    public static readonly EnemyKind[] SpawnKinds = [EnemyKind.Basic, EnemyKind.Fast, EnemyKind.Ranged, EnemyKind.Tank];

    public static double Rate(double cycle) => Math.Max(0, SpawnRate.At(cycle));

    public static int SpawnsPerCycle(double cycle) => (int)Math.Max(1, JsMath.Round(SpawnsPerRate * Rate(cycle)));

    public static double SpawnInterval(double cycle) => CycleBalance.SpawnDuration / SpawnsPerCycle(cycle);

    public static SpawnMix Weights(double cycle)
    {
        var fast = Math.Max(0, ShareFast.At(cycle));
        var tank = Math.Max(0, ShareTank.At(cycle));
        var ranged = Math.Max(0, ShareRanged.At(cycle));
        return new SpawnMix(Math.Max(0, 100 - fast - tank - ranged), fast, tank, ranged);
    }

    /// <summary>
    /// Expected spawns per type over cycles 1..cycle, bosses included. Normalised by the weight
    /// sum, not by 100. Used to size the daily missions.
    /// </summary>
    public static KindCounts Totals(double cycle)
    {
        double basic = 0, fast = 0, tank = 0, ranged = 0;
        var last = (int)Math.Floor(cycle);
        for (var c = 1; c <= last; c++)
        {
            var spawns = SpawnsPerCycle(c);
            var w = Weights(c);
            var sum = w.Sum;
            basic += spawns * w.Basic / sum;
            fast += spawns * w.Fast / sum;
            tank += spawns * w.Tank / sum;
            ranged += spawns * w.Ranged / sum;
        }
        return new KindCounts(basic, fast, tank, ranged, Math.Floor((double)last / CycleBalance.BossEvery));
    }
}
