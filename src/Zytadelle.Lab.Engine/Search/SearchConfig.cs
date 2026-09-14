using Zytadelle.Balancing;
using Zytadelle.Lab.Engine.Sim;

namespace Zytadelle.Lab.Engine.Search;

public enum SearchMode
{
    /// <summary>Cross-entropy: sample, score, refit on the elites, repeat.</summary>
    Priority,

    /// <summary>Every combination of a weight grid over the chosen genes.</summary>
    Exhaustive,
}

/// <param name="Campaign">What one campaign is: tier, target, budget and how often the shop is visited.</param>
/// <param name="Mode">Which of the two searches to run.</param>
/// <param name="MasterSeed">Seeds everything: the population draw and both seed sets.</param>
/// <param name="Generations">Priority mode: rounds of sample, score and refit.</param>
/// <param name="Population">Priority mode: candidates per round.</param>
/// <param name="MaxSeeds">Most campaign seeds any one candidate is scored on during the search.</param>
/// <param name="SeedRotation">Rounds between swapping the training seeds, so nothing settles into them.</param>
/// <param name="EliteFraction">Share of the survivors the next distribution is fitted to.</param>
/// <param name="Sigma0">Starting spread of the log weights.</param>
/// <param name="SigmaMin">Floor that keeps the spread from collapsing.</param>
/// <param name="SearchGenes">Genes the search may move. Everything else stays at the pinned weight.</param>
/// <param name="SeparateRunWeights">Search in-run priorities separately instead of mirroring the lab ones.</param>
/// <param name="PinnedWeight">What an unsearched gene is worth.</param>
/// <param name="GridLevels">Exhaustive mode: the weight steps each chosen gene may take.</param>
/// <param name="UnlockKs">Exhaustive mode: the levels-per-unlock values to try.</param>
/// <param name="GridDedup">Drop grid vectors that are a multiple of another. Only sound when nothing is pinned above zero.</param>
/// <param name="FinalSeeds">Held-out seeds the finalists are re-scored on.</param>
/// <param name="TopK">How many builds to report.</param>
/// <param name="Workers">Campaigns in flight.</param>
/// <param name="Speed">Game speed the real-time column is quoted at.</param>
public sealed record SearchConfig(
    CampaignConfig Campaign,
    SearchMode Mode = SearchMode.Priority,
    uint MasterSeed = 1,
    int Generations = 8,
    int Population = 40,
    int MaxSeeds = 8,
    int SeedRotation = 4,
    double EliteFraction = 0.25,
    double Sigma0 = 0.9,
    double SigmaMin = 0.15,
    IReadOnlyList<UpgradeId>? SearchGenes = null,
    bool SeparateRunWeights = false,
    double PinnedWeight = 1,
    IReadOnlyList<double>? GridLevels = null,
    IReadOnlyList<int>? UnlockKs = null,
    bool GridDedup = true,
    int FinalSeeds = 24,
    int TopK = 5,
    int Workers = 0,
    int Speed = 20)
{
    public static SearchConfig Default() => new(new CampaignConfig());

    public IReadOnlyList<UpgradeId> Genes => SearchGenes is { Count: > 0 } g ? g : UpgradeIds.All;

    public IReadOnlyList<double> Levels => GridLevels is { Count: > 1 } l ? l : [0, 1, 3, 9];

    public IReadOnlyList<int> Ks => UnlockKs is { Count: > 0 } k ? k : [5];

    public int WorkerCount => Workers > 0 ? Workers : Evaluator.DefaultWorkers;

    /// <summary>
    /// Dedup is only sound when nothing outside the chosen genes carries weight. With a pinned
    /// weight above zero, scaling the chosen block changes its ratio against the pinned block, so
    /// two vectors that look like multiples of each other are genuinely different builds.
    /// </summary>
    public bool DedupSound => Genes.Count == UpgradeIds.Count || PinnedWeight <= 0;
}
