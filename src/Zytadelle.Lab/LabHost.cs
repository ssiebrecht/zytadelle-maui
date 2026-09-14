using System.Text.Json;
using Zytadelle.Balancing;
using Zytadelle.Core.Persistence;
using Zytadelle.Core.Upgrades;
using Zytadelle.Lab.Engine;
using Zytadelle.Lab.Engine.Search;
using Zytadelle.Lab.Engine.Sim;
using Zytadelle.Lab.Engine.Tuning;

namespace Zytadelle.Lab;

/// <summary>
/// The whole state of the Lab, in one object the components read and call into.
///
/// Same shape as the game's <c>GameHost</c>: a singleton with a plain <see cref="UiChanged"/> event
/// rather than change notification on every property. A search reports through a counter that a
/// timer here samples a few times a second - marshalling per campaign would cost more than the
/// campaigns do.
/// </summary>
public sealed class LabHost(ISaveStorage storage)
{
    private CancellationTokenSource? _cancel;
    private Task? _pump;

    public event Action? UiChanged;

    // ---------------------------------------------------------------- tuning

    public TuningSet Tuning { get; private set; } = TuningSet.Shipped();

    public IReadOnlyList<TuningField> Fields => TuningSchema.Fields;

    public HashSet<string> ChangedPaths { get; private set; } = [];

    public string? TuningNote { get; private set; }

    public void SetTuning(string path, double value)
    {
        if (Busy) return;
        Tuning = Tuning.With(path, value);
        RefreshChanged();
        Announce();
    }

    public void ResetTuning(string? group = null)
    {
        if (Busy) return;
        var shipped = TuningSet.Shipped();
        var next = Tuning;
        foreach (var f in Fields)
            if (group is null || f.Group == group)
                next = next.With(f.Path, shipped[f.Path]);
        Tuning = next;
        RefreshChanged();
        TuningNote = group is null ? "Back to the shipped balance." : $"{group} reset.";
        Announce();
    }

    public string TuningJson() => Tuning.ToJson();

    public void LoadTuningJson(string json)
    {
        if (Busy) return;
        var before = Tuning;
        Tuning = TuningSet.FromJson(json);
        RefreshChanged();
        TuningNote = ReferenceEquals(Tuning, before) ? "Nothing loaded." : $"{ChangedPaths.Count} value(s) differ from the shipped set.";
        Announce();
    }

    public IReadOnlyList<TuningChange> Diff() => TuningDiff.Of(Tuning);

    public string ExportCSharp() => TuningDiff.ToCSharp(Tuning);

    private void RefreshChanged() => ChangedPaths = [.. Tuning.Changed()];

    public int ChangedIn(string group) => ChangedPaths.Count(p => p.StartsWith(group + '.', StringComparison.Ordinal));

    // ---------------------------------------------------------------- search settings

    public int Infection { get; set; } = 1;

    public int TargetCycle { get; set; } = 50;

    public double BudgetHours { get; set; } = 24;

    public int RunCap { get; set; } = 150;

    public double BuyInterval { get; set; } = 0.5;

    public SearchMode Mode { get; set; } = SearchMode.Priority;

    public int Generations { get; set; } = 8;

    public int Population { get; set; } = 40;

    public int MaxSeeds { get; set; } = 8;

    public int FinalSeeds { get; set; } = 24;

    public int TopK { get; set; } = 5;

    public uint MasterSeed { get; set; } = 1;

    public bool SeparateRunWeights { get; set; }

    public int Workers { get; set; } = Evaluator.DefaultWorkers;

    /// <summary>
    /// Game speed the real-time columns are quoted at. Only the speeds the game offers.
    ///
    /// This one and <see cref="ChartFrom"/> are the two settings that change what an <em>already
    /// finished</em> search looks like, so both have to announce. Every other setting here only
    /// matters at the next start, and the form that owns it re-renders itself.
    /// </summary>
    public int Speed
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            Announce();
        }
    } = 20;

    /// <summary>First cycle the per-cycle charts plot. Re-draws what is already on screen.</summary>
    public int ChartFrom
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            Announce();
        }
    } = 1;

    public HashSet<UpgradeId> Genes { get; } = [.. UpgradeIds.All];

    public string GridLevelText { get; private set; } = "0, 1, 3, 9";

    public void SetGridLevels(string? text)
    {
        if (Busy) return;
        GridLevelText = string.IsNullOrWhiteSpace(text) ? GridLevelText : text;
        // The measured rate is only valid for the settings it was measured at, and the grid is priced
        // off it - so a change here drops the clock rather than showing a stale one.
        CampaignsPerSecond = 0;
        Plan = GridPlan.Empty;
        Announce();
    }

    public bool GridDedup { get; set; } = true;

    /// <summary>What an unsearched gene is worth. Zero is what makes the grid dedup sound.</summary>
    public double PinnedWeight { get; set; } = 1;

    public void ToggleGene(UpgradeId id)
    {
        if (Busy) return;
        if (!Genes.Remove(id)) Genes.Add(id);
        Announce();
    }

    public void SetAllGenes(bool on)
    {
        if (Busy) return;
        Genes.Clear();
        if (on) foreach (var id in UpgradeIds.All) Genes.Add(id);
        Announce();
    }

    public IReadOnlyList<double> GridLevels()
    {
        var parsed = GridLevelText
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => double.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : double.NaN)
            .Where(v => double.IsFinite(v) && v >= 0)
            .Distinct()
            .OrderBy(v => v)
            .ToArray();
        return parsed.Length > 1 ? parsed : [0, 1, 3, 9];
    }

    public SearchConfig Config() => new(
        new CampaignConfig(
            Infection,
            Math.Max(2, TargetCycle),
            Math.Max(0.05, BuyInterval),
            false,
            Math.Max(600, BudgetHours * 3600),
            Math.Max(1, RunCap)),
        Mode,
        Math.Max(1, MasterSeed),
        Math.Max(1, Generations),
        Math.Max(6, Population),
        Math.Max(1, MaxSeeds),
        SearchConfig.Default().SeedRotation,
        SearchConfig.Default().EliteFraction,
        SearchConfig.Default().Sigma0,
        SearchConfig.Default().SigmaMin,
        Genes.Count > 0 ? [.. UpgradeIds.All.Where(Genes.Contains)] : UpgradeIds.All,
        SeparateRunWeights,
        PinnedWeight,
        GridLevels(),
        [5],
        GridDedup,
        Math.Max(2, FinalSeeds),
        Math.Max(1, TopK),
        Math.Max(1, Workers),
        Speed);

    // ---------------------------------------------------------------- running

    public bool Busy { get; private set; }

    public string Status { get; private set; } = "Idle.";

    public SearchProgress Progress { get; private set; } = new();

    public IReadOnlyList<Finalist> Results { get; private set; } = [];

    public int Selected { get; private set; }

    /// <summary>Measured, never assumed: the estimate is only worth showing if it came from a clock.</summary>
    public double CampaignsPerSecond { get; private set; }

    public GridPlan Plan { get; private set; } = GridPlan.Empty;

    public void Select(int index)
    {
        Selected = index;
        Announce();
    }

    /// <summary>
    /// Times a handful of campaigns at the current settings, then prices the grid from that. Cheap
    /// enough to run whenever the form changes something that would move it.
    /// </summary>
    public async Task Estimate()
    {
        if (Busy) return;
        Busy = true;
        Status = "Timing a few campaigns...";
        Progress = new SearchProgress(SearchPhase.Calibrating);
        Announce();

        var cfg = Config();
        try
        {
            CampaignsPerSecond = await Task.Run(() =>
            {
                LabRuntime.Prepare(Tuning);
                return Evaluator.Calibrate(cfg.Campaign, 8, CancellationToken.None);
            });
            Plan = GridSearch.Plan(cfg, CampaignsPerSecond);
            Status = $"{CampaignsPerSecond:F1} campaigns/s per core, {CampaignsPerSecond * cfg.WorkerCount:F0}/s on {cfg.WorkerCount} workers.";
        }
        catch (Exception e)
        {
            Status = $"Estimate failed: {e.Message}";
        }
        finally
        {
            Busy = false;
            Progress = new SearchProgress();
            Announce();
        }
    }

    public async Task Start()
    {
        if (Busy) return;
        Persist();

        var cfg = Config();
        var tuning = Tuning;
        _cancel = new CancellationTokenSource();
        var ct = _cancel.Token;

        Busy = true;
        Results = [];
        Selected = 0;
        Status = "Applying the tuning set...";
        Announce();

        SearchRun? run = null;
        _pump = PumpProgress(() => run, ct);

        try
        {
            var finalists = await Task.Run(() =>
            {
                // One tuning set per search, applied before any worker exists and never touched
                // again while they run. The balance lives in static state, so this is the invariant
                // the whole parallel search rests on.
                LabRuntime.Prepare(tuning);
                var pool = new Evaluator(cfg.WorkerCount);
                run = cfg.Mode == SearchMode.Exhaustive
                    ? new GridSearch(cfg, pool)
                    : new PrioritySearch(cfg, pool);
                return run.Run(ct);
            }, ct);

            Results = finalists;
            Status = finalists.Count == 0
                ? "Nothing finished. Raise the campaign budget or lower the target cycle."
                : $"Done. {Progress.Campaigns:N0} campaigns simulated.";
        }
        catch (OperationCanceledException)
        {
            Status = "Stopped.";
        }
        catch (Exception e)
        {
            Status = $"Failed: {e.Message}";
        }
        finally
        {
            Busy = false;
            _cancel?.Dispose();
            _cancel = null;
            Announce();
        }
    }

    public void Cancel()
    {
        if (!Busy) return;
        Status = "Stopping...";
        _cancel?.Cancel();
        Announce();
    }

    /// <summary>
    /// Samples the search five times a second while it runs. The search writes a counter and a
    /// snapshot; nothing crosses to the UI thread per campaign.
    /// </summary>
    private async Task PumpProgress(Func<SearchRun?> current, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
        try
        {
            while (Busy && await timer.WaitForNextTickAsync(ct))
            {
                if (current() is not { } run) continue;
                Progress = run.Progress;
                Status = Describe(Progress, Speed);
                Announce();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// Every hour this tool prints is simulated unless it says otherwise, and a speed selector
    /// sitting right above an unlabelled "h" is an invitation to read it as real time. So the one
    /// number that appears while a search runs carries both clocks.
    /// </summary>
    private static string Describe(SearchProgress p, int speed)
    {
        var phase = p.Phase switch
        {
            SearchPhase.Searching => $"Round {p.Generation + 1}/{Math.Max(1, p.Generations)}",
            SearchPhase.Finals => "Re-scoring the finalists on held-out seeds",
            SearchPhase.Charts => "Collecting curves",
            SearchPhase.Done => "Done",
            _ => "Working",
        };

        string best;
        if (!double.IsFinite(p.BestSeconds))
        {
            best = $", deepest cycle {p.BestCycle}";
        }
        else
        {
            var sim = p.BestSeconds / 3600;
            var real = sim / Math.Max(1, speed);
            best = speed > 1
                ? $", fastest campaign {sim:F2} h sim ({real:F2} h at x{speed})"
                : $", fastest campaign {sim:F2} h sim";
        }

        return $"{phase} - {p.Done}/{p.Total}, {p.Campaigns:N0} campaigns simulated{best}.";
    }

    public double Fraction => Progress.Total > 0 ? Math.Clamp(Progress.Done / (double)Progress.Total, 0, 1) : 0;

    private void Announce() => UiChanged?.Invoke();

    // ---------------------------------------------------------------- persistence

    private sealed record Stored(
        Dictionary<string, double> Tuning,
        int Infection, int TargetCycle, double BudgetHours, int RunCap, double BuyInterval,
        SearchMode Mode, int Generations, int Population, int MaxSeeds, int FinalSeeds, int TopK,
        uint MasterSeed, bool SeparateRunWeights, int Workers, int Speed, int ChartFrom,
        string[] Genes, string GridLevelText, bool GridDedup, double PinnedWeight);

    public void Initialise()
    {
        try
        {
            if (storage.Read() is not { Length: > 0 } json) return;
            if (JsonSerializer.Deserialize<Stored>(json) is not { } s) return;

            Tuning = TuningSet.FromJson(JsonSerializer.Serialize(s.Tuning));
            RefreshChanged();
            Infection = s.Infection;
            TargetCycle = s.TargetCycle;
            BudgetHours = s.BudgetHours;
            RunCap = s.RunCap;
            BuyInterval = s.BuyInterval;
            Mode = s.Mode;
            Generations = s.Generations;
            Population = s.Population;
            MaxSeeds = s.MaxSeeds;
            FinalSeeds = s.FinalSeeds;
            TopK = s.TopK;
            MasterSeed = s.MasterSeed;
            SeparateRunWeights = s.SeparateRunWeights;
            Workers = s.Workers > 0 ? s.Workers : Evaluator.DefaultWorkers;
            Speed = SimulationBalance.Speeds.Contains(s.Speed) ? s.Speed : 20;
            ChartFrom = s.ChartFrom;
            GridLevelText = s.GridLevelText;
            GridDedup = s.GridDedup;
            PinnedWeight = s.PinnedWeight;

            if (s.Genes.Length > 0)
            {
                Genes.Clear();
                foreach (var name in s.Genes)
                    if (Enum.TryParse<UpgradeId>(name, out var id))
                        Genes.Add(id);
            }
        }
        catch (JsonException)
        {
            // A settings file that will not parse is not worth a crash; the defaults are fine.
        }
    }

    public void Persist()
    {
        try
        {
            var body = new Stored(
                Tuning.Values.ToDictionary(kv => kv.Key, kv => kv.Value),
                Infection, TargetCycle, BudgetHours, RunCap, BuyInterval,
                Mode, Generations, Population, MaxSeeds, FinalSeeds, TopK,
                MasterSeed, SeparateRunWeights, Workers, Speed, ChartFrom,
                [.. Genes.Select(g => g.ToString())], GridLevelText, GridDedup, PinnedWeight);
            storage.Write(JsonSerializer.Serialize(body));
        }
        catch (JsonException)
        {
        }
    }
}
