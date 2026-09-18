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
    /// Puts one pathogen on the spawn circle. Above the cap nothing happens and no random number is
    /// drawn, so a full field does not desynchronise the sequence.
    /// </summary>
    public static Enemy? Spawn(World w, EnemyKind kind)
    {
        if (w.Enemies.Count >= SimulationBalance.MaxEnemies) return null;
        var def = EnemyBalance.Def(kind);
        var ang = w.Rng.Next() * Math.PI * 2;
        var hp = def.Hp * w.HpAtC * w.Infection.HpMult;
        var e = new Enemy
        {
            Id = w.NextId++,
            Kind = kind,
            Def = def,
            X = Math.Cos(ang) * ArenaBalance.ArenaRadius,
            Y = Math.Sin(ang) * ArenaBalance.ArenaRadius,
            Hp = hp,
            MaxHp = hp,
            Atk = def.Atk * w.AtkAtC * w.Infection.AtkMult,
            Speed = def.Speed * EnemyBalance.SpeedMult
                    * w.Rng.Range(EnemyBalance.SpeedJitterMin, EnemyBalance.SpeedJitterMax),
            Radius = def.Radius,
            SpawnCycle = w.Cycle,
        };
        w.Enemies.Add(e);
        return e;
    }
}
