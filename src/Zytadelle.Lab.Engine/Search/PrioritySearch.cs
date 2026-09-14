using Zytadelle.Balancing;
using Zytadelle.Core.Engine;
using Zytadelle.Core.Upgrades;
using Zytadelle.Lab.Engine.Sim;

namespace Zytadelle.Lab.Engine.Search;

/// <summary>
/// A cross-entropy method over the log of the gene weights.
///
/// Nineteen genes cannot be enumerated - the caps run to 6000 levels - so what is searched is the
/// priority vector behind a build, not the build. Each round samples a population from a diagonal
/// Gaussian in log-weight space, scores it, refits the Gaussian to the elites and repeats. Log
/// space because weights are ratios; diagonal because the relative-gain normalisation in
/// <see cref="LabAllocator.RelGain"/> already takes the units out, which is most of what a full
/// covariance would have had to learn.
/// </summary>
public sealed class PrioritySearch(SearchConfig config, Evaluator evaluator) : SearchRun(config, evaluator)
{
    /// <summary>Weights are ratios, so a log weight this far from the mean is already a pin or a ban.</summary>
    private const double LogClamp = 6;

    private readonly Rng _rng = new(Seeds.Mix(config.MasterSeed));
    private readonly double[] _mu = new double[Length(config)];
    private readonly double[] _sigma = Enumerable.Repeat(config.Sigma0, Length(config)).ToArray();
    private double _gateProbability = 0.5;

    protected override int RoundCount => Config.Generations;

    /// <summary>One log weight per searched gene - twice over when in-run is separate - plus unlockK.</summary>
    private static int Length(SearchConfig cfg) => cfg.Genes.Count * (cfg.SeparateRunWeights ? 2 : 1) + 1;

    protected override IEnumerable<Candidate[]> Rounds()
    {
        for (var g = 0; g < Config.Generations; g++)
        {
            if (g == 0)
            {
                var references = References();
                var rest = Sample(Math.Max(0, Config.Population - references.Count), 0.5);
                yield return [.. references, .. rest];
            }
            else
            {
                yield return Sample(Config.Population, _gateProbability);
            }
        }
    }

    protected override void Absorb(IReadOnlyList<Scored> survivors)
    {
        if (survivors.Count == 0) return;
        var eliteCount = Math.Max(3, (int)Math.Ceiling(Config.EliteFraction * survivors.Count));
        var elites = survivors.Take(eliteCount).Where(s => s.Candidate.Vector is not null).ToArray();
        if (elites.Length == 0) return;

        for (var i = 0; i < _mu.Length; i++)
        {
            var xs = elites.Select(e => e.Candidate.Vector![i]).ToArray();
            var mean = Average(xs);
            _mu[i] = mean;
            _sigma[i] = Math.Max(Config.SigmaMin, StdDev(xs, mean));
        }

        var gated = elites.Count(e => e.Candidate.Strategy.DamageGate) / (double)elites.Length;
        _gateProbability = Math.Clamp(gated, 0.1, 0.9);
    }

    // ---------------------------------------------------------------- the vector

    private Candidate[] Sample(int count, double gateProbability)
    {
        var pop = new Candidate[Math.Max(0, count)];
        for (var i = 0; i < pop.Length; i++)
        {
            var x = new double[_mu.Length];
            for (var j = 0; j < x.Length; j++) x[j] = _mu[j] + _sigma[j] * Gauss();
            var gate = _rng.Next() < gateProbability;
            pop[i] = new Candidate(FromVector(x, gate), null, x);
        }

        return pop;
    }

    /// <summary>Box-Muller off the game's own generator, so the whole search runs on one seed.</summary>
    private double Gauss()
    {
        var u = Math.Max(1e-12, _rng.Next());
        return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * _rng.Next());
    }

    public Strategy FromVector(double[] x, bool gate)
    {
        var n = Config.Genes.Count;
        var lab = Block(x, 0);
        var run = Config.SeparateRunWeights ? Block(x, n) : (double[])lab.Clone();
        var u = Math.Clamp(x[^1], -2, 2);
        var unlockK = Math.Clamp((int)Math.Round(5 * Math.Exp(u)), 1, 20);
        return new Strategy(lab, run, gate, unlockK);
    }

    /// <summary>
    /// Only ratios matter, so the block is re-centred on its own mean. Without that the population
    /// drifts along a direction that changes nothing, and the spread stops meaning anything.
    /// </summary>
    private double[] Block(double[] x, int from)
    {
        var genes = Config.Genes;
        var mean = 0.0;
        for (var i = 0; i < genes.Count; i++) mean += x[from + i];
        mean /= Math.Max(1, genes.Count);

        var w = new double[UpgradeIds.Count];
        Array.Fill(w, Config.PinnedWeight);
        for (var i = 0; i < genes.Count; i++)
            w[(int)genes[i]] = Math.Exp(Math.Clamp(x[from + i] - mean, -LogClamp, LogClamp));

        return w;
    }
}
