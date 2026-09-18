namespace Zytadelle.Core.Tests.Determinism;

/// <summary>
/// Bare gene set, only the free genes unlocked. On tiers 2-4 it breaches within cycle 1, so this
/// class is also the one that exercises the death-mid-tick contract on every run but T1.
/// </summary>
public sealed class GoldenTestsEmpty
{
    private static readonly Scenario[] Cases = ScenarioMatrix.Standard(Build.Empty).ToArray();

    [Fact] public void T1_S1() => GoldenFile.Verify(Cases[0]);
    [Fact] public void T1_S2() => GoldenFile.Verify(Cases[1]);
    [Fact] public void T1_S3() => GoldenFile.Verify(Cases[2]);
    [Fact] public void T2() => GoldenFile.Verify(Cases[3]);
    [Fact] public void T3() => GoldenFile.Verify(Cases[4]);
    [Fact] public void T4() => GoldenFile.Verify(Cases[5]);
}
