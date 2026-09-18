using System.Buffers.Binary;

using Zytadelle.Core.Sim;

namespace Zytadelle.Core.Snapshot;

/// <summary>
/// Packs the part of the world the renderer actually reads into one little-endian buffer.
///
/// The decoder in <c>wwwroot/js/arena.js</c> reads the four sizes below off the host at boot rather
/// than repeating them, so a field added here only needs the matching read added there. Positions
/// and sizes are single precision because they end up as pixel coordinates, but world time stays
/// double: every animation phase derives from it, and a float loses sub-millisecond resolution
/// after a few hours of simulated time.
///
/// Worst case is roughly 13 KB per frame, a live one is 3 to 5 KB.
/// </summary>
public static class FrameEncoder
{
    public const int HeaderSize = 40;
    public const int EnemySize = 44;
    public const int ProjectileSize = 20;
    public const int FxSize = 20;

    /// <summary>
    /// Thread-static: a headless simulator steps many <c>World</c>s in parallel, and a shared buffer
    /// here would let one thread's frame overwrite another's mid-encode. The App only ever calls
    /// this from its own frame-clock thread, so for it this is just one buffer, same as before.
    /// </summary>
    [ThreadStatic]
    private static byte[]? _scratch;

    /// <summary>The largest frame the current caps can produce, so the usual frame never resizes.</summary>
    private static int WorstCase() =>
        HeaderSize
        + SimulationBalance.MaxEnemies * EnemySize
        + RenderBalance.MaxProjectiles * ProjectileSize
        + (SimulationBalance.FxRingCap + 1) * FxSize;

    public static byte[] Encode(World w)
    {
        var enemyCount = Math.Min(w.Enemies.Count, SimulationBalance.MaxEnemies);
        var projCount = Math.Min(w.Projectiles.Count, RenderBalance.MaxProjectiles);
        var fxCount = Math.Min(w.Fx.Count, SimulationBalance.FxRingCap + 1);

        var size = HeaderSize + enemyCount * EnemySize + projCount * ProjectileSize + fxCount * FxSize;
        var scratch = _scratch;
        if (scratch is null || scratch.Length < size) _scratch = scratch = new byte[Math.Max(size, WorstCase())];
        var b = scratch.AsSpan(0, size);

        BinaryPrimitives.WriteDoubleLittleEndian(b, w.Time);
        WriteF32(b, 8, w.Cell.Hp);
        WriteF32(b, 12, w.Cell.MaxHp);
        WriteF32(b, 16, w.Cell.Flash);
        WriteF32(b, 20, w.Cell.FireCd);
        WriteF32(b, 24, w.Stats.Range);
        WriteF32(b, 28, w.Stats.AttackSpeed);
        BinaryPrimitives.WriteUInt16LittleEndian(b[32..], (ushort)enemyCount);
        BinaryPrimitives.WriteUInt16LittleEndian(b[34..], (ushort)projCount);
        BinaryPrimitives.WriteUInt16LittleEndian(b[36..], (ushort)fxCount);
        b[38] = 0;
        b[39] = 0;

        var o = HeaderSize;
        for (var i = 0; i < enemyCount; i++)
        {
            var e = w.Enemies[i];
            BinaryPrimitives.WriteInt32LittleEndian(b[o..], e.Id);
            b[o + 4] = (byte)e.Kind;
            b[o + 5] = e.Arrived ? (byte)1 : (byte)0;
            b[o + 6] = 0;
            b[o + 7] = 0;
            WriteF32(b, o + 8, e.X);
            WriteF32(b, o + 12, e.Y);
            WriteF32(b, o + 16, e.Radius);
            WriteF32(b, o + 20, e.Hp);
            WriteF32(b, o + 24, e.MaxHp);
            WriteF32(b, o + 28, e.Flash);
            WriteF32(b, o + 32, e.DmgMult);
            WriteF32(b, o + 36, e.AttackCd);
            WriteF32(b, o + 40, e.Def.AttackInterval);
            o += EnemySize;
        }

        for (var i = 0; i < projCount; i++)
        {
            var p = w.Projectiles[i];
            WriteF32(b, o, p.X);
            WriteF32(b, o + 4, p.Y);
            WriteF32(b, o + 8, p.Vx);
            WriteF32(b, o + 12, p.Vy);
            b[o + 16] = (byte)((p.Crit ? 1 : 0) | (p.FromCell ? 2 : 0));
            b[o + 17] = 0;
            b[o + 18] = 0;
            b[o + 19] = 0;
            o += ProjectileSize;
        }

        for (var i = 0; i < fxCount; i++)
        {
            var f = w.Fx[i];
            b[o] = (byte)f.Kind;
            b[o + 1] = 0;
            b[o + 2] = 0;
            b[o + 3] = 0;
            WriteF32(b, o + 4, f.X);
            WriteF32(b, o + 8, f.Y);
            WriteF32(b, o + 12, f.T);
            WriteF32(b, o + 16, f.Value);
            o += FxSize;
        }

        return b.ToArray();
    }

    private static void WriteF32(Span<byte> b, int offset, double value) =>
        BinaryPrimitives.WriteSingleLittleEndian(b[offset..], (float)value);
}
