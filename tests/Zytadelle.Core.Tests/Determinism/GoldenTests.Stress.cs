namespace Zytadelle.Core.Tests.Determinism;

/// <summary>
/// All barrier, no offense, no purchase policy: fills the enemy cap and stays there. The worst case
/// for entity-list size, and - via the Long trait - a 120-cycle (~252k tick) canary long enough for
/// a per-tick regression to show up as a real timing difference, not noise.
/// </summary>
public sealed class GoldenTestsStress
{
    private static readonly Scenario[] Cases = ScenarioMatrix.Standard(Build.Stress).ToArray();

    [Fact] public void T1_S1() => GoldenFile.Verify(Cases[0]);
    [Fact] public void T1_S2() => GoldenFile.Verify(Cases[1]);
    [Fact] public void T1_S3() => GoldenFile.Verify(Cases[2]);
    [Fact] public void T2() => GoldenFile.Verify(Cases[3]);
    [Fact] public void T3() => GoldenFile.Verify(Cases[4]);
    [Fact] public void T4() => GoldenFile.Verify(Cases[5]);

    [Fact]
    [Trait("Category", "Long")]
    public void Long_120Cycles() => GoldenFile.Verify(ScenarioMatrix.Long().Single(s => s.Build == Build.Stress));
}
