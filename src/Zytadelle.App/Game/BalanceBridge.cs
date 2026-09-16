using Zytadelle.Core.Snapshot;

namespace Zytadelle.App.Game;

/// <summary>
/// Everything the renderer needs to know that is decided in C#, handed over once at boot.
///
/// The painters used to keep their own copies of the dish radius, the pathogen radii, the frame
/// layout and a handful of timings. Two copies of a number drift, and the only thing that noticed
/// was a console warning covering half of them. Now there is one copy: this is read from
/// <c>Zytadelle.Balancing</c> and <see cref="FrameEncoder"/>, passed to <c>arena.js#boot</c>, and
/// spread into <c>render/constants.js</c> before the first frame is drawn.
///
/// Adding a number here means adding it to <c>applyBalance</c> on the JS side; nothing else.
/// </summary>
public static class BalanceBridge
{
    /// <summary>The whole set, shaped for the decoder. Property names are the JS field names.</summary>
    public static object Payload() => new
    {
        arenaRadius = ArenaBalance.ArenaRadius,
        cellRadius = CellBalance.Radius,
        enemyRadii = Enum.GetValues<EnemyKind>().Select(k => EnemyBalance.Def(k).Radius).ToArray(),

        // The packed frame layout. The decoder walks the buffer with exactly these strides.
        frame = new
        {
            header = FrameEncoder.HeaderSize,
            enemy = FrameEncoder.EnemySize,
            projectile = FrameEncoder.ProjectileSize,
            fx = FrameEncoder.FxSize,
        },

        // Timings the painters fade against. fxLifetime is how long an effect stays in the ring;
        // every per-effect fade is shorter than it.
        fxLifetime = SimulationBalance.FxLifetime,
        enemyFlash = RenderBalance.EnemyFlash,
        fade = new
        {
            crit = RenderBalance.CritFxFade,
            hit = RenderBalance.HitFxFade,
            atp = RenderBalance.AtpFxFade,
            bossFlash = RenderBalance.BossFlashFade,
        },

        lowIntegrityFrac = RenderBalance.LowIntegrityFrac,
        hpBarHideAbove = RenderBalance.HpBarHideAbove,
        cameraPad = RenderBalance.CameraPad,

        // The damage multiplier at which a pathogen starts glowing, derived from the heat-up rule
        // rather than written down again.
        heatGlowFrom = CombatBalance.HeatupBase + CombatBalance.HeatupPerHit * RenderBalance.HeatGlowAfterHits,

        // What the view shows in the one frame before the first snapshot lands.
        seed = new
        {
            range = GeneRegistry.ValueAt(GeneId.Range, 0),
            attackSpeed = CellBalance.EffectiveAttackSpeed(GeneRegistry.ValueAt(GeneId.AttackSpeed, 0)),
            attackInterval = EnemyBalance.Def(EnemyKind.Basic).AttackInterval,
        },
    };
}
