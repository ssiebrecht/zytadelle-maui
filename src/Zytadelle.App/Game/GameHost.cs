using Zytadelle.Balancing;
using Zytadelle.Core.Missions;
using Zytadelle.Core.Persistence;
using Zytadelle.Core.Sim;
using Zytadelle.Core.Snapshot;
using Zytadelle.Core.Upgrades;

namespace Zytadelle.App.Game;

public enum Mode
{
    Hub,
    Run,
    Results,
}

public enum HubView
{
    Cell,
    Genome,
    Missions,
    Records,
}

/// <summary>What the results sheet shows after a culture ends.</summary>
public sealed record RunResult(RunSummary Summary, int Infection, bool IsRecord, int PrevBest);

/// <summary>
/// Owns the save, the current culture and the frame clock.
///
/// The browser drives the clock: every animation frame it calls <see cref="Tick"/>, which advances
/// the simulation by whole fixed steps and hands back the packed snapshot. Speed multiplies how much
/// simulated time one frame is worth, never the frame rate itself.
///
/// UI updates are a separate, much slower stream: <see cref="UiChanged"/> fires at most ten times a
/// second plus once after anything the player did, so Blazor never re-renders per frame.
/// </summary>
public sealed class GameHost(ISaveStorage storage)
{
    private readonly ISaveStorage _storage = storage;

    private double? _lastNow;
    private double _accumulator;
    private double _lastUiPublish = double.NegativeInfinity;
    private double _clock;

    public SaveData Save { get; private set; } = SaveSerializer.Deserialize(storage.Read());

    public Mode Mode { get; private set; } = Mode.Hub;

    public HubView HubView { get; private set; } = HubView.Cell;

    public GeneTab LabTab { get; set; } = GeneTab.Attack;

    public GeneTab ShopTab { get; set; } = GeneTab.Attack;

    /// <summary>The running culture. Kept after death so the results sheet has an arena behind it.</summary>
    public World? World { get; private set; }

    /// <summary>
    /// The hub's idle dish: a culture that is drawn but never stepped, so Reach and the Gene Lab
    /// levels are visible before anything starts.
    /// </summary>
    public World Preview { get; private set; } = null!;

    public RunResult? Result { get; private set; }

    public int Infection => Mode == Mode.Hub ? Save.SelectedInfection : LastRunInfection;

    public int LastRunInfection { get; private set; } = 1;

    /// <summary>Zoom for the idle dish: frames the cell as the hero instead of the whole field.</summary>
    public double Zoom { get; private set; } = 1;

    public event Action? UiChanged;

    public void Initialise()
    {
        RollMissions();
        RebuildPreview();
#if DEBUG
        if (Environment.GetEnvironmentVariable("ZYTADELLE_STRESS") == "1") StartStressCulture();
        ApplyDebugView(Environment.GetEnvironmentVariable("ZYTADELLE_VIEW"));
#endif
    }

    /// <summary>
    /// Debug only: opens one hub section straight away, with enough DNA that the cards show both
    /// their affordable and their unaffordable state. Used to look at a screen without clicking to it.
    /// </summary>
    private void ApplyDebugView(string? view)
    {
        if (string.IsNullOrEmpty(view)) return;
        Save.Dna = 15000;
        Save.TotalDna = 15000;
        if (view.Equals("results", StringComparison.OrdinalIgnoreCase))
        {
            // A bare cell against Infection 2 breaches within a cycle, which is the fastest way to
            // look at the results sheet.
            Save.Speed = SimulationBalance.Speeds[^1];
            MissionRoll.Ensure(Save);
            StartRun(2);
            return;
        }

        HubView = view.ToLowerInvariant() switch
        {
            "genome" => HubView.Genome,
            "missions" => HubView.Missions,
            "records" => HubView.Records,
            _ => HubView.Cell,
        };
        MissionRoll.Ensure(Save);
        RebuildPreview();
    }

    /// <summary>
    /// Debug only: a culture strong enough to fill the field, at the highest speed. It exists to
    /// measure the frame budget under the load the game actually reaches, not at an empty dish.
    /// </summary>
    private void StartStressCulture()
    {
        // Deliberately all barrier and no offense: the cell has to survive without clearing the
        // field, which is the only way to actually reach the pathogen cap and measure the worst case.
        (UpgradeId Id, int Level)[] build =
        [
            (UpgradeId.Health, 400), (UpgradeId.Regen, 320), (UpgradeId.DefPct, 99), (UpgradeId.DefAbs, 700),
            (UpgradeId.Range, 40),
        ];
        foreach (var (id, level) in build) Save.Lab[id] = level;
        foreach (var def in UpgradeCatalog.All)
            if (def.UnlockDna > 0 && !Save.Unlocked.Contains(def.Id))
                Save.Unlocked.Add(def.Id);
        Save.Speed = SimulationBalance.Speeds[^1];
        StartRun(1);
    }

    // ---------------------------------------------------------------- the frame clock

    public byte[]? Tick(double nowMs)
    {
        var w = Mode == Mode.Hub ? Preview : World;
        if (w is null) return null;

        var elapsed = _lastNow is { } last
            ? Math.Min(SimulationBalance.MaxFrameDelta, Math.Max(0, (nowMs - last) / 1000))
            : 0;
        _lastNow = nowMs;
        _clock += elapsed;

        if (Mode == Mode.Hub)
        {
            // Nothing is simulated in the hub; only time moves, so the membrane and the reach ring live.
            w.Time += elapsed;
        }
        else if (!w.Dead)
        {
            _accumulator += elapsed * Save.Speed;
            var steps = 0;
            while (_accumulator >= SimulationBalance.FixedDt && steps < SimulationBalance.MaxStepsPerFrame)
            {
                Step.Run(w, SimulationBalance.FixedDt);
                _accumulator -= SimulationBalance.FixedDt;
                steps++;
                if (!w.Dead) continue;
                OnDeath();
                break;
            }
            // Saturated: drop the debt rather than queue a catch-up that would never be paid off.
            if (steps == SimulationBalance.MaxStepsPerFrame) _accumulator = 0;
        }

        PublishUiThrottled();
        return FrameEncoder.Encode(w);
    }

    private void PublishUiThrottled()
    {
        if (_clock - _lastUiPublish < 0.1) return;
        _lastUiPublish = _clock;
        UiChanged?.Invoke();
    }

    /// <summary>Announce a change the player caused, which must be visible before the next tick.</summary>
    public void Announce()
    {
        _lastUiPublish = _clock;
        UiChanged?.Invoke();
    }

    // ---------------------------------------------------------------- mode changes

    public void ShowHub(HubView view)
    {
        RollMissions();
        HubView = view;
        Mode = Mode.Hub;
        World = null;
        Result = null;
        RebuildPreview();
        Announce();
    }

    public void ShowHubView(HubView view)
    {
        HubView = view;
        Announce();
    }

    public void StartRun(int infection)
    {
        RollMissions();
        LastRunInfection = infection;
        World = World.Create(infection, Save.Lab, Save.UnlockedIds());
        Mode = Mode.Run;
        Result = null;
        Zoom = 1;
        _accumulator = 0;
        _lastNow = null;
        ShopTab = GeneTab.Attack;
        Announce();
    }

    public void Retry() => StartRun(LastRunInfection);

    private void OnDeath()
    {
        if (World is null) return;
        var summary = RunSummary.Of(World);
        var prevBest = Save.Best(LastRunInfection);

        Save.RecordRun(LastRunInfection, summary.Cycle, summary.Dna);
        // A culture can outlast midnight: settle the day first, then book the run into it.
        MissionRoll.Ensure(Save);
        MissionRoll.ApplyRun(Save, new RunStats(
            summary.Cycle, summary.Dna, summary.AtpEarned, summary.Kills,
            summary.Time, summary.Crits, summary.RunBuys, summary.KillsByKind));
        Persist();

        Result = new RunResult(summary, LastRunInfection, summary.Cycle > prevBest, prevBest);
        Mode = Mode.Results;
        Announce();
    }

    // ---------------------------------------------------------------- the hub

    private void RebuildPreview()
    {
        Preview = World.Create(Save.SelectedInfection, Save.Lab, Save.UnlockedIds());
        // Frame the cell as the hero: the reach ring lands at about 60 % of the short side.
        Zoom = Math.Min(3.6, Math.Max(1, ArenaBalance.ArenaRadius * 1.04 / (Preview.Stats.Range * 0.83)));
    }

    public void SelectInfection(int infection)
    {
        if (!InfectionBalance.IsUnlocked(infection, Save.BestCycle)) return;
        Save.SelectedInfection = infection;
        Persist();
        RebuildPreview();
        Announce();
    }

    public LabResult BuyLab(UpgradeId id)
    {
        var result = LabPurchase.Buy(Save, id);
        if (result is LabResult.Bought or LabResult.Unlocked)
        {
            Persist();
            RebuildPreview();
            Announce();
        }
        return result;
    }

    public BuyResult BuyRun(UpgradeId id)
    {
        if (World is null) return BuyResult.Locked;
        var result = Purchase.Buy(World, id);
        if (result == BuyResult.Bought) Announce();
        return result;
    }

    public double ClaimMission(int index)
    {
        var dna = MissionRoll.Claim(Save, index);
        if (dna <= 0) return 0;
        Persist();
        Announce();
        return dna;
    }

    public void SetSpeed(int speed)
    {
        if (!SimulationBalance.Speeds.Contains(speed)) return;
        Save.Speed = speed;
        Persist();
        Announce();
    }

    public void ResetSave()
    {
        Save = new SaveData();
        Persist();
        World = null;
        Result = null;
        Mode = Mode.Hub;
        HubView = HubView.Cell;
        RollMissions();
        RebuildPreview();
        Announce();
    }

    /// <summary>Rolls a new mission day when the calendar moved on, and persists it right away.</summary>
    public bool RollMissions()
    {
        if (!MissionRoll.Ensure(Save)) return false;
        Persist();
        return true;
    }

    public void Persist() => _storage.Write(SaveSerializer.Serialize(Save));
}
