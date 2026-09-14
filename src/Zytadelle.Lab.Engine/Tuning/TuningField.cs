namespace Zytadelle.Lab.Engine.Tuning;

/// <summary>What kind of input a field wants. Everything is carried as a double regardless.</summary>
public enum TuningKind
{
    Double,
    Int,
    Bool,
}

/// <summary>
/// One editable number in <c>Zytadelle.Balancing</c>, addressed by a dotted path and reached
/// through a pair of closures the schema built while it walked there.
/// </summary>
public sealed class TuningField
{
    /// <summary>Dotted path, e.g. <c>UpgradeBalance.All.Damage.Dna.Curve.Rate</c>. Stable across runs.</summary>
    public required string Path { get; init; }

    /// <summary>The static class the path starts in - the group the form puts it under.</summary>
    public required string Group { get; init; }

    /// <summary>The path without its group prefix.</summary>
    public required string Label { get; init; }

    public required TuningKind Kind { get; init; }

    /// <summary>The <c>///</c> line from the source, if the member carries one.</summary>
    public string? Hint { get; init; }

    /// <summary>The value this field had before anything was applied.</summary>
    public required double Shipped { get; init; }

    public required Func<double> Get { get; init; }

    public required Action<double> Set { get; init; }
}
