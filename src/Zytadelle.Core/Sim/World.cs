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

    /// <summary>
    /// The per-cycle-pure part of spawning and kill payout, refreshed by
    /// <see cref="Sim.Cycles.EnsureCache"/> whenever <see cref="CacheCycle"/> falls behind
    /// <see cref="Cycle"/> or <see cref="CacheRevision"/> falls behind <see cref="BalanceRevision.Current"/>.
    /// Each field is one pure sub-expression of a larger formula, never a pre-multiplied combination
    /// of two - the arithmetic that reads it stays byte-for-byte the same operation order as before,
    /// just with the cycle-only part computed once per cycle instead of on every call.
    /// </summary>
    public int CacheCycle = -1;

    public int CacheRevision = -1;

    public double SpawnIntervalC;

    public SpawnMix WeightsC;

    public double WeightsSumC;

    public double HpAtC;

    public double AtkAtC;

    public double BaseAtpPerKillC;

    /// <summary>Append-only within a tick, compacted at the end. The order is part of the state.</summary>
    public readonly StructList<Enemy> Enemies = new(SimulationBalance.MaxEnemies + 1);

    public readonly StructList<Projectile> Projectiles = new(256);

    /// <summary>Counts kills since the last compaction, so <see cref="Sim.Step.Run"/> can skip the
    /// O(n) <c>RemoveAll</c> on the (usual) tick where nothing died.</summary>
    public int DeadEnemies;

    /// <summary>Same as <see cref="DeadEnemies"/>, for expired/landed/exhausted toxins and shots.</summary>
    public int DeadProjectiles;

    /// <summary>Scratch buffer for <see cref="Sim.Combat.CompactEnemies"/>'s old-index to new-index
    /// remap (-1 for a removed enemy). Reused across ticks so a busy culture does not allocate one
    /// every compaction; grows if an unusually large enemy list ever needs a bigger one.</summary>
    public int[] EnemyRemap = new int[SimulationBalance.MaxEnemies + 1];

    /// <summary>Backs every bounce-capable toxin's hit list - see <see cref="Entities.Projectile.HitSlot"/>.</summary>
    public readonly IntSlab HitSlab = new(64);

    /// <summary>Scratch for a multishot volley's already-targeted ids, reused across shots instead
    /// of a fresh List per shot; grown lazily, so a build that never buys Multishot never pays for it.</summary>
    public int[] MultishotScratch = [];

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

    /// <summary>
    /// Off for a headless run that never renders, to skip work nothing reads back: the simulation's
    /// own determinism hash never reads Fx or the income windows, only the render-only probe does -
    /// so flipping either can never change what a culture becomes (see the golden test suite's
    /// Determinism/RenderFlagsTests, which asserts exactly that).
    /// </summary>
    public bool RecordFx = true;

    public bool TrackIncome = true;

    public readonly FxRing Fx = new(SimulationBalance.FxRingCap);

    /// <summary>One sample per simulated second, for the per-minute readouts.</summary>
    public readonly RateWindow AtpWindow = new(SimulationBalance.RateWindowSeconds);

    public readonly RateWindow DnaWindow = new(SimulationBalance.RateWindowSeconds);

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
        // Warms the per-cycle cache before the first tick - and before any direct Spawning.Spawn
        // call a caller might make without ever running a tick, such as a benchmark that spawns
        // straight into a fresh World.
        Cycles.EnsureCache(w);
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
        if (RecordFx) Fx.Push(kind, x, y, value);
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
