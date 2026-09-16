namespace Zytadelle.Balancing.Curves;

/// <summary>
/// Positive power-law increments with a small deterministic wave. Their sum is normalized to
/// GainAtCap, so tuning the shape cannot accidentally increase a probability's maximum.
/// </summary>
public sealed class UpgradeValueCurve(double gainAtCap, double ramp, double exponent, double wavePhase,
    double waveAmplitude = 0.18, double wavePeriod = 10)
{
    /// <summary>Total value gained between level zero and the cap (excluding the base stat).</summary>
    public double GainAtCap { get; init; } = gainAtCap;

    /// <summary>Opening length: larger values delay the acceleration of increments.</summary>
    public double Ramp { get; init; } = ramp;

    /// <summary>Power of each increment; zero gives constant growth before the wave.</summary>
    public double Exponent { get; init; } = exponent;

    /// <summary>Fractional variation of each increment; must be in [0, 1).</summary>
    public double WaveAmplitude { get; init; } = waveAmplitude;

    /// <summary>Number of levels per efficiency wave, at least three.</summary>
    public double WavePeriod { get; init; } = wavePeriod;

    /// <summary>Wave offset in turns; different genes peak at different levels.</summary>
    public double WavePhase { get; init; } = wavePhase;

    private sealed record Cache(int Revision, double[] Sums);
    private volatile Cache? _cache;
    private readonly object _cacheLock = new();

    /// <summary>Cumulative gain at a total permanent + temporary level, clamped to the cap.</summary>
    public double SumTo(int level, int cap)
    {
        if (cap <= 0 || level <= 0) return 0;
        if (level >= cap) return GainAtCap;
        if (Exponent == 0 && WaveAmplitude == 0) return GainAtCap / cap * level;
        var cache = _cache;
        if (cache is null || cache.Revision != BalanceRevision.Current || cache.Sums.Length != cap + 1)
        {
            lock (_cacheLock)
            {
                cache = _cache;
                if (cache is null || cache.Revision != BalanceRevision.Current || cache.Sums.Length != cap + 1)
                {
                    var sums = new double[cap + 1];
                    for (var n = 0; n < cap; n++)
                    {
                        var weight = Math.Pow(1 + n / Ramp, Exponent)
                                     * (1 + WaveAmplitude * Math.Sin(2 * Math.PI * (n / WavePeriod + WavePhase)));
                        sums[n + 1] = sums[n] + weight;
                    }
                    // Publish complete, immutable sums: campaign searches share curves across workers.
                    _cache = cache = new Cache(BalanceRevision.Current, sums);
                }
            }
        }
        return GainAtCap * (cache.Sums[level] / cache.Sums[cap]);
    }

    /// <summary>Discard normalized prefix sums after a tuning edit.</summary>
    public void ResetSums() => _cache = null;
}
