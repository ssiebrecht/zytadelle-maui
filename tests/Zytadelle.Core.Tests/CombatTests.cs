using System.Buffers.Binary;

using Zytadelle.Core.Snapshot;

namespace Zytadelle.Core.Tests;

/// <summary>
/// Combat edge cases the goldens do not cover: LifeSteal is off in every golden build, so a
/// post-death heal on the same projectile pass would never move a hash.
/// </summary>
public sealed class CombatTests
{
    /// <summary>
    /// <see cref="Combat.UpdateProjectiles"/> walks the whole list even after a shot has already
    /// killed the cell. A later toxin must not life-steal integrity back onto a dead cell - the
    /// results screen draws this frame, and <see cref="FrameEncoder"/> packs <c>Cell.Hp</c> as-is.
    /// </summary>
    [Fact]
    public void UpdateProjectiles_LifeStealAfterKillingShot_LeavesDeadCellAtZeroHp()
    {
        var lab = new Levels();
        lab[GeneId.LifeSteal] = 50;
        var w = World.Create(1, lab, [GeneId.LifeSteal], seed: 1);
        Assert.True(w.Stats.LifeSteal > 0, "The lab level must actually turn Phagocytosis on.");

        w.Cell.Hp = 1;

        var enemyIdx = AddEnemy(w, x: 10, y: 0, hp: 100);
        ref var enemy = ref w.Enemies[enemyIdx];

        // Killing shot first, then a toxin that lands this tick - the order the bug needs.
        AddEnemyShot(w, dmg: 100);
        AddToxin(w, enemyIdx, enemy.Id, x: enemy.X, y: enemy.Y, dmg: 40);

        Combat.UpdateProjectiles(w, SimulationBalance.FixedDt);

        Assert.True(w.Dead);
        Assert.Equal(0, w.Cell.Hp);
        Assert.True(w.Enemies[enemyIdx].Hp < 100, "The later toxin must still land; only the heal is suppressed.");

        var frame = FrameEncoder.Encode(w);
        Assert.Equal(0f, BinaryPrimitives.ReadSingleLittleEndian(frame.AsSpan(8)));
    }

    private static int AddEnemy(World w, double x, double y, double hp)
    {
        var index = w.Enemies.Count;
        ref var e = ref w.Enemies.AddRef();
        e.Id = w.NextId++;
        e.Kind = EnemyKind.Basic;
        e.X = x;
        e.Y = y;
        e.Hp = hp;
        e.MaxHp = hp;
        e.Radius = 1;
        e.DmgMult = CombatBalance.HeatupBase;
        e.Alive = true;
        return index;
    }

    private static void AddEnemyShot(World w, double dmg)
    {
        ref var p = ref w.Projectiles.AddRef();
        p.Id = w.NextId++;
        p.Dmg = dmg;
        p.Life = 1;
        p.TargetIndex = -1;
        p.TargetId = -1;
        p.HitSlot = -1;
        p.ShooterIndex = -1;
        p.ShooterId = -1;
        p.Alive = true;
    }

    private static void AddToxin(World w, int targetIndex, int targetId, double x, double y, double dmg)
    {
        ref var p = ref w.Projectiles.AddRef();
        p.Id = w.NextId++;
        p.X = x;
        p.Y = y;
        p.Speed = CellBalance.ProjectileSpeed;
        p.Dmg = dmg;
        p.FromCell = true;
        p.TargetIndex = targetIndex;
        p.TargetId = targetId;
        p.HitSlot = -1;
        p.ShooterIndex = -1;
        p.ShooterId = -1;
        p.Life = 1;
        p.Alive = true;
    }
}
