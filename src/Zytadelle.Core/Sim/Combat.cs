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

    public static void DamageEnemy(World w, Enemy e, double dmg)
    {
        if (!e.Alive) return;
        e.Hp -= dmg;
        e.Flash = RenderBalance.EnemyFlash;
        if (e.Hp <= 0) Kill(w, e);
    }

    public static void Kill(World w, Enemy e)
    {
        if (!e.Alive) return;
        e.Alive = false;
        w.Kills++;
        w.KillsByKind = w.KillsByKind.With(e.Kind, w.KillsByKind.Of(e.Kind) + 1);

        var atp = EconomyBalance.AtpForKill(w.Cycle, w.Stats.AtpBonus);
        w.Atp += atp;
        w.AtpEarned += atp;
        w.Dna += EconomyBalance.DnaForKill(e.Def.Dna, e.SpawnCycle, w.Cycle, w.Stats.DnaPerKill, w.Infection.DnaMult);

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
        var target = Targeting.NearestInRange(w.Enemies, s.Range);
        if (target is null) return;

        // An assignment, not an accumulation: the overshoot is dropped, so the fire rate is quantised
        // to the tick. That is how the browser build behaved and the balance was fitted against it.
        w.Cell.FireCd = 1 / s.AttackSpeed;

        SpawnToxin(w, target, RollDamage(s.Damage, s.CritChance, s.CritDamage, w.Rng), RollBounces(w, s));

        var extra = s.MultishotChance > 0 && w.Rng.Chance(s.MultishotChance)
            ? CombatBalance.MultishotExtraShots(s.MultishotTargets)
            : 0;
        if (extra <= 0) return;

        // Never two toxins on the same pathogen: with nothing else in reach the extra toxins are
        // simply not released, so a lone boss stays a pure damage check.
        var taken = new List<int> { target.Id };
        for (var i = 0; i < extra; i++)
        {
            var next = Targeting.NearestFrom(w.Enemies, 0, 0, s.Range, taken);
            if (next is null) break;
            taken.Add(next.Id);
            SpawnToxin(w, next, RollDamage(s.Damage, s.CritChance, s.CritDamage, w.Rng), RollBounces(w, s));
        }
    }

    private static int RollBounces(World w, Stats s) =>
        s.BounceChance > 0 && w.Rng.Chance(s.BounceChance) ? CombatBalance.BounceHops(s.BounceTargets) : 0;

    private static void SpawnToxin(World w, Enemy target, (double Dmg, bool Crit) roll, int bounces)
    {
        var dx = target.X;
        var dy = target.Y;
        var d = Math.Sqrt(dx * dx + dy * dy);
        if (d == 0) d = 1;
        w.Projectiles.Add(new Projectile
        {
            Id = w.NextId++,
            X = dx / d * CellBalance.Radius,
            Y = dy / d * CellBalance.Radius,
            Vx = dx / d * CellBalance.ProjectileSpeed,
            Vy = dy / d * CellBalance.ProjectileSpeed,
            Speed = CellBalance.ProjectileSpeed,
            Dmg = roll.Dmg,
            Crit = roll.Crit,
            FromCell = true,
            Target = target,
            Bounces = bounces,
            Hit = bounces > 0 ? [target.Id] : null,
            Life = CellBalance.ProjectileLifetime,
        });
    }

    public static void UpdateRegen(World w, double dt)
    {
        if (w.Cell.Flash > 0) w.Cell.Flash -= dt;
        if (w.Cell.Hp < w.Cell.MaxHp) w.Cell.Hp = Math.Min(w.Cell.MaxHp, w.Cell.Hp + w.Stats.Regen * dt);
    }

    // ---------------------------------------------------------------- projectiles

    /// <summary>
    /// Moves every toxin and shot. A diffusing toxin carries its full damage on from where it
    /// landed, keeps its rupture flag across every hop, and never returns to a pathogen it already
    /// hit - but the rupture is counted once per toxin, not once per hop.
    /// </summary>
    public static void UpdateProjectiles(World w, double dt)
    {
        for (var i = 0; i < w.Projectiles.Count; i++)
        {
            var p = w.Projectiles[i];
            if (!p.Alive) continue;

            p.Life -= dt;
            if (p.Life <= 0)
            {
                p.Alive = false;
                continue;
            }

            if (p.FromCell)
            {
                var t = p.Target;
                if (t is null || !t.Alive)
                {
                    p.Alive = false;
                    continue;
                }

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
                    DamageEnemy(w, t, p.Dmg);

                    var next = p.Bounces > 0
                        ? Targeting.NearestFrom(w.Enemies, t.X, t.Y, w.Stats.BounceRange, p.Hit!)
                        : null;
                    if (next is null)
                    {
                        p.Alive = false;
                        continue;
                    }

                    p.Bounces--;
                    p.Hops++;
                    p.Hit!.Add(next.Id);
                    p.Target = next;
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
                p.Alive = false;
                DamageCell(w, p.Dmg);
                w.PushFx(FxKind.Hit, p.X, p.Y);
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
            var e = w.Enemies[i];
            if (!e.Alive) continue;

            if (e.Flash > 0) e.Flash -= dt;
            e.AttackCd -= dt;

            var d = Math.Sqrt(e.X * e.X + e.Y * e.Y);
            if (d == 0) d = 1;
            var ranged = e.Def.Ranged;
            var stopDist = ranged is not null
                ? w.Stats.Range * ranged.RangeFrac
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
                if (ranged is not null) e.AttackCd = ranged.Windup;
            }

            if (e.AttackCd > 0) continue;

            e.AttackCd = e.Def.AttackInterval;
            if (ranged is not null) Shoot(w, e); else HitCell(w, e);
            if (w.Dead) return;
        }
    }

    private static void HitCell(World w, Enemy e)
    {
        DamageCell(w, e.Atk * e.DmgMult);
        w.PushFx(FxKind.Hit, e.X, e.Y);
        e.DmgMult += CombatBalance.HeatupPerHit;
    }

    private static void Shoot(World w, Enemy e)
    {
        var d = Math.Sqrt(e.X * e.X + e.Y * e.Y);
        if (d == 0) d = 1;
        var spd = e.Def.Ranged?.ProjectileSpeed ?? EnemyBalance.ProjectileFallbackSpeed;
        w.Projectiles.Add(new Projectile
        {
            Id = w.NextId++,
            X = e.X,
            Y = e.Y,
            Vx = -e.X / d * spd,
            Vy = -e.Y / d * spd,
            Speed = spd,
            Dmg = e.Atk * e.DmgMult,
            Life = EnemyBalance.ProjectileLifetime,
        });
        e.DmgMult += CombatBalance.HeatupPerHit;
    }
}
