using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing;

/// <summary>
/// The two rules that sit on top of every price curve: the half-price opening levels, and the
/// bracket jumps. Both are reproduced rather than fitted away, because the shipped tables have them.
/// </summary>
public static class PricingBalance
{
    /// <summary>How many opening levels cost half of what the curve asks.</summary>
    public static int StarterLevels { get; set; } = 2;

    /// <summary>What those opening levels are multiplied by.</summary>
    public static double StarterDiscount { get; set; } = 0.5;

    /// <summary>
    /// Price of buying from <paramref name="level"/> to <paramref name="level"/> + 1 (0-based).
    /// Levels 1 and 2 are half of the curve's first two steps; level 3 then restarts the curve at
    /// its own first step at full price. Brackets are tested against that shifted step, not against
    /// the level, and every bracket that is reached multiplies in.
    /// </summary>
    public static double CostAt(PriceCurve price, int level)
    {
        var bought = Math.Max(0, level) + 1;
        var starter = bought <= StarterLevels;
        double x = starter ? bought : bought - StarterLevels;
        var raw = price.Curve.ValueAt(x) * BracketMult(price.Brackets, x);
        return Math.Max(1, JsMath.Round(starter ? raw * StarterDiscount : raw));
    }

    private static double BracketMult(IReadOnlyList<Bracket>? brackets, double x)
    {
        if (brackets is null) return 1;
        var m = 1.0;
        foreach (var b in brackets)
            if (x >= b.From)
                m *= b.Mult;
        return m;
    }
}
