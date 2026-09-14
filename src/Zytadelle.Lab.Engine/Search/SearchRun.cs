using Zytadelle.Balancing;
using Zytadelle.Core.Upgrades;
using Zytadelle.Lab.Engine.Sim;

namespace Zytadelle.Lab.Engine.Search;

/// <summary>
/// Everything both searches share: how a candidate is scored, how the field is thinned, and how the
/// survivors are re-measured on seeds they never saw.
///
/// Three things keep the ranking honest, all of them carried over from the browser build.
/// Common random numbers - every candidate in a round meets the same campaign seeds. Blocking on
/// the seed - a candidate is ranked on how far it beat the field on that seed, which removes the
/// seed's own difficulty from the comparison exactly. And successive halving - most candidates die
/// after one seed, only survivors earn more. At the end the finalists are re-scored on held-out
/// seeds, because the top of a noisy ranking is otherwise mostly luck.
///
/// The two modes differ only in where a population comes from, which is what
/// <see cref="Rounds"/> and <see cref="Absorb"/> are for.
/// </summary>
public abstract class SearchRun(SearchConfig config, Evaluator evaluator)
{
    protected sealed record Candidate(Strategy Strategy, string? Label = null, double[]? Vector = null);

    protected sealed record Scored(Candidate Candidate, double Z, double Mean, int Seeds);

    private readonly Dictionary<string, double> _objective = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SlimResult> _raw = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Scored> _archive = new(StringComparer.Ordinal);
    private long _campaigns;

    protected SearchConfig Config { get; } = config;

    protected Evaluator Pool { get; } = evaluator;

    /// <summary>Latest snapshot, for a timer on the UI thread to read. Never pushed per campaign.</summary>
    public SearchProgress Progress { get; private set; } = new();

    /// <summary>Populations to score, in order. The priority search yields one per round; the grid, one.</summary>
    protected abstract IEnumerable<Candidate[]> Rounds();

    /// <summary>How many rounds <see cref="Rounds"/> will yield, for the progress bar.</summary>
    protected abstract int RoundCount { get; }

    /// <summary>Called with the survivors of a round, so a search that learns can refit.</summary>
    protected virtual void Absorb(IReadOnlyList<Scored> survivors)
    {
    }

    public IReadOnlyList<Finalist> Run(CancellationToken ct)
    {
        var round = 0;
        foreach (var population in Rounds())
        {
            ct.ThrowIfCancellationRequested();
            Absorb(ScoreRound(round++, population, ct));
        }

        var shortlist = Diverse([.. _archive.Values.OrderBy(s => s.Mean)], Config.TopK * 2);
        foreach (var reference in References())
        {
            var key = reference.Strategy.Key();
            if (shortlist.Any(s => s.Candidate.Strategy.Key() == key)) continue;
            shortlist.Add(_archive.GetValueOrDefault(key) ?? new Scored(reference, 0, double.PositiveInfinity, 0));
        }

        return FinalScore(shortlist, ct);
    }

    // ---------------------------------------------------------------- one round

    /// <summary>
    /// Cheap seeds die after one campaign, so the first rung is wide and the last is deep. The grid
    /// uses the same ladder, which is what makes a gene more affordable there than a flat budget
    /// would allow.
    /// </summary>
    private IReadOnlyList<(int Keep, int Seeds)> Rungs(int population)
    {
        (int Keep, int Seeds)[] steps =
        [
            (population, 1),
            (Math.Max(8, (int)Math.Ceiling(population * 0.4)), 2),
            (Math.Max(6, (int)Math.Ceiling(population * 0.16)), 4),
            (Math.Max(4, (int)Math.Ceiling(population * 0.08)), Config.MaxSeeds),
        ];

        var rungs = new List<(int Keep, int Seeds)>();
        foreach (var step in steps)
        {
            if (step.Seeds > Config.MaxSeeds) continue;
            if (rungs.Count > 0 && rungs[^1].Seeds >= step.Seeds) continue;
            rungs.Add((Math.Min(step.Keep, population), step.Seeds));
        }

        return rungs;
    }

    private IReadOnlyList<Scored> ScoreRound(int round, Candidate[] population, CancellationToken ct)
    {
        var block = round / Math.Max(1, Config.SeedRotation);
        var seeds = Seeds.Set(Config.MasterSeed, Sim.Seeds.TrainSalt, block, Config.MaxSeeds);
        var alive = population;
        IReadOnlyList<Scored> scored = [];

        foreach (var rung in Rungs(population.Length))
        {
            ct.ThrowIfCancellationRequested();
            var rungSeeds = seeds.Take(rung.Seeds).ToArray();
            Measure(alive, rungSeeds, round, ct);
            scored = BlockRank(alive, rungSeeds);

            // Anything measured on more than one seed is worth remembering: the archive is what the
            // held-out scoring draws from, and keeping only the last rung makes it far too narrow.
            if (rung.Seeds >= 2)
                foreach (var s in scored)
                    Remember(s);

            alive = [.. scored.Take(Math.Min(rung.Keep, scored.Count)).Select(s => s.Candidate)];
            if (alive.Length <= 1) break;
        }

        var kept = alive.ToHashSet();
        return [.. scored.Where(s => kept.Contains(s.Candidate))];
    }

    private void Measure(IReadOnlyList<Candidate> candidates, uint[] seeds, int round, CancellationToken ct)
    {
        var jobs = new List<EvalJob>();
        for (var i = 0; i < candidates.Count; i++)
        {
            var key = candidates[i].Strategy.Key();
            foreach (var seed in seeds)
                if (!_objective.ContainsKey($"{key}@{seed}"))
                    jobs.Add(new EvalJob(i, seed, candidates[i].Strategy));
        }

        if (jobs.Count == 0) return;

        Report(SearchPhase.Searching, round, 0, jobs.Count);
        var results = Pool.Evaluate(jobs, Config.Campaign, ct);
        for (var j = 0; j < jobs.Count; j++)
        {
            var key = $"{candidates[jobs[j].Candidate].Strategy.Key()}@{jobs[j].Seed}";
            _objective[key] = Objective(results[j]);
            _raw[key] = results[j];
        }

        _campaigns += jobs.Count;
        Report(SearchPhase.Searching, round, jobs.Count, jobs.Count);
    }

    private void Remember(Scored s)
    {
        var key = s.Candidate.Strategy.Key();
        if (_archive.TryGetValue(key, out var prev) &&
            (prev.Seeds > s.Seeds || (prev.Seeds == s.Seeds && prev.Mean <= s.Mean))) return;
        _archive[key] = s;
    }

    /// <summary>
    /// Ranks on the residual after the seed's own difficulty is taken out: a candidate is measured
    /// by how far it beat the rest of the field on each seed, not by the raw seconds it took.
    /// Strictly better than ranking the means, and it costs one extra pass.
    /// </summary>
    private IReadOnlyList<Scored> BlockRank(IReadOnlyList<Candidate> candidates, uint[] seeds)
    {
        var objectives = candidates
            .Select(c => seeds.Select(s => _objective.GetValueOrDefault($"{c.Strategy.Key()}@{s}", double.PositiveInfinity)).ToArray())
            .ToArray();

        var seedMean = new double[seeds.Length];
        for (var s = 0; s < seeds.Length; s++)
            seedMean[s] = Average(objectives.Select(row => row[s]).Where(double.IsFinite));

        return
        [
            .. candidates.Select((c, i) =>
            {
                var row = objectives[i];
                var finite = row.Where(double.IsFinite).ToArray();
                return new Scored(
                    c,
                    Average(row.Select((v, s) => v - seedMean[s]).Where(double.IsFinite)),
                    finite.Length > 0 ? Average(finite) : double.PositiveInfinity,
                    finite.Length);
            }).OrderBy(s => s.Z),
        ];
    }

    // ---------------------------------------------------------------- the finals

    private IReadOnlyList<Finalist> FinalScore(List<Scored> shortlist, CancellationToken ct)
    {
        if (shortlist.Count == 0)
        {
            Report(SearchPhase.Done, RoundCount - 1, 0, 0);
            return [];
        }

        var seeds = Seeds.Set(Config.MasterSeed, Sim.Seeds.HoldoutSalt, 0, Config.FinalSeeds);
        var jobs = new List<EvalJob>();
        for (var i = 0; i < shortlist.Count; i++)
            foreach (var seed in seeds)
                jobs.Add(new EvalJob(i, seed, shortlist[i].Candidate.Strategy));

        Report(SearchPhase.Finals, RoundCount - 1, 0, jobs.Count);
        var results = Pool.Evaluate(jobs, Config.Campaign, ct);
        _campaigns += jobs.Count;

        var rows = new List<(int Candidate, uint Seed, SlimResult Result)>(jobs.Count);
        for (var j = 0; j < jobs.Count; j++) rows.Add((jobs[j].Candidate, jobs[j].Seed, results[j]));

        var finalists = rows.GroupBy(r => r.Candidate)
            .Select(g =>
            {
                var list = g.ToArray();
                var objectives = list.Select(x => Objective(x.Result)).ToArray();
                var mean = Average(objectives);
                var sd = StdDev(objectives, mean);
                var representative = list
                    .OrderBy(x => Math.Abs(Objective(x.Result) - mean))
                    .First();
                return new Finalist(
                    shortlist[g.Key].Candidate.Strategy,
                    shortlist[g.Key].Candidate.Label,
                    mean,
                    list.Length > 1 ? sd / Math.Sqrt(list.Length) : 0,
                    list.Count(x => x.Result.Reached) / (double)list.Length,
                    Average(list.Select(x => (double)x.Result.BestCycle)),
                    Average(list.Select(x => (double)x.Result.Runs)),
                    representative.Seed,
                    null);
            })
            .OrderBy(f => f.Score)
            .ToList();

        // Top K, plus any reference that did not make it - the comparison is the point of having them.
        var keep = finalists.Take(Config.TopK).ToList();
        keep.AddRange(finalists.Skip(Config.TopK).Where(f => f.Label is not null));

        var withDetail = new List<Finalist>(keep.Count);
        for (var i = 0; i < keep.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            Report(SearchPhase.Charts, RoundCount - 1, i, keep.Count);
            withDetail.Add(keep[i] with
            {
                Detail = Evaluator.Detail(keep[i].Strategy, keep[i].RepresentativeSeed, Config.Campaign, ct),
            });
            _campaigns++;
        }

        Report(SearchPhase.Done, RoundCount - 1, withDetail.Count, withDetail.Count);
        return withDetail;
    }

    // ---------------------------------------------------------------- scoring and thinning

    /// <summary>
    /// Lower is better, in campaign seconds. A campaign that never reached the target is parked
    /// beyond anything a successful one can score and ordered by how close it got, so the censored
    /// tail still has a gradient for the search to climb.
    /// </summary>
    public double Objective(SlimResult r) =>
        r.Reached
            ? r.TimeToTarget
            : Config.Campaign.TimeBudget * 4 +
              (Config.Campaign.TargetCycle - r.BestCycle) * (Config.Campaign.TimeBudget / Config.Campaign.TargetCycle);

    /// <summary>
    /// Thins a ranked list down to entries that actually differ. Weight vectors are normalised to
    /// one and compared on total mass moved, so two builds that spend their DNA the same way with
    /// different absolute numbers count as one - otherwise the top five is five copies of the same
    /// winner with rounding noise between them.
    /// </summary>
    private static List<Scored> Diverse(IReadOnlyList<Scored> ranked, int want)
    {
        const double minDistance = 0.18;
        var kept = new List<(Scored Scored, double[] Shape)>();

        foreach (var s in ranked)
        {
            if (kept.Count >= want) break;
            var shape = Normalise(s.Candidate.Strategy.LabWeights);
            if (kept.All(k => Distance(shape, k.Shape) >= minDistance)) kept.Add((s, shape));
        }

        // A search that converged hard can leave too few distinct builds; top up with the next best.
        foreach (var s in ranked)
        {
            if (kept.Count >= want) break;
            if (kept.All(k => k.Scored != s)) kept.Add((s, Normalise(s.Candidate.Strategy.LabWeights)));
        }

        return [.. kept.Select(k => k.Scored)];
    }

    private static double[] Normalise(double[] weights)
    {
        var total = weights.Sum(w => Math.Max(0, w));
        if (total <= 0) total = 1;
        return [.. weights.Select(w => Math.Max(0, w) / total)];
    }

    private static double Distance(double[] a, double[] b) =>
        a.Select((x, i) => Math.Abs(x - b[i])).Sum() / 2;

    // ---------------------------------------------------------------- reference builds

    /// <summary>
    /// Fixed points that run whether or not the search keeps them: flat weights with the Toxicity
    /// gate on and off, and the buy order the browser build's console probe always used. Without
    /// them a ranking of strangers says nothing about whether the search found anything.
    /// </summary>
    protected IReadOnlyList<Candidate> References()
    {
        var probe = Strategy.Uniform();
        for (var rank = 0; rank < ProbeOrder.Length; rank++)
            probe[(int)ProbeOrder[rank]] = ProbeOrder.Length - rank;

        return
        [
            new Candidate(Strategy.Flat(damageGate: true), "flat, gate on"),
            new Candidate(Strategy.Flat(damageGate: false), "flat, gate off"),
            new Candidate(new Strategy(probe, (double[])probe.Clone(), true, 5), "console probe order"),
        ];
    }

    private static readonly UpgradeId[] ProbeOrder =
    [
        UpgradeId.Damage, UpgradeId.AttackSpeed, UpgradeId.Regen, UpgradeId.Health, UpgradeId.Range,
        UpgradeId.AtpBonus, UpgradeId.CritChance, UpgradeId.CritDamage, UpgradeId.DefAbs, UpgradeId.DefPct,
        UpgradeId.MultishotChance, UpgradeId.BounceChance, UpgradeId.MultishotTargets,
        UpgradeId.BounceTargets, UpgradeId.BounceRange,
    ];

    // ---------------------------------------------------------------- progress

    private void Report(SearchPhase phase, int round, int done, int total)
    {
        var best = double.PositiveInfinity;
        var bestCycle = 0;
        foreach (var r in _raw.Values)
        {
            if (r.Reached && r.TimeToTarget < best) best = r.TimeToTarget;
            if (r.BestCycle > bestCycle) bestCycle = r.BestCycle;
        }

        Progress = new SearchProgress(phase, round, RoundCount, done, total, _campaigns, best, bestCycle);
    }

    protected static double Average(IEnumerable<double> xs)
    {
        var sum = 0.0;
        var n = 0;
        foreach (var x in xs)
        {
            sum += x;
            n++;
        }

        return n == 0 ? 0 : sum / n;
    }

    protected static double StdDev(IReadOnlyCollection<double> xs, double mean) =>
        xs.Count > 1 ? Math.Sqrt(xs.Sum(x => (x - mean) * (x - mean)) / (xs.Count - 1)) : 0;
}
