using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing;

public enum MissionId
{
    CycleSingle,
    TimeSingle,
    CyclesTotal,
    KillsTotal,
    KillsSingle,
    BossTotal,
    FastTotal,
    TankTotal,
    RangedTotal,
    CritTotal,
    DnaTotal,
    AtpTotal,
    BuysTotal,
    Cultures,
}

/// <summary>Only two missions of a group per day, so a day is never five variations of the same grind.</summary>
public enum MissionGroup
{
    Progress,
    Volume,
    Kind,
    Economy,
    Chore,
}

/// <summary>Whether a mission adds every culture of the day up or keeps the single best one.</summary>
public enum MissionScope
{
    Sum,
    Best,
}

/// <summary>A count per pathogen type. Used for both the reference run and a finished culture.</summary>
public readonly record struct KindCounts(double Basic, double Fast, double Tank, double Ranged, double Boss)
{
    public double Of(EnemyKind kind) => kind switch
    {
        EnemyKind.Basic => Basic,
        EnemyKind.Fast => Fast,
        EnemyKind.Tank => Tank,
        EnemyKind.Ranged => Ranged,
        EnemyKind.Boss => Boss,
        _ => 0,
    };

    public KindCounts With(EnemyKind kind, double value) => kind switch
    {
        EnemyKind.Basic => this with { Basic = value },
        EnemyKind.Fast => this with { Fast = value },
        EnemyKind.Tank => this with { Tank = value },
        EnemyKind.Ranged => this with { Ranged = value },
        EnemyKind.Boss => this with { Boss = value },
        _ => this,
    };
}

/// <summary>
/// What one culture up to the anchor cycle is worth. Every daily target is a multiple of this, so
/// the targets scale with the player instead of being written down.
/// </summary>
public sealed record MissionReference(
    int Cycle,
    int Infection,
    double Kills,
    KindCounts ByKind,
    double Dna,
    double Atp,
    double Time,
    double CritChance,
    double Spent);

/// <summary>The group, the scope, the target and the gate of one mission. Its wording is content.</summary>
public sealed record MissionRule(
    MissionId Id,
    MissionGroup Group,
    MissionScope Scope,
    Func<MissionReference, double> Target,
    Func<MissionReference, bool>? Eligible = null);

/// <summary>
/// The daily missions: how hard they are, how many of a kind a day may draw, and what they pay.
///
/// The reward is a rebate on the player's own investment - a frozen percentage of the DNA ever sunk
/// into the Gene Lab. Only the percentage is frozen when the day rolls; the spend is read live, so
/// investing during the day raises what the day still owes. That grows monotonically with lifetime
/// spend and overtakes culture income late, which is what <see cref="RewardScale"/> is for.
/// </summary>
public static class MissionBalance
{
    public static int MissionsPerDay { get; set; } = 5;

    /// <summary>Lower bound of the rebate percentage a mission is rolled with.</summary>
    public static double RewardPctMin { get; set; } = 3;

    /// <summary>Upper bound of that percentage.</summary>
    public static double RewardPctMax { get; set; } = 6;

    /// <summary>Floor so a fresh save, which has spent nothing, still earns something.</summary>
    public static double RewardMinDna { get; set; } = 10;

    /// <summary>Global multiplier on every reward - the one number to turn if the payout runs hot.</summary>
    public static double RewardScale { get; set; } = 1;

    /// <summary>At most this many missions from one group per day, and only ever one chore.</summary>
    public static IReadOnlyDictionary<MissionGroup, int> GroupCap { get; set; } = new Dictionary<MissionGroup, int>
    {
        [MissionGroup.Progress] = 2,
        [MissionGroup.Volume] = 2,
        [MissionGroup.Kind] = 2,
        [MissionGroup.Economy] = 2,
        [MissionGroup.Chore] = 1,
    };

    /// <summary>The anchor never falls below this, so a fresh save still gets finishable targets.</summary>
    public static double AnchorFloor { get; set; } = 10;

    /// <summary>
    /// How much of the previous infection's record carries into the anchor. It exists because the
    /// record of a freshly unlocked infection is 0 on the day it opens, which would otherwise
    /// collapse every target to the floor.
    /// </summary>
    public static double AnchorPrevWeight { get; set; } = 0.3;

    /// <summary>
    /// The cycle the day's targets are measured against: the best cycle of the highest unlocked
    /// infection, with the previous one carrying <see cref="AnchorPrevWeight"/> of the weight.
    /// </summary>
    public static int AnchorCycle(IReadOnlyDictionary<int, int> bestCycle)
    {
        var hi = InfectionBalance.HighestUnlocked(bestCycle);
        var prev = bestCycle.TryGetValue(hi - 1, out var p) ? p : 0;
        var best = bestCycle.TryGetValue(hi, out var b) ? b : 0;
        return (int)Math.Max(AnchorFloor, Math.Max(best, JsMath.Round(AnchorPrevWeight * prev)));
    }

    /// <summary>Reward in DNA for one mission, from the percentage it was rolled with.</summary>
    public static double Reward(double lifetimeSpend, double pct) =>
        Math.Max(RewardMinDna, Math.Ceiling(lifetimeSpend * pct * RewardScale / 100));

    /// <summary>
    /// The catalog, in a fixed order. The order is load-bearing: the daily draw shuffles this list
    /// with a seeded RNG, so reordering it changes which five missions a given day rolls.
    /// </summary>
    public static IReadOnlyList<MissionRule> Rules { get; set; } =
    [
        new(MissionId.CycleSingle, MissionGroup.Progress, MissionScope.Best,
            r => Math.Max(5, JsMath.Round(0.7 * r.Cycle))),
        new(MissionId.TimeSingle, MissionGroup.Progress, MissionScope.Best,
            r => JsMath.Round(0.62 * r.Time)),
        new(MissionId.CyclesTotal, MissionGroup.Volume, MissionScope.Sum,
            r => JsMath.Round(2 * r.Cycle)),
        new(MissionId.KillsTotal, MissionGroup.Volume, MissionScope.Sum,
            r => JsMath.Round(1.7 * r.Kills)),
        new(MissionId.KillsSingle, MissionGroup.Volume, MissionScope.Best,
            r => JsMath.Round(0.62 * r.Kills)),
        new(MissionId.BossTotal, MissionGroup.Kind, MissionScope.Sum,
            r => Math.Max(2, JsMath.Round(1.6 * r.ByKind.Boss)),
            r => r.Cycle >= 20),
        new(MissionId.FastTotal, MissionGroup.Kind, MissionScope.Sum,
            r => Math.Max(20, JsMath.Round(1.5 * r.ByKind.Fast))),
        new(MissionId.TankTotal, MissionGroup.Kind, MissionScope.Sum,
            r => Math.Max(20, JsMath.Round(1.5 * r.ByKind.Tank)),
            r => r.ByKind.Tank >= 30),
        new(MissionId.RangedTotal, MissionGroup.Kind, MissionScope.Sum,
            r => Math.Max(15, JsMath.Round(1.5 * r.ByKind.Ranged)),
            r => r.ByKind.Ranged >= 40),
        new(MissionId.CritTotal, MissionGroup.Kind, MissionScope.Sum,
            r => Math.Max(25, JsMath.Round(1.5 * r.Kills * r.CritChance)),
            r => r.CritChance >= 0.05),
        new(MissionId.DnaTotal, MissionGroup.Economy, MissionScope.Sum,
            r => Math.Ceiling(1.6 * r.Dna)),
        new(MissionId.AtpTotal, MissionGroup.Economy, MissionScope.Sum,
            r => Math.Ceiling(1.6 * r.Atp)),
        new(MissionId.BuysTotal, MissionGroup.Chore, MissionScope.Sum,
            r => Math.Max(10, JsMath.Round(0.45 * r.Cycle))),
        new(MissionId.Cultures, MissionGroup.Chore, MissionScope.Sum,
            _ => 3),
    ];

    public static MissionRule Rule(MissionId id) => Rules.First(m => m.Id == id);
}
