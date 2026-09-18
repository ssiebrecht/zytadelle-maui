namespace Zytadelle.Core.Tests.Determinism;

public sealed record Checkpoint(int Cycle, int Tick, ulong Hash);

public sealed record ScenarioResult(
    int ProbeVersion,
    Scenario Scenario,
    int Ticks,
    bool Died,
    ulong FinalHash,
    ulong RenderHash,
    IReadOnlyList<Checkpoint> Checkpoints,
    RunSummary Summary);

/// <summary>
/// Drives one <see cref="Scenario"/> to its cycle cap or its death, folding <see cref="WorldProbe"/>
/// into a running hash every tick and <see cref="RenderProbe"/> into a second one at every
/// checkpoint. A checkpoint lands on every cycle change and additionally every
/// <see cref="CheckpointStride"/> ticks, so a divergence is never more than a few simulated seconds
/// from the nearest recorded point in either direction - close enough to bisect by eye, cheap enough
/// to commit for a 250k-tick long-trait run.
/// </summary>
public static class ScenarioRunner
{
    public const int CheckpointStride = 300;

    /// <summary>Generous multiple of the longest scenario in the matrix - a real runaway trips this, not the cap.</summary>
    private const int MaxTicksGuard = 5_000_000;

    /// <summary><paramref name="recordFx"/>/<paramref name="trackIncome"/> exist for
    /// <c>RenderFlagsTests</c>: a headless run flips them off, and the resulting hash must still
    /// match a rendering run bit for bit, since neither is read by <see cref="WorldProbe"/>.</summary>
    public static ScenarioResult Run(Scenario scenario, bool recordFx = true, bool trackIncome = true)
    {
        var w = scenario.CreateWorld();
        w.RecordFx = recordFx;
        w.TrackIncome = trackIncome;
        var policy = scenario.PolicyEveryTicks is { } every ? new CheapestAffordablePolicy(every) : null;

        var worldHash = new StateHasher();
        var renderHash = new StateHasher();
        var buf = new WordWriter();
        var checkpoints = new List<Checkpoint>();

        var tick = 0;
        while (!w.Dead && w.Cycle <= scenario.MaxCycles)
        {
            if (tick > MaxTicksGuard)
                throw new InvalidOperationException(
                    $"{scenario.Name}: exceeded {MaxTicksGuard} ticks without reaching its cap or dying - runaway loop?");

            var cycleBefore = w.Cycle;
            policy?.Apply(w);
            Step.Run(w, SimulationBalance.FixedDt);
            tick++;

            buf.Reset();
            WorldProbe.Write(w, buf);
            worldHash.Append(buf.Span);

            if (w.Cycle != cycleBefore || tick % CheckpointStride == 0 || w.Dead)
            {
                buf.Reset();
                RenderProbe.Write(w, buf);
                renderHash.Append(buf.Span);
                checkpoints.Add(new Checkpoint(w.Cycle, tick, worldHash.Checkpoint()));
            }
        }

        return new ScenarioResult(
            WorldProbe.Version, scenario, tick, w.Dead,
            worldHash.Checkpoint(), renderHash.Checkpoint(), checkpoints, RunSummary.Of(w));
    }
}
