using Zytadelle.Lab.Engine.Sim;

namespace Zytadelle.Lab.Engine.Search;

/// <summary>A build that made the table, with what the held-out seeds said about it.</summary>
/// <param name="Strategy">The priorities themselves.</param>
/// <param name="Label">Set on the fixed reference builds, which are always carried through.</param>
/// <param name="Score">Mean objective over the held-out seeds, in campaign seconds.</param>
/// <param name="StandardError">Standard error of that mean.</param>
/// <param name="ReachedFraction">Share of held-out campaigns that got to the target at all.</param>
/// <param name="MeanBestCycle">Mean deepest cycle over those campaigns.</param>
/// <param name="MeanRuns">Mean cultures spent.</param>
/// <param name="RepresentativeSeed">The held-out seed closest to the mean - the one the charts show.</param>
/// <param name="Detail">Filled in after the finalists are re-run with telemetry.</param>
public sealed record Finalist(
    Strategy Strategy,
    string? Label,
    double Score,
    double StandardError,
    double ReachedFraction,
    double MeanBestCycle,
    double MeanRuns,
    uint RepresentativeSeed,
    CampaignResult? Detail)
{
    /// <summary>The ladder as the table shows it, empty until the build has been re-run in detail.</summary>
    public IReadOnlyList<LadderRow> Ladder(int speed) =>
        Detail is null ? [] : Sim.Ladder.Rows(Detail.Ladder, speed);
}

public enum SearchPhase
{
    Idle,
    Calibrating,
    Searching,
    Finals,
    Charts,
    Done,
    Cancelled,
}

/// <param name="Phase">Where the run is.</param>
/// <param name="Generation">Round in progress, for the priority search.</param>
/// <param name="Generations">Rounds in total.</param>
/// <param name="Done">Campaigns finished in the batch that is running.</param>
/// <param name="Total">Campaigns in that batch.</param>
/// <param name="Campaigns">Campaigns simulated since the run started.</param>
/// <param name="BestSeconds">Fastest single campaign seen so far; infinity until one lands.</param>
/// <param name="BestCycle">Deepest cycle any campaign has reached.</param>
public sealed record SearchProgress(
    SearchPhase Phase = SearchPhase.Idle,
    int Generation = 0,
    int Generations = 0,
    int Done = 0,
    int Total = 0,
    long Campaigns = 0,
    double BestSeconds = double.PositiveInfinity,
    int BestCycle = 0);
