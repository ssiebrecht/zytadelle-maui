namespace Zytadelle.Balancing.Curves;

/// <summary>
/// A cubic in <c>ln(cycle)</c>. Spawn rate and the type mix used to be step tables over 31 wave
/// thresholds; they are these curves now, pinned so cycle 1 matches the old table exactly.
/// </summary>
/// <param name="Base">Value at cycle 1, where the curve is pinned to the old step table.</param>
/// <param name="Log">Weight of ln(cycle).</param>
/// <param name="Log2">Weight of ln(cycle) squared.</param>
/// <param name="Log3">Weight of ln(cycle) cubed.</param>
public readonly record struct LogCurve(double Base, double Log, double Log2, double Log3)
{
    public double At(double cycle)
    {
        var l = Math.Log(Math.Max(1, cycle));
        return Base + Log * l + Log2 * l * l + Log3 * l * l * l;
    }
}
