namespace Zytadelle.Core.Tests.Determinism;

/// <summary>
/// Multishot plus diffusion together: the build that actually exercises the extra-toxin loop, the
/// bounce-chain "already hit" list, and their interaction with a running purchase policy.
/// </summary>
public sealed class GoldenTestsBounceMulti
{
    private static readonly Scenario[] Cases = ScenarioMatrix.Standard(Build.BounceMulti).ToArray();

    [Fact] public void T1_S1() => GoldenFile.Verify(Cases[0]);
    [Fact] public void T1_S2() => GoldenFile.Verify(Cases[1]);
    [Fact] public void T1_S3() => GoldenFile.Verify(Cases[2]);
    [Fact] public void T2() => GoldenFile.Verify(Cases[3]);
    [Fact] public void T3() => GoldenFile.Verify(Cases[4]);
    [Fact] public void T4() => GoldenFile.Verify(Cases[5]);
}
