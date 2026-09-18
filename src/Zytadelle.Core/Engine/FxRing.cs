using System.Collections;

using Zytadelle.Core.Entities;

namespace Zytadelle.Core.Engine;

/// <summary>
/// The live Fx events, replacing a <c>List&lt;FxEvent&gt;</c> that shifted every remaining element
/// on <c>RemoveAt(0)</c>/<c>RemoveAll(age &gt; lifetime)</c>. Capacity is fixed at
/// <see cref="SimulationBalance.FxRingCap"/> + 1 - the exact steady-state size the list settled on,
/// since its own push only evicted the oldest once already <em>over</em> the cap, not at it. That
/// off-by-one is preserved rather than "fixed": golden hashes are pinned to it.
///
/// <see cref="Age"/> is a prefix-pop: every live Fx ages by the same <c>dt</c> each tick, so age is
/// non-increasing from oldest to newest, and the moment the oldest is back within the lifetime,
/// every newer one already is too. That makes the old <c>RemoveAll(f =&gt; f.T &gt; Lifetime)</c> -
/// an O(n) scan of the whole list - equivalent to popping only the leading run that is over, O(evicted).
/// </summary>
public sealed class FxRing : IReadOnlyList<FxEvent>
{
    private readonly FxEvent[] _buf;
    private int _start;
    private int _count;

    public FxRing(int cap) => _buf = new FxEvent[cap + 1];

    public int Count => _count;

    public FxEvent this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count) throw new ArgumentOutOfRangeException(nameof(index));
            return _buf[(_start + index) % _buf.Length];
        }
    }

    public void Push(FxKind kind, double x, double y, double value)
    {
        var cap = _buf.Length;
        if (_count == cap)
        {
            _start = (_start + 1) % cap;
            _count--;
        }
        _buf[(_start + _count) % cap] = new FxEvent { Kind = kind, X = x, Y = y, Value = value };
        _count++;
    }

    public void Age(double dt)
    {
        var cap = _buf.Length;
        for (var i = 0; i < _count; i++) _buf[(_start + i) % cap].T += dt;

        var lifetime = SimulationBalance.FxLifetime;
        while (_count > 0 && _buf[_start].T > lifetime)
        {
            _start = (_start + 1) % cap;
            _count--;
        }
    }

    public IEnumerator<FxEvent> GetEnumerator()
    {
        for (var i = 0; i < _count; i++) yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
