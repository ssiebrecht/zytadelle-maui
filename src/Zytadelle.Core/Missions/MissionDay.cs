using Zytadelle.Balancing;

namespace Zytadelle.Core.Missions;

/// <summary>One of the five daily missions.</summary>
public sealed class MissionSlot
{
    public required MissionId Id { get; init; }

    /// <summary>Frozen when the day is rolled; a new record mid-day does not move the goalposts.</summary>
    public required double Target { get; init; }

    /// <summary>
    /// Percent of lifetime Gene Lab spend this mission pays, two decimals. Only the percentage is
    /// frozen - the spend it applies to is read when the reward is collected.
    /// </summary>
    public required double Pct { get; init; }

    public double Progress;

    public bool Claimed;

    public bool IsDone => Progress >= Target;
}

public sealed class MissionDay
{
    public required string Day { get; init; }

    public required List<MissionSlot> List { get; init; }
}

/// <summary>Everything a finished culture contributes to the day.</summary>
public sealed record RunStats(
    int Cycle,
    double Dna,
    double AtpEarned,
    int Kills,
    double Time,
    int Crits,
    int RunBuys,
    KindCounts KillsByKind);
