namespace Zytadelle.Core.Tests.Determinism;

/// <summary>
/// Crit-heavy single-target build with a purchase policy running. The Long trait pushes to cycle
/// 210, crossing <see cref="EconomyBalance.AtpKillSlowCycle"/> (200) so the ATP-per-kill slowdown
/// branch is actually exercised, not just the common path below it.
/// </summary>
public sealed class GoldenTestsMidOffense
{
    private static readonly Scenario[] Cases = ScenarioMatrix.Standard(Build.MidOffense).ToArray();

    [Fact] public void T1_S1() => GoldenFile.Verify(Cases[0]);
    [Fact] public void T1_S2() => GoldenFile.Verify(Cases[1]);
    [Fact] public void T1_S3() => GoldenFile.Verify(Cases[2]);
    [Fact] public void T2() => GoldenFile.Verify(Cases[3]);
    [Fact] public void T3() => GoldenFile.Verify(Cases[4]);
    [Fact] public void T4() => GoldenFile.Verify(Cases[5]);

    [Fact]
    [Trait("Category", "Long")]
    public void Long_Cycle210() => GoldenFile.Verify(ScenarioMatrix.Long().Single(s => s.Build == Build.MidOffense));
}
