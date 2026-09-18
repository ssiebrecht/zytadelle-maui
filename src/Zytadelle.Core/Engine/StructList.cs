using System.Diagnostics;

namespace Zytadelle.Core.Engine;

/// <summary>An entity a <see cref="StructList{T}"/> can hold - just enough to compact stably.</summary>
public interface IEntity
{
    bool Alive { get; }
}

/// <summary>
/// Backs World.Enemies/Projectiles: a growable array of value-type entities instead of a
/// List&lt;T&gt; of classes, so a spawn/shot is a slot write instead of an allocation and every
/// tick's loops walk contiguous memory instead of chasing a class pointer.
///
/// <see cref="AddRef"/> hands back a zeroed slot - the same "everything not explicitly set is the
/// type's default" guarantee a fresh <c>new SomeClass()</c> gave. Slots get reused across
/// compactions, so without zeroing, a new entity would silently inherit whatever a previous
/// occupant of that slot left behind in any field it does not itself overwrite.
///
/// Never hold a ref returned from here across another call that can grow this same list (another
/// <see cref="AddRef"/>, or a resize inside <see cref="Compact()"/>'s scratch) - like any array or
/// Span, a grow can move the backing storage and strand the old reference.
/// </summary>
public sealed class StructList<T> where T : struct, IEntity
{
    private T[] _buf;
    private int _count;
    private int[]? _compactScratch;

    public StructList(int capacity) => _buf = new T[Math.Max(1, capacity)];

    public int Count => _count;

    public ref T this[int index]
    {
        get
        {
            Debug.Assert((uint)index < (uint)_count, "StructList index out of range.");
            return ref _buf[index];
        }
    }

    public Span<T> AsSpan() => _buf.AsSpan(0, _count);

    public void Clear() => _count = 0;

    /// <summary>Appends a zeroed slot and returns a ref to it, growing the backing array if it is full.</summary>
    public ref T AddRef()
    {
        if (_count == _buf.Length) Array.Resize(ref _buf, _buf.Length * 2);
        _buf[_count] = default;
        return ref _buf[_count++];
    }

    /// <summary>
    /// Stable in-place compaction: dead entries dropped, survivors keep their relative order - the
    /// same semantics as the <c>List&lt;T&gt;.RemoveAll</c> it replaces. Writes
    /// <c>remap[oldIndex] = newIndex</c>, or -1 for a dropped entry, so a caller tracking indices
    /// into this list (<c>Projectile.TargetIndex</c> into <c>World.Enemies</c>) can fix them up.
    /// <paramref name="remap"/> must be at least <see cref="Count"/> long.
    /// </summary>
    public void Compact(Span<int> remap)
    {
        var write = 0;
        for (var read = 0; read < _count; read++)
        {
            if (!_buf[read].Alive)
            {
                remap[read] = -1;
                continue;
            }
            remap[read] = write;
            if (write != read) _buf[write] = _buf[read];
            write++;
        }
        _count = write;
    }

    /// <summary>Same compaction, for a caller with nothing that tracks indices into this list -
    /// reuses an internal scratch buffer instead of asking the caller for one.</summary>
    public void Compact()
    {
        if (_compactScratch is null || _compactScratch.Length < _count) _compactScratch = new int[_count];
        Compact(_compactScratch.AsSpan(0, _count));
    }
}
