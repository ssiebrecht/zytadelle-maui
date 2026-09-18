using System.IO.Hashing;

namespace Zytadelle.Core.Tests.Determinism;

/// <summary>
/// A running XxHash3 over a whole simulation trajectory. One <see cref="Append"/> call per tick
/// (the tick's <see cref="WordWriter"/> buffer), so the final hash is sensitive to the entire
/// history, not just the end state - a divergence that "heals" itself two cycles later still moves
/// every hash after it. <see cref="Checkpoint"/> reads the running value without resetting it, so
/// the same instance produces both the per-cycle checkpoints and the final hash.
/// </summary>
public sealed class StateHasher
{
    private readonly XxHash3 _hash = new();

    public void Append(ReadOnlySpan<byte> bytes) => _hash.Append(bytes);

    public ulong Checkpoint() => _hash.GetCurrentHashAsUInt64();
}
