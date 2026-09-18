namespace Zytadelle.Core.Tests.Determinism;

/// <summary>
/// The one place that reads simulation state for the goldens. Field order is a frozen contract
/// (bumped only by <see cref="Version"/>, never silently): the refactor in the plan changes how
/// <c>World</c> stores its entities, but every value and the order it is written in here must stay
/// exactly what it is today. Anything this probe does not read - <c>Enemy.Flash</c>, <c>Cell.Flash</c>,
/// <c>Fx</c>, the income windows - is deliberately render-only, so a headless run with
/// <c>RecordFx=false</c> still hashes identically to one that renders.
/// </summary>
public static class WorldProbe
{
    public const int Version = 1;

    public static void Write(World w, WordWriter buf)
    {
        buf.F64(w.Time);
        buf.I32(w.Cycle);
        buf.I32((int)w.Phase);
        buf.F64(w.PhaseTimer);
        buf.F64(w.SpawnTimer);
        buf.F64(w.Atp);
        buf.F64(w.Dna);
        buf.F64(w.AtpEarned);
        buf.I32(w.Kills);
        buf.I32(w.Crits);
        buf.I32(w.RunBuys);
        buf.I32(w.NextId);
        buf.Bool(w.Dead);

        buf.F64(w.KillsByKind.Basic);
        buf.F64(w.KillsByKind.Fast);
        buf.F64(w.KillsByKind.Tank);
        buf.F64(w.KillsByKind.Ranged);
        buf.F64(w.KillsByKind.Boss);

        buf.F64(w.Cell.Hp);
        buf.F64(w.Cell.MaxHp);
        buf.F64(w.Cell.FireCd);

        foreach (var id in GeneIds.All) buf.I32(w.Run[id]);

        buf.U32(w.Rng.State);

        buf.I32(w.Enemies.Count);
        foreach (var e in w.Enemies)
        {
            buf.I32(e.Id);
            buf.I32((int)e.Kind);
            buf.F64(e.X);
            buf.F64(e.Y);
            buf.F64(e.Hp);
            buf.F64(e.MaxHp);
            buf.F64(e.Atk);
            buf.F64(e.Speed);
            buf.I32(e.SpawnCycle);
            buf.F64(e.DmgMult);
            buf.F64(e.AttackCd);
            buf.Bool(e.Arrived);
            buf.Bool(e.Alive);
        }

        buf.I32(w.Projectiles.Count);
        foreach (var p in w.Projectiles)
        {
            buf.I32(p.Id);
            buf.F64(p.X);
            buf.F64(p.Y);
            buf.F64(p.Vx);
            buf.F64(p.Vy);
            buf.F64(p.Speed);
            buf.F64(p.Dmg);
            buf.Bool(p.Crit);
            buf.Bool(p.FromCell);
            buf.I32(p.TargetId);
            buf.I32(p.Bounces);
            buf.I32(p.Hops);
            buf.F64(p.Life);
            buf.Bool(p.Alive);
            buf.I32(p.HitCount);
            if (p.HitSlot < 0) continue;
            foreach (var hitId in w.HitSlab.Span(p.HitSlot, p.HitCount)) buf.I32(hitId);
        }
    }
}
