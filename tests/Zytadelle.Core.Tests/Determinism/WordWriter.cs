using System.Buffers.Binary;

namespace Zytadelle.Core.Tests.Determinism;

/// <summary>
/// A reusable little-endian byte buffer. <see cref="WorldProbe"/> fills one per tick and the runner
/// feeds the whole thing to <see cref="StateHasher"/> in a single call, rather than one hash update
/// per field - hashing a few hundred thousand ticks would otherwise dominate golden-generation time.
///
/// Doubles are written as their raw IEEE-754 bits (<see cref="BitConverter.DoubleToInt64Bits"/>), not
/// formatted text: two bit patterns are either identical or not, with no rounding-in-comparison to
/// second-guess.
/// </summary>
public sealed class WordWriter
{
    private byte[] _buf = new byte[1024];
    private int _pos;

    public void Reset() => _pos = 0;

    public ReadOnlySpan<byte> Span => _buf.AsSpan(0, _pos);

    private Span<byte> Reserve(int n)
    {
        if (_pos + n > _buf.Length) Array.Resize(ref _buf, Math.Max(_buf.Length * 2, _pos + n));
        var s = _buf.AsSpan(_pos, n);
        _pos += n;
        return s;
    }

    public void F64(double v) => BinaryPrimitives.WriteInt64LittleEndian(Reserve(8), BitConverter.DoubleToInt64Bits(v));

    public void I32(int v) => BinaryPrimitives.WriteInt32LittleEndian(Reserve(4), v);

    public void U32(uint v) => BinaryPrimitives.WriteUInt32LittleEndian(Reserve(4), v);

    public void Bool(bool v) => Reserve(1)[0] = v ? (byte)1 : (byte)0;

    public void Bytes(ReadOnlySpan<byte> b) => b.CopyTo(Reserve(b.Length));
}
