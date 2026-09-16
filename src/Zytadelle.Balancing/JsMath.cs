namespace Zytadelle.Balancing;

/// <summary>
/// The handful of JavaScript numeric semantics the ported balance formulas depend on.
/// Using <see cref="Math.Round(double)"/> instead would silently shift every price by one,
/// because .NET rounds halves to even and JavaScript rounds them towards +infinity.
/// </summary>
public static class JsMath
{
    /// <summary>JavaScript <c>Math.round</c>: halves go towards +infinity, not to even.</summary>
    public static double Round(double v) => Math.Floor(v + 0.5);

    /// <summary>JavaScript <c>Math.imul</c>: 32-bit signed multiply, low 32 bits kept.</summary>
    public static uint Imul(uint a, uint b) => unchecked((uint)((int)a * (int)b));
}
