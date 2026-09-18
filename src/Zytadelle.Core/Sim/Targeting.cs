using System.Runtime.InteropServices;

using Zytadelle.Core.Entities;

namespace Zytadelle.Core.Sim;

/// <summary>
/// Who the cell shoots at, and where a diffusing toxin goes next. Returns an index into the list
/// passed in (or -1 for "nobody"), not a reference - callers hold onto it as
/// <see cref="Projectile.TargetIndex"/>, valid only until the next enemy compaction.
///
/// Both comparisons are "less than or equal", so on an exact distance tie the pathogen that appears
/// later in the list wins. That makes the list order part of the simulation state: spawns append,
/// and compaction has to keep the surviving order.
/// </summary>
public static class Targeting
{
    /// <summary>Index of the nearest living pathogen within range of the cell, or -1.</summary>
    public static int NearestInRange(List<Enemy> enemies, double range)
    {
        var best = -1;
        var bestD = range * range;
        for (var i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (!e.Alive) continue;
            var d = e.X * e.X + e.Y * e.Y;
            if (d > bestD) continue;
            best = i;
            bestD = d;
        }
        return best;
    }

    /// <summary>Index of the nearest living pathogen within range of a point, skipping the ones
    /// already hit, or -1.</summary>
    public static int NearestFrom(List<Enemy> enemies, double x, double y, double range, List<int> skip)
    {
        var skipSpan = CollectionsMarshal.AsSpan(skip);
        var best = -1;
        var bestD = range * range;
        for (var i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (!e.Alive || skipSpan.Contains(e.Id)) continue;
            var dx = e.X - x;
            var dy = e.Y - y;
            var d = dx * dx + dy * dy;
            if (d > bestD) continue;
            best = i;
            bestD = d;
        }
        return best;
    }
}
