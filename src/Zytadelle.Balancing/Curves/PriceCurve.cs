namespace Zytadelle.Balancing.Curves;

/// <summary>
/// One uninterrupted power law: FirstCost * (1 + purchasedLevels / Ramp)^Exponent. No starter
/// resets, bracket multipliers or tail segments.
/// </summary>
public sealed class PriceCurve(double firstCost, double ramp, double exponent)
{
    /// <summary>Cost of the first purchase, already including the inexpensive opening.</summary>
    public double FirstCost { get; init; } = firstCost;

    /// <summary>Opening length; larger values delay the steep part of the curve.</summary>
    public double Ramp { get; init; } = ramp;

    /// <summary>Long-term price power; must exceed the value increment power for declining efficiency.</summary>
    public double Exponent { get; init; } = exponent;

    /// <summary>Rounded price of buying from <paramref name="level"/> to <paramref name="level"/> + 1 (0-based).</summary>
    public double CostAt(int level) =>
        Math.Max(1, JsMath.Round(FirstCost * Math.Pow(1 + Math.Max(0, level) / Ramp, Exponent)));
}
