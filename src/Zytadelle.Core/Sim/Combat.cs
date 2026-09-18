using System.Diagnostics;

using Zytadelle.Core.Engine;
using Zytadelle.Core.Entities;
using Zytadelle.Core.Upgrades;

namespace Zytadelle.Core.Sim;

public static class Combat
{
    // ---------------------------------------------------------------- damage

    public static (double Dmg, bool Crit) RollDamage(double baseDmg, double critChance, double critDamage, Rng rng)
    {
        var crit = rng.Chance(critChance);
        return (CombatBalance.ApplyCrit(baseDmg, critDamage, crit), crit);
    }

    /// <summary>
    /// <paramref name="lifeSteal"/> is false for Thorns' reflect damage - it doesn't count as
    /// damage the cell dealt, so it never heals the cell (matches how Thorns excludes itself in
    /// the reference this was modelled on). Life steal also refuses to heal a cell that is
    /// already dead: <see cref="UpdateProjectiles"/> can still land a later toxin on the same
    /// tick a shot killed the cell, and healing then would leave a dead cell with positive
    /// integrity packed into the death frame the results screen draws.
    /// </summary>
    public static void DamageEnemy(World w, ref Enemy e, double dmg, bool lifeSteal = true)
    {
        if (!e.Alive) return;
        e.Hp -= dmg;
        e.Flash = RenderBalance.EnemyFlash;
        if (lifeSteal && !w.Dead && w.Stats.LifeSteal > 0)
            w.Cell.Hp = Math.Min(w.Cell.MaxHp, w.Cell.Hp + dmg * w.Stats.LifeSteal);
        if (e.Hp <= 0) Kill(w, ref e);
    }

    public static void Kill(World w, ref Enemy e)
    {
        if (!e.Alive) return;
        e.Alive = false;
        w.DeadEnemies++;
        w.Kills++;
        w.KillsByKind = w.KillsByKind.With(e.Kind, w.KillsByKind.Of(e.Kind) + 1);

        var atp = w.BaseAtpPerKillC * w.Stats.AtpBonus;
        w.Atp += atp;
        w.AtpEarned += atp;
        w.Dna += EconomyBalance.DnaForKill(e.Dna, e.SpawnCycle, w.Cycle, w.Stats.DnaPerKill, w.Infection.DnaMult);

        w.PushFx(e.Kind == EnemyKind.Boss ? FxKind.BossKill : FxKind.Kill, e.X, e.Y, atp);
    }

    /// <summary>Resistance % first, then Cell Wall; the remainder reaches integrity.</summary>
    public static double DamageCell(World w, double raw)
    {
        var taken = CombatBalance.ApplyIncoming(raw, w.Stats.DefPct, w.Stats.DefAbs);
        w.Cell.Hp -= taken;
        w.Cell.Flash = RenderBalance.CellFlash;
        if (w.Cell.Hp > 0) return taken;
        w.Cell.Hp = 0;
        w.Dead = true;
        return taken;
    }

    // ---------------------------------------------------------------- the cell

    /// <summary>
    /// One shot, plus a granule burst on a proc. The random draws happen in a fixed order - damage
    /// roll, then the diffusion roll, then the burst gate - and the last two only happen when their
    /// chance is above zero. Changing that order changes every culture from the same seed on.
    /// </summary>
    public static void UpdateCellFire(World w, double dt)
    {
        w.Cell.FireCd -= dt;
        if (w.Cell.FireCd > 0) return;

        var s = w.Stats;
        var targetIdx = Targeting.NearestInRange(w.Enemies.AsSpan(), s.Range);
        if (targetIdx < 0) return;

        // An assignment, not an accumulation: the overshoot is dropped, so the fire rate is quantised
        // to the tick. That is how the browser build behaved and the balance was fitted against it.
        w.Cell.FireCd = 1 / s.AttackSpeed;

        SpawnToxin(w, targetIdx, RollDamage(s.Damage, s.CritChance, s.CritDamage, w.Rng), RollBounces(w, s));

        var extra = s.MultishotChance > 0 && w.Rng.Chance(s.MultishotChance)
            ? CombatBalance.MultishotExtraShots(s.MultishotTargets)
            : 0;
        if (extra <= 0) return;

        // Never two toxins on the same pathogen: with nothing else in reach the extra toxins are
        // simply not released, so a lone boss stays a pure damage check. Reused scratch instead of
        // a fresh List per volley - grown lazily, so a build that never buys Multishot never pays.
        if (w.MultishotScratch.Length < extra + 1) w.MultishotScratch = new int[extra + 1];
        var taken = w.MultishotScratch;
        taken[0] = w.Enemies[targetIdx].Id;
        var takenCount = 1;
        for (var i = 0; i < extra; i++)
        {
            var nextIdx = Targeting.NearestFrom(w.Enemies.AsSpan(), 0, 0, s.Range, taken.AsSpan(0, takenCount));
            if (nextIdx < 0) break;
            taken[takenCount++] = w.Enemies[nextIdx].Id;
            SpawnToxin(w, nextIdx, RollDamage(s.Damage, s.CritChance, s.CritDamage, w.Rng), RollBounces(w, s));
        }
    }

    private static int RollBounces(World w, Stats s) =>
        s.BounceChance > 0 && w.Rng.Chance(s.BounceChance) ? CombatBalance.BounceHops(s.BounceTargets) : 0;

    private static void SpawnToxin(World w, int targetIndex, (double Dmg, bool Crit) roll, int bounces)
    {
        ref readonly var target = ref w.Enemies[targetIndex];
        var dx = target.X;
        var dy = target.Y;
        var d = Math.Sqrt(dx * dx + dy * dy);
        if (d == 0) d = 1;

        // Capacity is exactly bounces + 1: Bounces only ever counts down from here, so this is the
        // most this toxin's hit list can ever hold across its whole lifetime.
        var hitSlot = -1;
        if (bounces > 0)
        {
            hitSlot = w.HitSlab.Rent(bounces + 1);
            w.HitSlab.Set(hitSlot, 0, target.Id);
        }

        ref var proj = ref w.Projectiles.AddRef();
        proj.Id = w.NextId++;
        proj.X = dx / d * CellBalance.Radius;
        proj.Y = dy / d * CellBalance.Radius;
        proj.Vx = dx / d * CellBalance.ProjectileSpeed;
        proj.Vy = dy / d * CellBalance.ProjectileSpeed;
        proj.Speed = CellBalance.ProjectileSpeed;
        proj.Dmg = roll.Dmg;
        proj.Crit = roll.Crit;
        proj.FromCell = true;
        proj.TargetIndex = targetIndex;
        proj.TargetId = target.Id;
        proj.Bounces = bounces;
        proj.HitSlot = hitSlot;
        proj.HitCount = bounces > 0 ? 1 : 0;
        // A toxin from the cell never has a shooter to reflect Thorns onto.
        proj.ShooterIndex = -1;
        proj.ShooterId = -1;
        proj.Life = CellBalance.ProjectileLifetime;
        // Struct default is false, not the class field initializer it replaces.
        proj.Alive = true;
        Debug.Assert(proj.Alive, "Projectile spawned without its required explicit Alive=true.");
    }

    public static void UpdateRegen(World w, double dt)
    {
        if (w.Cell.Flash > 0) w.Cell.Flash -= dt;
        if (w.Cell.Hp < w.Cell.MaxHp) w.Cell.Hp = Math.Min(w.Cell.MaxHp, w.Cell.Hp + w.Stats.Regen * dt);
    }

    // ---------------------------------------------------------------- compaction

    /// <summary>
    /// Compacts w.Enemies in place - same stable-order semantics as the List.RemoveAll it used to
    /// be - and remaps every projectile's TargetIndex to match: a still-alive target keeps being
    /// tracked at its new position, one that died lands on -1, exactly as if a dangling reference
    /// had gone null. Always walks every projectile when called, even ones that did not die this
    /// tick and even if none of them target the pathogen that did: a compaction shifts survivors'
    /// positions regardless of which specific enemy died, so the fixup cannot be narrowed any
    /// further than "ran at all".
    /// </summary>
    public static void CompactEnemies(World w)
    {
        if (w.EnemyRemap.Length < w.Enemies.Count) w.EnemyRemap = new int[w.Enemies.Count];
        var remap = w.EnemyRemap.AsSpan(0, w.Enemies.Count);
        w.Enemies.Compact(remap);

        foreach (ref var p in w.Projectiles.AsSpan())
        {
            if (p.TargetIndex >= 0) p.TargetIndex = remap[p.TargetIndex];
            if (p.ShooterIndex >= 0) p.ShooterIndex = remap[p.ShooterIndex];
        }
    }

    // ---------------------------------------------------------------- projectiles

    /// <summary>Kills a projectile and returns its hit-list slot (if it rented one) to the slab -
    /// the one place that does both, so a death site can never free a projectile without freeing
    /// what it was renting, or vice versa.</summary>
    private static void KillProjectile(World w, ref Projectile p)
    {
        p.Alive = false;
        w.DeadProjectiles++;
        if (p.HitSlot < 0) return;
        w.HitSlab.Free(p.HitSlot);
        p.HitSlot = -1;
    }

    /// <summary>
    /// Moves every toxin and shot. A diffusing toxin carries its full damage on from where it
    /// landed, keeps its rupture flag across every hop, and never returns to a pathogen it already
    /// hit - but the rupture is counted once per toxin, not once per hop.
    /// </summary>
    public static void UpdateProjectiles(World w, double dt)
    {
        for (var i = 0; i < w.Projectiles.Count; i++)
        {
            ref var p = ref w.Projectiles[i];
            if (!p.Alive) continue;

            p.Life -= dt;
            if (p.Life <= 0)
            {
                KillProjectile(w, ref p);
                continue;
            }

            if (p.FromCell)
            {
                var targetIdx = p.TargetIndex;
                if (targetIdx < 0 || !w.Enemies[targetIdx].Alive)
                {
                    KillProjectile(w, ref p);
                    continue;
                }

                ref var t = ref w.Enemies[targetIdx];
                Debug.Assert(t.Id == p.TargetId, "TargetIndex/TargetId out of sync - a compaction remap bug.");

                var dx = t.X - p.X;
                var dy = t.Y - p.Y;
                var d = Math.Sqrt(dx * dx + dy * dy);
                var stepLen = p.Speed * dt;

                if (d <= stepLen + t.Radius)
                {
                    if (p.Crit)
                    {
                        if (p.Hops == 0) w.Crits++;
                        w.PushFx(FxKind.Crit, t.X, t.Y, p.Dmg);
                    }
                    DamageEnemy(w, ref t, p.Dmg);

                    var nextIdx = p.Bounces > 0
                        ? Targeting.NearestFrom(w.Enemies.AsSpan(), t.X, t.Y, w.Stats.BounceRange, w.HitSlab.Span(p.HitSlot, p.HitCount))
                        : -1;
                    if (nextIdx < 0)
                    {
                        KillProjectile(w, ref p);
                        continue;
                    }

                    ref var next = ref w.Enemies[nextIdx];
                    p.Bounces--;
                    p.Hops++;
                    w.HitSlab.Set(p.HitSlot, p.HitCount, next.Id);
                    p.HitCount++;
                    p.TargetIndex = nextIdx;
                    p.TargetId = next.Id;
                    p.X = t.X;
                    p.Y = t.Y;
                    p.Life = Math.Max(p.Life, CellBalance.RicochetHopLifetime);
                    continue;
                }

                p.Vx = dx / d * p.Speed;
                p.Vy = dy / d * p.Speed;
                p.X += p.Vx * dt;
                p.Y += p.Vy * dt;
            }
            else
            {
                p.X += p.Vx * dt;
                p.Y += p.Vy * dt;
                if (Math.Sqrt(p.X * p.X + p.Y * p.Y) > CellBalance.Radius) continue;
                KillProjectile(w, ref p);
                DamageCell(w, p.Dmg);
                w.PushFx(FxKind.Hit, p.X, p.Y);

                if (w.Stats.Thorns > 0 && p.ShooterIndex >= 0)
                {
                    ref var shooter = ref w.Enemies[p.ShooterIndex];
                    if (shooter.Alive && shooter.Id == p.ShooterId)
                        DamageEnemy(w, ref shooter, shooter.MaxHp * CombatBalance.ThornsAgainst(w.Stats.Thorns, shooter.Kind == EnemyKind.Boss), lifeSteal: false);
                }
            }
        }
    }

    // ---------------------------------------------------------------- pathogens

    /// <summary>
    /// Moves every pathogen towards its stop distance and lets it attack from there. A shooting
    /// pathogen stops at a fraction of the player's current Reach, so buying Reach pushes the phages
    /// further out on the very next tick.
    /// </summary>
    public static void UpdateEnemies(World w, double dt)
    {
        for (var i = 0; i < w.Enemies.Count; i++)
        {
            ref var e = ref w.Enemies[i];
            if (!e.Alive) continue;

            if (e.Flash > 0) e.Flash -= dt;
            e.AttackCd -= dt;

            var d = Math.Sqrt(e.X * e.X + e.Y * e.Y);
            if (d == 0) d = 1;
            var stopDist = e.IsRanged
                ? w.Stats.Range * e.RangeFrac
                : CellBalance.Radius + e.Radius;

            if (d > stopDist)
            {
                var mv = Math.Min(e.Speed * dt, d - stopDist);
                e.X -= e.X / d * mv;
                e.Y -= e.Y / d * mv;
                continue;
            }

            if (!e.Arrived)
            {
                e.Arrived = true;
                if (e.IsRanged) e.AttackCd = e.Windup;
            }

            if (e.AttackCd > 0) continue;

            e.AttackCd = e.AttackInterval;
            if (e.IsRanged) Shoot(w, ref e, i); else HitCell(w, ref e);
            if (w.Dead) return;
        }
    }

    private static void HitCell(World w, ref Enemy e)
    {
        DamageCell(w, e.Atk * e.DmgMult);
        w.PushFx(FxKind.Hit, e.X, e.Y);
        if (w.Stats.Thorns > 0)
            DamageEnemy(w, ref e, e.MaxHp * CombatBalance.ThornsAgainst(w.Stats.Thorns, e.Kind == EnemyKind.Boss), lifeSteal: false);
        e.DmgMult += CombatBalance.HeatupPerHit;
    }

    private static void Shoot(World w, ref Enemy e, int index)
    {
        var d = Math.Sqrt(e.X * e.X + e.Y * e.Y);
        if (d == 0) d = 1;
        var spd = e.IsRanged ? e.ProjectileSpeed : EnemyBalance.ProjectileFallbackSpeed;

        ref var proj = ref w.Projectiles.AddRef();
        proj.Id = w.NextId++;
        proj.X = e.X;
        proj.Y = e.Y;
        proj.Vx = -e.X / d * spd;
        proj.Vy = -e.Y / d * spd;
        proj.Speed = spd;
        proj.Dmg = e.Atk * e.DmgMult;
        proj.Life = EnemyBalance.ProjectileLifetime;
        // This shot never has a target to track - explicit -1, since the struct default is 0.
        proj.TargetIndex = -1;
        proj.TargetId = -1;
        proj.HitSlot = -1;
        // Who fired it, for Thorns to reflect back onto on landing - remapped by CompactEnemies
        // exactly like TargetIndex/TargetId above.
        proj.ShooterIndex = index;
        proj.ShooterId = e.Id;
        proj.Alive = true;
        Debug.Assert(proj.Alive, "Projectile spawned without its required explicit Alive=true.");

        e.DmgMult += CombatBalance.HeatupPerHit;
    }
}
