using Zytadelle.Core.Engine;
using Zytadelle.Core.Entities;
using Zytadelle.Core.Upgrades;

namespace Zytadelle.Core.Sim;

public enum CyclePhase
{
    Spawn,
    Cooldown,
}

/// <summary>Everything one culture needs. Created on start, thrown away on death.</summary>
public sealed class World
{
    public required InfectionDef Infection { get; init; }

    public required Rng Rng { get; init; }

    /// <summary>Simulated seconds. Advanced at the end of a step, never during one.</summary>
    public double Time;

    public int Cycle = 1;

    public CyclePhase Phase = CyclePhase.Spawn;

    /// <summary>Seconds left in the current phase. Additive across transitions, so cycles never drift.</summary>
    public double PhaseTimer;

    public double SpawnTimer;

    /// <summary>Append-only within a tick, compacted at the end. The order is part of the state.</summary>
    public readonly List<Enemy> Enemies = [];

    public readonly List<Projectile> Projectiles = [];

    public required Cell Cell { get; init; }

    public double Atp;

    public double Dna;

    /// <summary>Gross ATP earned this culture. Purchases never reduce it.</summary>
    public double AtpEarned;

    public int Kills;

    /// <summary>Kills per type, ruptures landed and in-culture levels bought - the daily missions read these.</summary>
    public KindCounts KillsByKind;

    public int Crits;

    public int RunBuys;

    public required Levels Lab { get; init; }

    public readonly Levels Run = new();

    public required IReadOnlySet<GeneId> Unlocked { get; init; }

    public Stats Stats { get; private set; } = null!;

    public bool Dead;

    /// <summary>Monotonic entity id, shared by pathogens and toxins. Never reused.</summary>
    public int NextId = 1;

    public readonly List<FxEvent> Fx = [];

    /// <summary>One sample per simulated second, for the per-minute readouts.</summary>
    public readonly List<double> AtpWindow = [];

    public readonly List<double> DnaWindow = [];

    public static World Create(int infection, Levels lab, IEnumerable<GeneId> unlocked, uint? seed = null)
    {
        var labCopy = new Levels(lab);
        var stats = Stats.From(labCopy, new Levels());
        var w = new World
        {
            Infection = InfectionBalance.Def(infection),
            Rng = new Rng(seed ?? Rng.TimeSeed()),
            Cell = new Cell { Hp = stats.Health, MaxHp = stats.Health },
            Lab = labCopy,
            Unlocked = unlocked.ToHashSet(),
            PhaseTimer = CycleBalance.SpawnDuration,
            Atp = stats.StartAtp,
        };
        w.Stats = stats;
        return w;
    }

    /// <summary>
    /// Recompute stats after a purchase. A bigger membrane adds its growth to current integrity
    /// rather than leaving the cell at a lower fraction of a larger maximum.
    /// </summary>
    public void RefreshStats()
    {
        var prevMax = Cell.MaxHp;
        Stats = Stats.From(Lab, Run);
        Cell.MaxHp = Stats.Health;
        // Deliberate: the growth is handed over as integrity, so buying Membrane mid-culture is a
        // heal as well as a bigger bar. Shrinking never takes integrity away, only clamps it.
        if (Cell.MaxHp > prevMax) Cell.Hp += Cell.MaxHp - prevMax;
        Cell.Hp = Math.Min(Cell.Hp, Cell.MaxHp);
    }

    public void PushFx(FxKind kind, double x, double y, double value = 0)
    {
        if (Fx.Count > SimulationBalance.FxRingCap) Fx.RemoveAt(0);
        Fx.Add(new FxEvent { Kind = kind, X = x, Y = y, Value = value });
    }
}

/// <summary>What a finished culture is worth. Structurally a superset of the mission counters.</summary>
public sealed record RunSummary(
    int Cycle,
    double Dna,
    double AtpEarned,
    int Kills,
    double Time,
    int Infection,
    KindCounts KillsByKind,
    int Crits,
    int RunBuys)
{
    public static RunSummary Of(World w) => new(
        w.Cycle,
        Math.Floor(w.Dna),
        w.AtpEarned,
        w.Kills,
        w.Time,
        w.Infection.Infection,
        w.KillsByKind,
        w.Crits,
        w.RunBuys);
}
