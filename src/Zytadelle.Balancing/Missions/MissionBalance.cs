namespace Zytadelle.Balancing.Missions;

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

    /// <summary>Decimal places the rolled percentage is frozen at, so the save stays readable.</summary>
    public static int RewardPctDecimals { get; set; } = 2;

    /// <summary>No mission ever asks for less than this, whatever its own floor works out to.</summary>
    public static double TargetFloor { get; set; } = 1;

    /// <summary>At most this many missions from one group per day, and only ever one chore.</summary>
    public static IReadOnlyDictionary<MissionGroup, int> GroupCap { get; } = new Dictionary<MissionGroup, int>
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

    // ------------------------------------------------------------------ eligibility gates

    /// <summary>Boss missions stay out of the pool until the reference run reaches this cycle.</summary>
    public static double BossGateCycle { get; set; } = 20;

    /// <summary>Tank missions need the reference run to meet at least this many tanks.</summary>
    public static double TankGateCount { get; set; } = 30;

    /// <summary>Ranged missions need the reference run to meet at least this many phages.</summary>
    public static double RangedGateCount { get; set; } = 40;

    /// <summary>Crit missions need this much crit chance before asking for crit kills.</summary>
    public static double CritGateChance { get; set; } = 0.05;

    // ------------------------------------------------------------------ formulas

    /// <summary>Reward in DNA for one mission, from the percentage it was rolled with.</summary>
    public static double Reward(double lifetimeSpend, double pct) =>
        Math.Max(RewardMinDna, Math.Ceiling(lifetimeSpend * pct * RewardScale / 100));

    /// <summary>Quantises a freshly rolled percentage before it is frozen into the day.</summary>
    public static double RoundPct(double pct)
    {
        var scale = Math.Pow(10, RewardPctDecimals);
        return JsMath.Round(pct * scale) / scale;
    }

    /// <summary>The whole target a mission asks of a reference run, floor included.</summary>
    public static double TargetFor(MissionRule rule, MissionReference reference) =>
        Math.Max(TargetFloor, rule.Target(reference));

    // ------------------------------------------------------------------ catalog

    /// <summary>
    /// The catalog, in a fixed order. The order is load-bearing: the daily draw shuffles this list
    /// with a seeded RNG, so reordering it changes which five missions a given day rolls.
    /// </summary>
    public static IReadOnlyList<MissionRule> Rules { get; } =
    [
        new(MissionId.CycleSingle, MissionGroup.Progress, MissionScope.Best,
            Basis: r => r.Cycle, Mult: 0.7, Floor: 5),
        new(MissionId.TimeSingle, MissionGroup.Progress, MissionScope.Best,
            Basis: r => r.Time, Mult: 0.62),
        new(MissionId.CyclesTotal, MissionGroup.Volume, MissionScope.Sum,
            Basis: r => r.Cycle, Mult: 2),
        new(MissionId.KillsTotal, MissionGroup.Volume, MissionScope.Sum,
            Basis: r => r.Kills, Mult: 1.7),
        new(MissionId.KillsSingle, MissionGroup.Volume, MissionScope.Best,
            Basis: r => r.Kills, Mult: 0.62),
        new(MissionId.BossTotal, MissionGroup.Kind, MissionScope.Sum,
            Basis: r => r.ByKind.Boss, Mult: 1.6, Floor: 2,
            Eligible: r => r.Cycle >= BossGateCycle),
        new(MissionId.FastTotal, MissionGroup.Kind, MissionScope.Sum,
            Basis: r => r.ByKind.Fast, Mult: 1.5, Floor: 20),
        new(MissionId.TankTotal, MissionGroup.Kind, MissionScope.Sum,
            Basis: r => r.ByKind.Tank, Mult: 1.5, Floor: 20,
            Eligible: r => r.ByKind.Tank >= TankGateCount),
        new(MissionId.RangedTotal, MissionGroup.Kind, MissionScope.Sum,
            Basis: r => r.ByKind.Ranged, Mult: 1.5, Floor: 15,
            Eligible: r => r.ByKind.Ranged >= RangedGateCount),
        new(MissionId.CritTotal, MissionGroup.Kind, MissionScope.Sum,
            Basis: r => r.Kills * r.CritChance, Mult: 1.5, Floor: 25,
            Eligible: r => r.CritChance >= CritGateChance),
        new(MissionId.DnaTotal, MissionGroup.Economy, MissionScope.Sum,
            Basis: r => r.Dna, Mult: 1.6, Rounding: MissionRounding.Up),
        new(MissionId.AtpTotal, MissionGroup.Economy, MissionScope.Sum,
            Basis: r => r.Atp, Mult: 1.6, Rounding: MissionRounding.Up),
        new(MissionId.BuysTotal, MissionGroup.Chore, MissionScope.Sum,
            Basis: r => r.Cycle, Mult: 0.45, Floor: 10),
        new(MissionId.Cultures, MissionGroup.Chore, MissionScope.Sum,
            Basis: _ => 1, Mult: 3),
    ];

    public static MissionRule Rule(MissionId id) => Rules.First(m => m.Id == id);
}
