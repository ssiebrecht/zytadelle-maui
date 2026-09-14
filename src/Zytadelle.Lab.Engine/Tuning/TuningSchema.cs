using System.Collections;
using System.Reflection;
using Zytadelle.Balancing;
using Zytadelle.Balancing.Curves;

namespace Zytadelle.Lab.Engine.Tuning;

/// <summary>
/// Every number in <c>Zytadelle.Balancing</c>, found by walking the project rather than by listing
/// it. A hand-written field list is the one thing that reliably drifts: the moment a constant is
/// added it is missing from the tool, and nothing says so. This walks, so the form stays complete.
///
/// What counts as a knob is a structural rule, not a blocklist: a public static member of a public
/// static class whose leaf is a double, int or bool, and whose leaf has a setter. Computed
/// properties such as <see cref="CycleBalance.CycleLength"/> have no setter and fall out by
/// themselves; enums and strings are not numbers; and the walk only descends into types from this
/// same project, so nothing from the BCL is ever entered. The one exception that needs saying out
/// loud is <see cref="BalanceIdentityAttribute"/>.
///
/// Writing is the awkward half. Curves and price tracks are classes and can be written where they
/// stand, but <c>Bracket</c> and <c>LogCurve</c> are readonly record structs: reflection mutates a
/// boxed copy, so every value type on the path has to be put back into its parent afterwards.
/// <see cref="Accessor.Write"/> does that innermost first and stops at the first reference type,
/// which was mutated in place and needs no write-back.
/// </summary>
public static class TuningSchema
{
    /// <summary>Reading order of the groups, following the table in the project README.</summary>
    private static readonly string[] GroupOrder =
    [
        nameof(ArenaBalance), nameof(CycleBalance), nameof(SimulationBalance), nameof(EnemyBalance),
        nameof(EnemyScalingBalance), nameof(SpawnBalance), nameof(InfectionBalance),
        nameof(UpgradeBalance), nameof(PricingBalance), nameof(EconomyBalance), nameof(CombatBalance),
        nameof(MissionBalance),
    ];

    /// <summary>Deep enough for <c>UpgradeBalance.All.Damage.Dna.Curve.Tail.Rate</c> and then some.</summary>
    private const int MaxDepth = 10;

    private static readonly List<Curve> KnownCurves = [];

    private static readonly Lazy<IReadOnlyList<TuningField>> Lazy = new(Discover, isThreadSafe: true);

    private static readonly Lazy<Dictionary<string, TuningField>> ByPath =
        new(() => Lazy.Value.ToDictionary(f => f.Path, StringComparer.Ordinal), isThreadSafe: true);

    /// <summary>Every field, in reading order. Built once; the closures stay valid for the process.</summary>
    public static IReadOnlyList<TuningField> Fields => Lazy.Value;

    public static TuningField? Find(string path) => ByPath.Value.GetValueOrDefault(path);

    /// <summary>
    /// Drops the cached running totals on every curve the walk passed. Values are written into the
    /// curve objects that are already in place, so without this a retuned curve would keep serving
    /// the sums of the shape it had before.
    /// </summary>
    public static void ResetCurveCaches()
    {
        _ = Fields;
        foreach (var c in KnownCurves) c.ResetSums();
    }

    // ---------------------------------------------------------------- the walk

    private static IReadOnlyList<TuningField> Discover()
    {
        var docs = XmlDocs.Load(typeof(ArenaBalance).Assembly);
        var found = new List<TuningField>();

        foreach (var type in RootTypes())
        foreach (var member in MembersOf(type, BindingFlags.Public | BindingFlags.Static))
            Walk(found, docs, type.Name, $"{type.Name}.{member.Name}", new Accessor([ToStep(member)]), member, 1);

        return found;
    }

    private static IEnumerable<Type> RootTypes() =>
        typeof(ArenaBalance).Assembly.GetExportedTypes()
            .Where(t => t is { IsAbstract: true, IsSealed: true, IsGenericType: false })
            .OrderBy(t => Array.IndexOf(GroupOrder, t.Name) is var i and >= 0 ? i : GroupOrder.Length)
            .ThenBy(t => t.Name, StringComparer.Ordinal);

    private static void Walk(List<TuningField> into, XmlDocs docs, string group, string path, Accessor acc, MemberInfo? source, int depth)
    {
        if (depth > MaxDepth) return;

        if (Kind(acc.Steps[^1].ValueType) is { } kind)
        {
            if (!acc.Writable) return;
            if (source?.GetCustomAttribute<BalanceIdentityAttribute>() is not null) return;
            var type = acc.Steps[^1].ValueType;
            double Get() => ToDouble(acc.Read());
            into.Add(new TuningField
            {
                Path = path,
                Group = group,
                Label = path[(group.Length + 1)..],
                Kind = kind,
                Hint = source is null ? null : docs.Summary(source),
                Shipped = Get(),
                Get = Get,
                Set = v => acc.Write(FromDouble(v, type)),
            });
            return;
        }

        var value = acc.Read();
        if (value is null or string or Enum) return;
        if (value is Curve curve && !KnownCurves.Contains(curve)) KnownCurves.Add(curve);

        switch (value)
        {
            case IDictionary dict:
                foreach (DictionaryEntry e in dict)
                {
                    var key = e.Key;
                    var name = Convert.ToString(key) ?? "?";
                    var step = new Step(name, e.Value?.GetType() ?? typeof(object),
                        p => ((IDictionary)p!)[key],
                        (p, v) => ((IDictionary)p!)[key] = v);
                    Walk(into, docs, group, $"{path}.{name}", acc.Then(step), null, depth + 1);
                }

                return;

            case IList list:
                for (var i = 0; i < list.Count; i++)
                {
                    var index = i;
                    var elem = list[i]?.GetType() ?? typeof(object);

                    // A collection expression compiles to a read-only wrapper, not an array, and its
                    // slots throw on assignment. Class elements are still fine - they are mutated
                    // where they stand - but a struct element there has no write path at all.
                    var writable = !list.IsReadOnly;
                    if (!writable && elem.IsValueType) continue;

                    var step = new Step($"[{i}]", elem,
                        p => ((IList)p!)[index],
                        writable ? (p, v) => ((IList)p!)[index] = v : null);
                    Walk(into, docs, group, $"{path}[{i}]", acc.Then(step), null, depth + 1);
                }

                return;

            // A sequence with no indexer cannot be written through, so it is not a knob. Skipping it
            // beats offering an input that quietly does nothing.
            case IEnumerable:
                return;
        }

        if (!IsOurs(value.GetType())) return;

        foreach (var member in MembersOf(value.GetType(), BindingFlags.Public | BindingFlags.Instance))
        {
            // A single-segment curve still has a TailFrom, and nothing ever reads it: ValueAt only
            // consults it when a tail exists. Offering it would be an input that does nothing.
            if (member.Name == nameof(Curve.TailFrom) && value is Curve { Tail: null }) continue;
            Walk(into, docs, group, $"{path}.{member.Name}", acc.Then(ToStep(member)), member, depth + 1);
        }
    }

    // ---------------------------------------------------------------- paths

    /// <summary>One step down a path: what to read, and how to write a value back where it came from.</summary>
    private sealed record Step(string Name, Type ValueType, Func<object?, object?> Get, Action<object?, object?>? Set);

    /// <summary>The whole chain from a static member down to one leaf, with the write-back rule.</summary>
    private sealed class Accessor(IReadOnlyList<Step> steps)
    {
        public IReadOnlyList<Step> Steps { get; } = steps;

        public bool Writable => Steps[^1].Set is not null;

        public Accessor Then(Step step) => new([.. Steps, step]);

        public object? Read()
        {
            object? cur = null;
            foreach (var s in Steps) cur = s.Get(cur);
            return cur;
        }

        public void Write(object? value)
        {
            // Read the whole chain first - the parents are needed again on the way back up.
            var chain = new object?[Steps.Count];
            object? cur = null;
            for (var i = 0; i < Steps.Count; i++) chain[i] = cur = Steps[i].Get(cur);

            Steps[^1].Set!(Steps.Count > 1 ? chain[^2] : null, value);

            // Reflection mutated a boxed copy, not the member it came from, so put the box back -
            // and keep going while what was written is still a value type.
            for (var i = Steps.Count - 2; i >= 0; i--)
            {
                if (!Steps[i].ValueType.IsValueType || Steps[i].Set is not { } set) break;
                set(i > 0 ? chain[i - 1] : null, chain[i]);
            }
        }
    }

    private static IEnumerable<MemberInfo> MembersOf(Type type, BindingFlags flags) =>
        type.GetProperties(flags)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Cast<MemberInfo>()
            .Concat(type.GetFields(flags).Where(f => !f.IsLiteral))
            .OrderBy(m => m.MetadataToken);

    private static Step ToStep(MemberInfo m) => m switch
    {
        // An init-only setter is an ordinary public setter carrying a modifier only the compiler
        // reads, so reflection may call it. That is what lets the fitted curves be written in place.
        PropertyInfo p => new Step(p.Name, p.PropertyType, o => p.GetValue(o),
            p.SetMethod is { IsPublic: true } ? (o, v) => p.SetValue(o, v) : null),
        FieldInfo f => new Step(f.Name, f.FieldType, o => f.GetValue(o),
            f.IsInitOnly ? null : (o, v) => f.SetValue(o, v)),
        _ => throw new ArgumentOutOfRangeException(nameof(m), m.Name, "not a readable member"),
    };

    // ---------------------------------------------------------------- leaves

    private static bool IsOurs(Type t) => t.Assembly == typeof(ArenaBalance).Assembly;

    private static TuningKind? Kind(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        // An int-backed enum answers Int32 to GetTypeCode, so it has to be turned away by name.
        if (t.IsEnum) return null;
        return Type.GetTypeCode(t) switch
        {
            TypeCode.Double or TypeCode.Single or TypeCode.Decimal => TuningKind.Double,
            TypeCode.Int32 or TypeCode.Int64 or TypeCode.Int16 or TypeCode.Byte => TuningKind.Int,
            TypeCode.Boolean => TuningKind.Bool,
            _ => null,
        };
    }

    private static double ToDouble(object? v) => v switch
    {
        bool b => b ? 1 : 0,
        IConvertible c => c.ToDouble(null),
        _ => 0,
    };

    private static object FromDouble(double v, Type target)
    {
        var t = Nullable.GetUnderlyingType(target) ?? target;
        if (t == typeof(bool)) return v != 0;
        if (t == typeof(double)) return v;
        if (t == typeof(float) || t == typeof(decimal)) return Convert.ChangeType(v, t);
        return Convert.ChangeType(Math.Round(v), t);
    }
}
