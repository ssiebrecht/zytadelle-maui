namespace Zytadelle.Balancing.Curves;

/// <summary>
/// Integrated growth-rate curve used by enemy health and damage per cycle. Upgrades use the
/// simpler <see cref="PriceCurve"/> and <see cref="UpgradeValueCurve"/> power laws instead.
/// <code>
/// growth(x) = rate * (x + shift)^-decay + floor
/// value(x)  = base * exp( rate * (T(x + shift) - T(1 + shift)) + floor * (x - 1) )
/// </code>
/// A few curves change character near their cap and carry a second segment in <see cref="Tail"/>,
/// re-based so its own first step sits at <see cref="TailFrom"/>. Those breakpoints are often
/// fractional (965.5, 303.5, 163.5, 1848.5) and must not be rounded.
///
/// </summary>
public sealed class Curve
{
    /// <summary>Value at x = 1.</summary>
    public required double Base { get; init; }

    /// <summary>Growth per step at the start of the curve.</summary>
    public required double Rate { get; init; }

    /// <summary>How fast that growth flattens out.</summary>
    public required double Decay { get; init; }

    /// <summary>Growth per step the curve never drops below.</summary>
    public required double Floor { get; init; }

    /// <summary>Pushes the steep opening to the right.</summary>
    public required double Shift { get; init; }

    /// <summary>Second segment, re-based at <see cref="TailFrom"/>. Null for single-segment curves.</summary>
    public Curve? Tail { get; init; }

    /// <summary>Step at which <see cref="Tail"/> takes over. Ignored when <see cref="Tail"/> is null.</summary>
    public double TailFrom { get; init; }

    /// <summary>Running totals over steps 1..n, grown lazily as levels are bought.</summary>
    private List<double>? _sums;

    /// <summary>Revision the cached totals were built at; a retune past it invalidates them.</summary>
    private int _sumsRevision;

    /// <summary>Value of the curve at step x (x = 1 is the first step).</summary>
    public double ValueAt(double x) =>
        Tail is not null && x >= TailFrom ? Tail.SegmentAt(x - TailFrom + 1) : SegmentAt(x);

    /// <summary>
    /// Sum of the curve over steps 1..n. There is no closed form, so the running totals are cached on the curve itself and extended as levels are bought - a gene's
    /// value and the DNA sunk into it are both this. Retuning writes into the curve object that is
    /// already in place, so the memo is stamped with <see cref="BalanceRevision"/> and rebuilds
    /// itself once that moves; <see cref="ResetSums"/> drops it on demand.
    /// </summary>
    public double SumTo(double n)
    {
        var steps = (int)Math.Max(0, Math.Floor(n));
        if (steps == 0) return 0;

        if (_sums is null || _sumsRevision != BalanceRevision.Current)
        {
            _sums = [0];
            _sumsRevision = BalanceRevision.Current;
        }
        var acc = _sums;
        for (var x = acc.Count; x <= steps; x++) acc.Add(acc[x - 1] + ValueAt(x));
        return acc[steps];
    }

    /// <summary>
    /// Drops the cached running totals. <see cref="BalanceRevision"/> does this on its own for a
    /// tuning pass that remembers to bump; this is the explicit door for one that does not.
    /// </summary>
    public void ResetSums() => _sums = null;

    private double SegmentAt(double x) =>
        Base * Math.Exp(Rate * (Integral(x + Shift, Decay) - Integral(1 + Shift, Decay)) + Floor * (x - 1));

    private static double Integral(double u, double decay) =>
        Math.Abs(1 - decay) < 1e-9 ? Math.Log(u) : (Math.Pow(u, 1 - decay) - 1) / (1 - decay);
}
