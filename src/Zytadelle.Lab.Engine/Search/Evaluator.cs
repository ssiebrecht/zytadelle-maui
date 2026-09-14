using Zytadelle.Lab.Engine.Sim;

namespace Zytadelle.Lab.Engine.Search;

/// <summary>One campaign to run: which candidate it belongs to, and which world it meets.</summary>
public sealed record EvalJob(int Candidate, uint Seed, Strategy Strategy);

/// <summary>A campaign result stripped to what the search ranks on - no ladder, no telemetry.</summary>
public sealed record SlimResult(bool Reached, double TimeToTarget, int BestCycle, double TotalSeconds, int Runs, double Dna)
{
    public static SlimResult Of(CampaignResult r) =>
        new(r.Reached, r.TimeToTarget, r.BestCycle, r.TotalSeconds, r.Runs, r.Dna);

    /// <summary>What a candidate the pool never answered for scores. Parked behind everything real.</summary>
    public static readonly SlimResult Missing = new(false, double.PositiveInfinity, 0, 0, 0, 0);
}

/// <summary>
/// Runs campaigns across every core but one.
///
/// Results go into a pre-sized array at their own job index and are never accumulated in the order
/// they finish. That is the whole of what makes a parallel search reproduce a sequential one: every
/// later average, rank and refit then sums the same doubles in the same order, and floating-point
/// addition stops caring how the work was scheduled.
///
/// The threads are real threads, not the thread pool. A search runs for hours at full saturation,
/// and the pool is what the web view needs to answer interop calls - borrowing it would freeze the
/// window the search is reporting into. Below-normal priority keeps the form usable while it works.
/// </summary>
public sealed class Evaluator(int workers)
{
    private int _done;

    public int Workers { get; } = Math.Clamp(workers, 1, 256);

    /// <summary>Campaigns finished in the batch that is running. Read it from a timer, not per job.</summary>
    public int Done => Volatile.Read(ref _done);

    public int Total { get; private set; }

    public static int DefaultWorkers => Math.Max(1, Environment.ProcessorCount - 1);

    public SlimResult[] Evaluate(IReadOnlyList<EvalJob> jobs, CampaignConfig cfg, CancellationToken ct)
    {
        var results = new SlimResult[jobs.Count];
        Array.Fill(results, SlimResult.Missing);
        Total = jobs.Count;
        Volatile.Write(ref _done, 0);
        if (jobs.Count == 0) return results;

        var next = -1;
        var threads = new Thread[Math.Min(Workers, jobs.Count)];
        for (var i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                int k;
                while ((k = Interlocked.Increment(ref next)) < jobs.Count)
                {
                    if (ct.IsCancellationRequested) return;
                    try
                    {
                        results[k] = SlimResult.Of(Campaign.Run(jobs[k].Strategy, jobs[k].Seed, cfg, ct));
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    Interlocked.Increment(ref _done);
                }
            })
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = $"campaign {i}",
            };
            threads[i].Start();
        }

        foreach (var t in threads) t.Join();
        ct.ThrowIfCancellationRequested();
        return results;
    }

    /// <summary>Re-runs one build with telemetry, for the charts and the ladder table.</summary>
    public static CampaignResult Detail(Strategy strategy, uint seed, CampaignConfig cfg, CancellationToken ct) =>
        Campaign.Run(strategy, seed, cfg with { Collect = true }, ct);

    /// <summary>
    /// Campaigns per second on one core at this configuration, measured rather than guessed. The
    /// exhaustive mode turns this into the estimate it shows before it lets anyone start a run that
    /// would take days - a fixed constant would be wrong the moment the target cycle moved.
    /// </summary>
    public static double Calibrate(CampaignConfig cfg, int samples, CancellationToken ct)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        for (var i = 0; i < samples; i++)
            Campaign.Run(Strategy.Flat(), Seeds.Mix((uint)(i + 1)), cfg, ct);
        var seconds = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalSeconds;
        return seconds <= 0 ? double.PositiveInfinity : samples / seconds;
    }
}
