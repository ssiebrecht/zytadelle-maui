using Zytadelle.Core.Engine;

namespace Zytadelle.Core.Sim;

public static class Step
{
    /// <summary>
    /// One fixed simulation step. Order: cycles and spawning, pathogens, the cell's shot, toxins,
    /// regeneration, then bookkeeping.
    ///
    /// The three checks for death in the middle matter: when the cell dies mid-tick, world time does
    /// not advance, dead entities are not compacted and the income windows are not written. The
    /// results screen then draws exactly the frame the breach happened in.
    /// </summary>
    public static void Run(World w, double dt)
    {
        if (w.Dead) return;

        var atpBefore = w.Atp;
        var dnaBefore = w.Dna;

        Cycles.Update(w, dt);
        Combat.UpdateEnemies(w, dt);
        if (w.Dead) return;
        Combat.UpdateCellFire(w, dt);
        Combat.UpdateProjectiles(w, dt);
        if (w.Dead) return;
        Combat.UpdateRegen(w, dt);

        w.Time += dt;

        if (w.DeadEnemies > 0)
        {
            Combat.CompactEnemies(w);
            w.DeadEnemies = 0;
        }
        if (w.DeadProjectiles > 0)
        {
            w.Projectiles.RemoveAll(p => !p.Alive);
            w.DeadProjectiles = 0;
        }

        if (w.RecordFx) w.Fx.Age(dt);

        if (w.TrackIncome)
        {
            TrackRate(w.AtpWindow, w.Atp - atpBefore, w.Time, dt);
            TrackRate(w.DnaWindow, w.Dna - dnaBefore, w.Time, dt);
        }
    }

    /// <summary>One sample per simulated second, in a ring the per-minute readouts average over.</summary>
    private static void TrackRate(RateWindow win, double delta, double time, double dt)
    {
        var idx = (int)Math.Floor(time);
        var prevIdx = (int)Math.Floor(time - dt);
        if (win.Count == 0 || idx != prevIdx) win.AddSample();
        win.Last += delta;
    }

    /// <summary>Sum of a tracked window, scaled to per minute.</summary>
    public static double PerMinute(IReadOnlyList<double> win)
    {
        if (win.Count == 0) return 0;
        var s = 0.0;
        foreach (var v in win) s += v;
        return s / win.Count * 60;
    }
}
