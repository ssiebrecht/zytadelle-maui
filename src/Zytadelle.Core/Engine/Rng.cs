namespace Zytadelle.Core.Engine;

/// <summary>
/// Seedable PRNG (mulberry32), ported bit for bit from the browser build. It is the only source of
/// randomness in the simulation, so the same seed and the same starting levels must always produce
/// the same culture - and the same day key must always roll the same five missions.
/// </summary>
public sealed class Rng(uint seed = 0x9e3779b9u)
{
    private uint _s = seed;

    /// <summary>Raw generator state, for determinism probes only - not part of the public engine surface.</summary>
    internal uint State => _s;

    public double Next()
    {
        _s = unchecked(_s + 0x6d2b79f5u);
        var t = _s;
        t = JsMath.Imul(t ^ (t >> 15), t | 1u);
        t ^= unchecked(t + JsMath.Imul(t ^ (t >> 7), t | 61u));
        return (t ^ (t >> 14)) / 4294967296.0;
    }

    public bool Chance(double p) => Next() < p;

    public double Range(double min, double max) => min + (max - min) * Next();

    /// <summary>The seed a culture gets when none was supplied, mirroring the browser expression.</summary>
    public static uint TimeSeed() =>
        unchecked((uint)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() ^ Random.Shared.Next()));
}
