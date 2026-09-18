using System.Diagnostics;

using Zytadelle.Core.Entities;

namespace Zytadelle.Core.Sim;

public static class Spawning
{
    /// <summary>
    /// Rolls a pathogen type off the current cycle's mix. Walks
    /// <see cref="SpawnBalance.SpawnKinds"/> in its declared order - that order decides which type a
    /// given draw lands on, so it is not interchangeable with the order the shares are declared in.
    /// </summary>
    public static EnemyKind PickKind(World w)
    {
        var r = w.Rng.Next() * w.WeightsSumC;
        foreach (var k in SpawnBalance.SpawnKinds)
        {
            r -= w.WeightsC.Of(k);
            if (r <= 0) return k;
        }
        return EnemyKind.Basic;
    }

    /// <summary>
    /// Puts one pathogen on the spawn circle and returns its index in World.Enemies, or -1 above
    /// the cap - where nothing happens and no random number is drawn, so a full field does not
    /// desynchronise the sequence.
    /// </summary>
    public static int Spawn(World w, EnemyKind kind)
    {
        if (w.Enemies.Count >= SimulationBalance.MaxEnemies) return -1;
        var def = EnemyBalance.Def(kind);
        var ang = w.Rng.Next() * Math.PI * 2;
        var hp = def.Hp * w.HpAtC * w.Infection.HpMult;

        var index = w.Enemies.Count;
        ref var e = ref w.Enemies.AddRef();
        e.Id = w.NextId++;
        e.Kind = kind;
        e.X = Math.Cos(ang) * ArenaBalance.ArenaRadius;
        e.Y = Math.Sin(ang) * ArenaBalance.ArenaRadius;
        e.Hp = hp;
        e.MaxHp = hp;
        e.Atk = def.Atk * w.AtkAtC * w.Infection.AtkMult;
        e.Speed = def.Speed * EnemyBalance.SpeedMult
                  * w.Rng.Range(EnemyBalance.SpeedJitterMin, EnemyBalance.SpeedJitterMax);
        e.Radius = def.Radius;
        e.SpawnCycle = w.Cycle;
        e.AttackInterval = def.AttackInterval;
        e.Dna = def.Dna;
        e.IsRanged = def.Ranged is not null;
        if (def.Ranged is { } ranged)
        {
            e.RangeFrac = ranged.RangeFrac;
            e.Windup = ranged.Windup;
            e.ProjectileSpeed = ranged.ProjectileSpeed;
        }

        // Struct defaults are 0/false, not the class field initializers they replace - both must
        // be set explicitly on every spawn, never left to a default.
        e.DmgMult = CombatBalance.HeatupBase;
        e.Alive = true;
        Debug.Assert(e.Alive && e.DmgMult == CombatBalance.HeatupBase, "Enemy spawned without its required explicit fields.");

        return index;
    }
}
