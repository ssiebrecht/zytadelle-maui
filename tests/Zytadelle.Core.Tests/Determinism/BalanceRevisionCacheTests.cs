namespace Zytadelle.Core.Tests.Determinism;

/// <summary>Its own non-parallel collection: the test below mutates the process-wide
/// <see cref="BalanceRevision.Current"/>, and keeping it away from anything else that might do the
/// same keeps cause and effect unambiguous, even though a concurrent bump from elsewhere could not
/// actually corrupt a result - see the test's own doc comment.</summary>
[CollectionDefinition(nameof(BalanceRevisionCollection), DisableParallelization = true)]
public class BalanceRevisionCollection;

/// <summary>
/// Cycles.EnsureCache refreshes the per-cycle cache whenever <see cref="BalanceRevision.Current"/>
/// moves, not only on a cycle change - the plan's own required extra check for this step. A bump
/// that writes no actual value back (a tuning pass that touched nothing, or just this test) must be
/// invisible to the simulation: re-evaluating the same statics for the same cycle can only ever
/// reproduce the same numbers, so a run that gets bumped mid-flight must still land on the exact
/// same hash as one that never does.
/// </summary>
[Collection(nameof(BalanceRevisionCollection))]
public class BalanceRevisionCacheTests
{
    [Fact]
    public void BumpMidRun_WithNoValueChange_DoesNotChangeTheResult()
    {
        var scenario = new Scenario(Build.BounceMulti, 2, 1u, 20);
        var baseline = ScenarioRunner.Run(scenario);

        var bumped = false;
        var withBump = ScenarioRunner.Run(scenario, onTick: (_, tick) =>
        {
            if (!bumped && tick > 50)
            {
                BalanceRevision.Bump();
                bumped = true;
            }
        });

        Assert.True(bumped, "scenario finished before the injected bump landed - lengthen it");
        Assert.Equal(baseline.Ticks, withBump.Ticks);
        Assert.Equal(baseline.Died, withBump.Died);
        Assert.Equal(baseline.FinalHash, withBump.FinalHash);
        Assert.Equal(baseline.Summary, withBump.Summary);

        Assert.Equal(baseline.Checkpoints.Count, withBump.Checkpoints.Count);
        for (var i = 0; i < baseline.Checkpoints.Count; i++)
            Assert.Equal(baseline.Checkpoints[i], withBump.Checkpoints[i]);
    }
}
