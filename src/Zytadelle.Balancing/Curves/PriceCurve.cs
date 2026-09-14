namespace Zytadelle.Balancing.Curves;

/// <summary>A price track: the curve itself plus the brackets that jump it.</summary>
public sealed class PriceCurve
{
    public required Curve Curve { get; init; }

    public IReadOnlyList<Bracket>? Brackets { get; init; }

    /// <summary>Price of buying from <paramref name="level"/> to <paramref name="level"/> + 1 (0-based).</summary>
    public double CostAt(int level) => PricingBalance.CostAt(this, level);
}
