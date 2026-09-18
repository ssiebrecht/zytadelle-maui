namespace Zytadelle.Core.Tests.Determinism;

/// <summary>
/// The future simulator's whole premise is many <see cref="World"/> instances stepping in parallel
/// on separate threads, reading the same <c>*Balance</c> statics. This runs the identical scenario
/// on eight threads at once and checks every thread produced the exact same trajectory hash - not
/// just the same final state, since <see cref="WorldProbe"/> is folded in every tick, so any
/// per-tick divergence anywhere would move it. Three passes catch anything that only shows up once
/// the JIT and any lazily-built caches (<c>UpgradeValueCurve</c>'s prefix sums) are warm.
/// </summary>
public sealed class ThreadSafetyTests
{
    [Fact]
    public void SameScenario_OnEightThreads_ProducesIdenticalHashes()
    {
        var scenario = new Scenario(Build.BounceMulti, 3, 0x9E3779B9u, 8);

        for (var pass = 0; pass < 3; pass++)
        {
            var hashes = new ulong[8];
            Parallel.For(0, hashes.Length, i => hashes[i] = ScenarioRunner.Run(scenario).FinalHash);
            for (var i = 1; i < hashes.Length; i++)
                Assert.True(hashes[0] == hashes[i],
                    $"pass {pass}: thread {i} hash {hashes[i]:x16} differs from thread 0's {hashes[0]:x16}.");
        }
    }
}
