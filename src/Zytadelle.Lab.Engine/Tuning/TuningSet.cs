using System.Text.Json;

namespace Zytadelle.Lab.Engine.Tuning;

/// <summary>
/// A set of balance values, as path to number. The shipped set is captured once before anything can
/// have been applied, and every other set is read against it.
///
/// Reading a pasted or stored set is deliberately tolerant, the way the save is: every path the
/// schema knows is taken from the input when it is there and finite, and falls back to the shipped
/// value when it is not. Unknown paths are dropped. A set stored before the balance grew therefore
/// still loads, and so does one written after it shrank.
/// </summary>
public sealed class TuningSet
{
    private static readonly Lazy<IReadOnlyDictionary<string, double>> ShippedValues =
        new(() => TuningSchema.Fields.ToDictionary(f => f.Path, f => f.Shipped, StringComparer.Ordinal),
            isThreadSafe: true);

    private readonly Dictionary<string, double> _values;

    private TuningSet(Dictionary<string, double> values) => _values = values;

    public IReadOnlyDictionary<string, double> Values => _values;

    /// <summary>What the game ships with, captured before the first apply could have run.</summary>
    public static TuningSet Shipped() => new(new Dictionary<string, double>(ShippedValues.Value, StringComparer.Ordinal));

    /// <summary>The values in force right now, detached from them.</summary>
    public static TuningSet Read() =>
        new(TuningSchema.Fields.ToDictionary(f => f.Path, f => f.Get(), StringComparer.Ordinal));

    public double this[string path] => _values.TryGetValue(path, out var v) ? v : 0;

    public TuningSet With(string path, double value)
    {
        var copy = new Dictionary<string, double>(_values, StringComparer.Ordinal) { [path] = value };
        return new TuningSet(copy);
    }

    /// <summary>Paths whose value differs from the shipped one - what the form marks as touched.</summary>
    public IEnumerable<string> Changed() =>
        _values.Where(kv => ShippedValues.Value.TryGetValue(kv.Key, out var s) && !Same(kv.Value, s))
            .Select(kv => kv.Key);

    /// <summary>
    /// Writes the set into the balance project and drops the curve caches it invalidated. Process
    /// wide by nature, which is why the Lab applies one set per session and never during a search.
    /// </summary>
    public void Apply()
    {
        foreach (var f in TuningSchema.Fields)
            if (_values.TryGetValue(f.Path, out var v) && !Same(v, f.Get()))
                f.Set(v);

        TuningSchema.ResetCurveCaches();
    }

    public string ToJson() => JsonSerializer.Serialize(_values, JsonOptions);

    /// <summary>Rebuilds a full set from whatever came out of storage or a paste. Never throws.</summary>
    public static TuningSet FromJson(string? json)
    {
        var shipped = Shipped();
        if (string.IsNullOrWhiteSpace(json)) return shipped;

        try
        {
            if (JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions) is not { } raw) return shipped;
            foreach (var (path, element) in raw)
            {
                if (!shipped._values.ContainsKey(path)) continue;
                if (element.ValueKind is JsonValueKind.Number && element.TryGetDouble(out var v) && double.IsFinite(v))
                    shipped._values[path] = v;
            }

            return shipped;
        }
        catch (JsonException)
        {
            return shipped;
        }
    }

    /// <summary>
    /// Curve constants are fitted to six significant figures and have to survive a round trip
    /// through text, so equality is on the round-tripped value rather than on a tolerance.
    /// </summary>
    private static bool Same(double a, double b) => a.Equals(b);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
