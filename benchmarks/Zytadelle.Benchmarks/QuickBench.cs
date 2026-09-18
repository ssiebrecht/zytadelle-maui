using System.Diagnostics;

namespace Zytadelle.Benchmarks;

/// <summary>
/// A non-BenchmarkDotNet fast path: one warmup plus five timed runs of a fixed tick count, done in
/// a few seconds instead of BDN's usual minutes. Good enough to tell "did that change help or hurt"
/// while iterating on one step of the refactor; the real numbers for a commit still come from BDN.
/// </summary>
public static class QuickBench
{
    private const int TicksPerRun = 60_000; // 1000 simulated seconds at the fixed 60 Hz step.

    public static void Run(string[] args)
    {
        var build = ParseBuild(args.Length > 0 ? args[0] : "stress");
        Console.WriteLine($"--quick {build}: 1 warmup + 5 runs, {TicksPerRun:N0} ticks each");

        OneRun(build); // warmup: JIT tiers up, any lazily-built caches (UpgradeValueCurve) fill in.

        for (var i = 0; i < 5; i++)
        {
            var r = OneRun(build);
            var nsPerTick = r.ElapsedMs * 1_000_000.0 / TicksPerRun;
            var mticksPerSec = TicksPerRun / r.ElapsedMs / 1000.0;
            var bytesPerTick = r.Bytes / (double)TicksPerRun;
            Console.WriteLine(
                $"  run {i + 1}: {r.ElapsedMs,7:F1} ms  {nsPerTick,8:F1} ns/tick  {mticksPerSec,6:F2} Mticks/s  " +
                $"{bytesPerTick,8:F1} B/tick  gen0={r.Gen0} gen1={r.Gen1} gen2={r.Gen2}");
        }
    }

    /// <summary>Runs one build until killed, so an external profiler has a stable, long-lived target to attach to.</summary>
    public static void Loop(string[] args)
    {
        var build = ParseBuild(args.Length > 0 ? args[0] : "stress");
        Console.WriteLine($"--loop {build}: running until stopped - attach dotnet-trace / dotnet-counters / PerfView now");

        var world = MakeWorld(build);
        var policy = build == Build.Stress ? null : new CheapestAffordablePolicy(30);
        var tick = 0L;
        while (true)
        {
            policy?.Apply(world);
            Step.Run(world, SimulationBalance.FixedDt);
            if (++tick % 10_000_000 == 0) Console.WriteLine($"  {tick:N0} ticks...");
        }
    }

    private sealed record RunStats(double ElapsedMs, long Bytes, int Gen0, int Gen1, int Gen2);

    private static RunStats OneRun(Build build)
    {
        var world = MakeWorld(build);
        var policy = build == Build.Stress ? null : new CheapestAffordablePolicy(30);

        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);
        var bytesBefore = GC.GetTotalAllocatedBytes(precise: true);
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < TicksPerRun; i++)
        {
            policy?.Apply(world);
            Step.Run(world, SimulationBalance.FixedDt);
        }

        sw.Stop();
        var bytesAfter = GC.GetTotalAllocatedBytes(precise: true);
        return new RunStats(
            sw.Elapsed.TotalMilliseconds, bytesAfter - bytesBefore,
            GC.CollectionCount(0) - gen0, GC.CollectionCount(1) - gen1, GC.CollectionCount(2) - gen2);
    }

    /// <summary>Tier 1, a fixed seed, no cycle cap - <c>--quick</c>/<c>--loop</c> just want a steady-state feed.</summary>
    private static World MakeWorld(Build build) => new Scenario(build, 1, 1u, int.MaxValue).CreateWorld();

    private static Build ParseBuild(string s) =>
        Enum.TryParse<Build>(s, ignoreCase: true, out var b)
            ? b
            : throw new ArgumentException($"Unknown build '{s}'. Expected one of: {string.Join(", ", Enum.GetNames<Build>())}.");
}
