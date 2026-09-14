using System.Globalization;
using System.Text;

namespace Zytadelle.Lab.Engine.Tuning;

/// <summary>What a tuning set changed, and the C# that would make the change permanent.</summary>
/// <param name="Path">The dotted path, as the schema names it.</param>
/// <param name="Shipped">What the game currently ships with.</param>
/// <param name="Tuned">What the session settled on.</param>
public sealed record TuningChange(string Path, double Shipped, double Tuned)
{
    public double Factor => Shipped == 0 ? double.NaN : Tuned / Shipped;
}

/// <summary>
/// The way a session ends. The browser build could load a tuning set straight into the game, but
/// this one reads its balance from compiled fields, so a number that stays in the Lab is a number
/// nobody gets to play. The export is therefore the point of the tool, not an extra: a list of what
/// moved, and the assignments that would move it in the source.
///
/// The C# is a starting point rather than a patch. Simple properties come out as complete
/// statements; a value inside a curve, a bracket or the gene table comes out as the assignment that
/// reaches it, which is a line a person still has to place by hand.
/// </summary>
public static class TuningDiff
{
    public static IReadOnlyList<TuningChange> Of(TuningSet set)
    {
        var shipped = TuningSet.Shipped();
        return TuningSchema.Fields
            .Where(f => !set[f.Path].Equals(shipped[f.Path]))
            .Select(f => new TuningChange(f.Path, shipped[f.Path], set[f.Path]))
            .ToList();
    }

    /// <summary>The changes as C#, grouped by the balance class each one belongs to.</summary>
    public static string ToCSharp(TuningSet set)
    {
        var changes = Of(set);
        if (changes.Count == 0) return "// Nothing changed - the session is on the shipped balance.";

        var sb = new StringBuilder();
        sb.AppendLine($"// {changes.Count} value(s) changed in the Balance Lab.");
        sb.AppendLine("// Paths reach into Zytadelle.Balancing; a value inside a curve or the gene");
        sb.AppendLine("// table has to be written where that curve is declared, not as a statement.");

        foreach (var group in changes.GroupBy(c => c.Path[..c.Path.IndexOf('.')]))
        {
            sb.AppendLine();
            sb.AppendLine($"// ---- {group.Key} ----");
            foreach (var c in group)
            {
                var nested = c.Path.Count(ch => ch == '.') > 1 || c.Path.Contains('[');
                sb.AppendLine($"{(nested ? "// " : string.Empty)}{c.Path} = {Literal(c.Tuned)};" +
                              $"   // was {Literal(c.Shipped)}{Factor(c)}");
            }
        }

        return sb.ToString();
    }

    /// <summary>Ten significant figures: the fitted curve constants carry six and must not be rounded.</summary>
    public static string Literal(double v) =>
        v == Math.Floor(v) && Math.Abs(v) < 1e15
            ? v.ToString("0", CultureInfo.InvariantCulture)
            : v.ToString("G10", CultureInfo.InvariantCulture);

    private static string Factor(TuningChange c) =>
        double.IsNaN(c.Factor) || double.IsInfinity(c.Factor)
            ? string.Empty
            : $", x{c.Factor.ToString("0.###", CultureInfo.InvariantCulture)}";
}
