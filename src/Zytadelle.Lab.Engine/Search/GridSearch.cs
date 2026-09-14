using Zytadelle.Balancing;
using Zytadelle.Core.Upgrades;
using Zytadelle.Lab.Engine.Sim;

namespace Zytadelle.Lab.Engine.Search;

/// <summary>What an exhaustive run would cost, worked out before anyone starts it.</summary>
/// <param name="Raw">Weight vectors in the full grid.</param>
/// <param name="Candidates">Builds after dedup, and after the gate and unlockK multiply in.</param>
/// <param name="Campaigns">Campaigns that implies, at the average seeds the halving ladder spends.</param>
/// <param name="Estimate">How long that takes on the configured workers, from a measured rate.</param>
/// <param name="DedupApplied">Whether the scale-invariance dedup was sound here and therefore used.</param>
public sealed record GridPlan(long Raw, long Candidates, long Campaigns, TimeSpan Estimate, bool DedupApplied)
{
    public static readonly GridPlan Empty = new(0, 0, 0, TimeSpan.Zero, false);
}

/// <summary>
/// Every combination of a weight grid over the chosen genes - the closest a nineteen-gene game gets
/// to trying every possible build.
///
/// The full space is not reachable and saying so is part of the tool: nineteen genes at four levels
/// is 4^19 vectors, which at a generous rate is a five-figure number of years. What is reachable is
/// a handful of genes at a time, and the honest thing to show is the count and the clock before the
/// run starts rather than a progress bar that never ends.
///
/// Two economies make the reachable set larger than it looks. Only ratios matter, so a vector that
/// is a whole multiple of another is the same build - though that is only true when nothing outside
/// the chosen genes carries weight, which is why <see cref="SearchConfig.DedupSound"/> gates it. And
/// the grid runs through the same successive-halving ladder as the priority search, so most
/// candidates cost one campaign rather than the full seed budget.
/// </summary>
public sealed class GridSearch(SearchConfig config, Evaluator evaluator) : SearchRun(config, evaluator)
{
    /// <summary>Average seeds a candidate survives to, across the halving ladder. Measured, roughly.</summary>
    private const double AverageSeedsPerCandidate = 1.6;

    protected override int RoundCount => 1;

    protected override IEnumerable<Candidate[]> Rounds()
    {
        yield return [.. References(), .. Enumerate(Config).Select(s => new Candidate(s))];
    }

    /// <summary>
    /// Every build in the grid. Vectors are produced in a fixed order and deduplicated on their
    /// shape, so the same configuration always enumerates the same list - which is what would let a
    /// long run be checkpointed and resumed.
    /// </summary>
    public static IReadOnlyList<Strategy> Enumerate(SearchConfig cfg) => [.. EnumerateCore(cfg)];

    private static IEnumerable<Strategy> EnumerateCore(SearchConfig cfg)
    {
        var genes = cfg.Genes;
        var levels = cfg.Levels;
        var dedup = cfg.GridDedup && cfg.DedupSound;
        var seen = dedup ? new HashSet<string>(StringComparer.Ordinal) : null;
        var vector = new double[genes.Count];

        foreach (var combination in Combinations(levels.Count, genes.Count))
        {
            var top = 0.0;
            for (var i = 0; i < genes.Count; i++)
            {
                vector[i] = levels[combination[i]];
                top = Math.Max(top, vector[i]);
            }

            // A build that buys nothing is not a build.
            if (top <= 0) continue;

            if (seen is not null && !seen.Add(Shape(vector, top))) continue;

            var lab = Expand(vector, genes, cfg.PinnedWeight);
            foreach (var gate in new[] { false, true })
            foreach (var k in cfg.Ks)
                yield return new Strategy(lab, (double[])lab.Clone(), gate, k);
        }
    }

    /// <summary>Counting in base <paramref name="levels"/>, least significant gene first.</summary>
    private static IEnumerable<int[]> Combinations(int levels, int width)
    {
        var digits = new int[width];
        while (true)
        {
            yield return digits;
            var i = width - 1;
            for (; i >= 0; i--)
            {
                if (++digits[i] < levels) break;
                digits[i] = 0;
            }

            if (i < 0) yield break;
        }
    }

    private static string Shape(double[] vector, double top) =>
        string.Join(',', vector.Select(v => (v / top).ToString("F4", System.Globalization.CultureInfo.InvariantCulture)));

    private static double[] Expand(double[] vector, IReadOnlyList<UpgradeId> genes, double pinned)
    {
        var w = new double[UpgradeIds.Count];
        Array.Fill(w, pinned);
        for (var i = 0; i < genes.Count; i++) w[(int)genes[i]] = vector[i];
        return w;
    }

    // ---------------------------------------------------------------- the estimate

    /// <summary>
    /// The count in closed form, and the clock from a rate the caller measured. A fixed constant
    /// would be wrong the moment the target cycle moved, which is exactly when someone is most
    /// likely to start a run that takes days.
    /// </summary>
    public static GridPlan Plan(SearchConfig cfg, double campaignsPerSecondPerCore)
    {
        var levels = cfg.Levels.Count;
        var genes = cfg.Genes.Count;
        var dedup = cfg.GridDedup && cfg.DedupSound;

        var raw = Pow(levels, genes);
        // A vector is a duplicate exactly when every entry sits on a level that is the step above
        // another level in the set - so the survivors are the ones that touch the top level at all.
        var distinct = dedup ? raw - Pow(levels - 1, genes) : raw;
        var candidates = distinct * 2 * cfg.Ks.Count;
        var campaigns = (long)(candidates * AverageSeedsPerCandidate);

        var rate = campaignsPerSecondPerCore * cfg.WorkerCount;
        var estimate = rate > 0 && double.IsFinite(rate)
            ? TimeSpan.FromSeconds(campaigns / rate)
            : TimeSpan.MaxValue;

        return new GridPlan(raw, candidates, campaigns, estimate, dedup);
    }

    /// <summary>Saturates instead of overflowing: nineteen genes at four levels is past every budget anyway.</summary>
    private static long Pow(int b, int e)
    {
        var acc = 1L;
        for (var i = 0; i < e; i++)
        {
            if (acc > long.MaxValue / Math.Max(1, b)) return long.MaxValue;
            acc *= b;
        }

        return acc;
    }
}
