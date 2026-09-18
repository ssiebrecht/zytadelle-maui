namespace Zytadelle.Core.Tests.Determinism;

/// <summary>
/// World.RecordFx and World.TrackIncome let a headless run skip render-only bookkeeping (the Fx
/// ring, the income rate windows) that nothing in the simulation reads back. WorldProbe's own doc
/// comment already claims neither is part of its hash - this proves that claim holds in practice,
/// checkpoint by checkpoint, not just by inspection of which fields the probe happens to write.
/// </summary>
public class RenderFlagsTests
{
    public static IEnumerable<object[]> Scenarios()
    {
        foreach (var build in Enum.GetValues<Build>())
            yield return [new Scenario(build, 1, 1u, build == Build.Stress ? 12 : 40)];
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void RecordFxOff_And_TrackIncomeOff_MatchesRenderingRun(Scenario scenario)
    {
        var rendering = ScenarioRunner.Run(scenario);
        var headless = ScenarioRunner.Run(scenario, recordFx: false, trackIncome: false);

        Assert.Equal(rendering.Ticks, headless.Ticks);
        Assert.Equal(rendering.Died, headless.Died);
        Assert.Equal(rendering.FinalHash, headless.FinalHash);
        Assert.Equal(rendering.Summary, headless.Summary);

        Assert.Equal(rendering.Checkpoints.Count, headless.Checkpoints.Count);
        for (var i = 0; i < rendering.Checkpoints.Count; i++)
            Assert.Equal(rendering.Checkpoints[i], headless.Checkpoints[i]);
    }
}
