using Zytadelle.Balancing;
using Zytadelle.Core.Sim;
using Zytadelle.Core.Upgrades;
using Zytadelle.Lab.Engine.Tuning;

namespace Zytadelle.Lab.Engine;

/// <summary>
/// Getting the process ready for a search.
///
/// The balance lives in static state, so a tuned value is tuned for everyone in the process. The
/// Lab therefore applies one set per search and never touches it again while workers are running.
/// That is safe here because the Lab is its own application: a number turned in this window can
/// never reach a game someone is playing.
///
/// Two things have to happen before the first worker starts, and both are about the caches the game
/// grows lazily. <see cref="Curve.SumTo"/> extends a list as levels are bought, which is a data race
/// the moment two campaigns run at once; filling those lists to their cap first makes every later
/// read a plain indexed read. And the type initialisers behind the catalogs would otherwise
/// serialise the whole pool behind a lock in the middle of a measurement.
/// </summary>
public static class LabRuntime
{
    /// <summary>
    /// Applies a tuning set, silences the render-only work a headless campaign has no use for, and
    /// warms every cache the simulation grows lazily. Call once, before starting any worker; never
    /// while one is running.
    /// </summary>
    public static void Prepare(TuningSet tuning)
    {
        tuning.Apply();
        GoHeadless();
        Warm();
    }

    /// <summary>
    /// Effects and the per-minute income windows are read by the frame encoder and by the HUD, and
    /// by nothing in the simulation. A campaign draws no frames, so both are pure overhead - and the
    /// effect buffer is the expensive kind, because every step walks the whole ring. Shrinking them
    /// to nothing changes no result; the determinism probe compares a campaign run both ways.
    /// </summary>
    private static void GoHeadless()
    {
        SimulationBalance.FxRingCap = 0;
        SimulationBalance.RateWindowSeconds = 1;
    }

    /// <summary>
    /// Grows every lazily built cache to its final size while a single thread still owns them.
    ///
    /// The running totals are bounded because <see cref="UpgradeBalance.ValueAt"/> clamps the level
    /// to the gene's cap, and nothing can hold more levels than that: both shops refuse to sell past
    /// it. Only the four accelerating genes ever allocate a list at all - the price tracks go
    /// through <see cref="Curve.ValueAt"/>, which caches nothing.
    /// </summary>
    private static void Warm()
    {
        foreach (var id in UpgradeIds.All)
        {
            var curves = UpgradeBalance.Of(id);
            _ = UpgradeBalance.ValueAt(id, curves.Cap);
            _ = curves.Dna.CostAt(0);
            _ = curves.Atp?.CostAt(0);
        }

        _ = Stats.From(new Levels(), new Levels());
        _ = EnemyBalance.Def(EnemyKind.Basic);
        _ = InfectionBalance.Def(1);
        _ = SpawnBalance.Weights(1);

        // Workers are started after this point, and starting a thread publishes everything written
        // before it. The rule that follows: never reuse a pool across a retune.
        Thread.MemoryBarrier();
    }
}
