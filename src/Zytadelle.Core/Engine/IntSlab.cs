using System.Diagnostics;

namespace Zytadelle.Core.Engine;

/// <summary>
/// A small pool allocator for the growable int-lists a bouncing toxin needs (the pathogens it has
/// already hit), so a bounce-capable shot stops allocating a <c>List&lt;int&gt;</c> - and that
/// list's own separately growing backing array - every time one is fired.
///
/// A handle is a slot <em>index</em>, not a raw offset into the backing buffer: each rental records
/// its own offset and capacity once, in side tables, so one toxin needing more room than another
/// (a later BounceTargets purchase can raise how much a newly spawned toxin asks for, mid-run) never
/// disturbs any other still-live handle - only the backing buffer itself grows, tail-appended, when
/// nothing freed is big enough to reuse. Freed slots are tracked in a small list and reused
/// first-fit; a linear scan of it is fine at the scale this ever runs at (bounce-capable projectiles
/// alive at once, not entities in general).
/// </summary>
public sealed class IntSlab
{
    private int[] _buf;
    private int _bufUsed;
    private int[] _offset = new int[16];
    private int[] _capacity = new int[16];
    private int _slotCount;
    private readonly List<int> _free = [];
#if DEBUG
    private readonly HashSet<int> _rented = [];
#endif

    public IntSlab(int initialCapacity) => _buf = new int[initialCapacity];

    /// <summary>Reuses a freed slot with enough room, first-fit, or bump-allocates a fresh one.</summary>
    public int Rent(int minLen)
    {
        for (var i = _free.Count - 1; i >= 0; i--)
        {
            var freeSlot = _free[i];
            if (_capacity[freeSlot] < minLen) continue;
            _free.RemoveAt(i);
#if DEBUG
            _rented.Add(freeSlot);
#endif
            return freeSlot;
        }

        if (_bufUsed + minLen > _buf.Length)
            Array.Resize(ref _buf, Math.Max(_buf.Length * 2, _bufUsed + minLen));
        if (_slotCount == _offset.Length)
        {
            Array.Resize(ref _offset, _offset.Length * 2);
            Array.Resize(ref _capacity, _capacity.Length * 2);
        }

        var slot = _slotCount++;
        _offset[slot] = _bufUsed;
        _capacity[slot] = minLen;
        _bufUsed += minLen;
#if DEBUG
        _rented.Add(slot);
#endif
        return slot;
    }

    public void Free(int slot)
    {
#if DEBUG
        Debug.Assert(_rented.Remove(slot), "IntSlab.Free: slot is not currently rented - double free?");
#endif
        _free.Add(slot);
    }

    public Span<int> Span(int slot, int len)
    {
#if DEBUG
        Debug.Assert(_rented.Contains(slot), "IntSlab.Span: freed slot not in use.");
#endif
        return _buf.AsSpan(_offset[slot], len);
    }

    public void Set(int slot, int index, int value)
    {
#if DEBUG
        Debug.Assert(_rented.Contains(slot), "IntSlab.Set: freed slot not in use.");
#endif
        _buf[_offset[slot] + index] = value;
    }
}
