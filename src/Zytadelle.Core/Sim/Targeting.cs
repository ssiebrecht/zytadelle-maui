using Zytadelle.Core.Entities;

namespace Zytadelle.Core.Sim;

/// <summary>
/// Who the cell shoots at, and where a diffusing toxin goes next.
///
/// Both comparisons are "less than or equal", so on an exact distance tie the pathogen that appears
/// later in the list wins. That makes the list order part of the simulation state: spawns append,
/// and compaction has to keep the surviving order.
/// </summary>
public static class Targeting
{
    /// <summary>Nearest living pathogen within range of the cell, or null.</summary>
    public static Enemy? NearestInRange(List<Enemy> enemies, double range)
    {
        Enemy? best = null;
        var bestD = range * range;
        foreach (var e in enemies)
        {
            if (!e.Alive) continue;
            var d = e.X * e.X + e.Y * e.Y;
            if (d > bestD) continue;
            best = e;
            bestD = d;
        }
        return best;
    }

    /// <summary>Nearest living pathogen within range of a point, skipping the ones already hit.</summary>
    public static Enemy? NearestFrom(List<Enemy> enemies, double x, double y, double range, List<int> skip)
    {
        Enemy? best = null;
        var bestD = range * range;
        foreach (var e in enemies)
        {
            if (!e.Alive || skip.Contains(e.Id)) continue;
            var dx = e.X - x;
            var dy = e.Y - y;
            var d = dx * dx + dy * dy;
            if (d > bestD) continue;
            best = e;
            bestD = d;
        }
        return best;
    }
}
