using System.Collections;

namespace Zytadelle.Core.Engine;

/// <summary>
/// The last <see cref="SimulationBalance.RateWindowSeconds"/> seconds of income, one sample per
/// simulated second. Replaces a <c>List&lt;double&gt;</c> that grew with <c>Add</c> and, once over
/// capacity, shifted every remaining element down with <c>RemoveAt(0)</c> - fixed capacity turns
/// that into an O(1) ring instead. Enumerates oldest to newest, exactly like the list it replaces,
/// so <see cref="Sim.Step.PerMinute"/> and the render/App/probe readers that just iterate it are
/// unaffected. Capacity is fixed at construction: Balancing statics are read-only for the lifetime
/// of a campaign (see the plan's parallelism constraint), so nothing needs a live-resize path.
/// </summary>
public sealed class RateWindow : IReadOnlyList<double>
{
    private readonly double[] _buf;
    private int _start;
    private int _count;

    public RateWindow(int capacity) => _buf = new double[Math.Max(1, capacity)];

    public int Count => _count;

    public double this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count) throw new ArgumentOutOfRangeException(nameof(index));
            return _buf[(_start + index) % _buf.Length];
        }
    }

    /// <summary>The most recently opened sample. Only valid once <see cref="AddSample"/> has been
    /// called at least once - true from the first tracked tick on, same as the list's <c>[^1]</c>.</summary>
    public ref double Last => ref _buf[(_start + _count - 1) % _buf.Length];

    /// <summary>Opens a new zero sample, evicting the oldest once the ring is already at capacity -
    /// the same net effect as the list's <c>Add(0)</c> followed by a conditional <c>RemoveAt(0)</c>.</summary>
    public void AddSample()
    {
        var cap = _buf.Length;
        if (_count < cap)
        {
            _buf[(_start + _count) % cap] = 0;
            _count++;
        }
        else
        {
            _start = (_start + 1) % cap;
            _buf[(_start + _count - 1) % cap] = 0;
        }
    }

    public IEnumerator<double> GetEnumerator()
    {
        for (var i = 0; i < _count; i++) yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
