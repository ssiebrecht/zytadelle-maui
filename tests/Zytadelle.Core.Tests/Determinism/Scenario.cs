namespace Zytadelle.Core.Tests.Determinism;

/// <summary>
/// The four starting cultures the goldens are built from. Picked to stress different parts of the
/// tick: <see cref="Stress"/> fills the enemy cap and never buys, <see cref="Empty"/> is the bare
/// starting gene set (and dies mid-tick against the harder tiers), <see cref="MidOffense"/> exercises
/// crits and the ATP-slowdown branch, <see cref="BounceMulti"/> exercises multishot and diffusion.
/// </summary>
public enum Build
{
    Empty,
    Stress,
    MidOffense,
    BounceMulti,
}

/// <summary>
/// One reproducible run: a starting build, an infection tier, a seed and how long to run it.
/// <see cref="Name"/> is the golden file's stem, so it doubles as the run's on-disk identity.
/// </summary>
public sealed record Scenario(Build Build, int Tier, uint Seed, int MaxCycles, bool LongTrait = false)
{
    /// <summary>
    /// Includes MaxCycles: Stress and MidOffense each have a Long-trait case at the same
    /// (Build, Tier, Seed) as one of their standard cases, differing only in how far it runs, and
    /// this is the on-disk golden identity - two scenarios must never share a file.
    /// </summary>
    public string Name => $"{Build}_t{Tier}_s{Seed:x8}_c{MaxCycles}";

    /// <summary>Ticks between purchase decisions. Stress never buys, so it runs no policy at all.</summary>
    public int? PolicyEveryTicks => Build == Build.Stress ? null : 30;

    public World CreateWorld() =>
        World.Create(Tier, BuildLevels.LabFor(Build), BuildLevels.UnlockedFor(Build), Seed);
}

/// <summary>The Gene Lab preset and unlock set behind each <see cref="Build"/>.</summary>
public static class BuildLevels
{
    /// <summary>Genes with no DNA unlock cost - what <see cref="Build.Empty"/> starts with.</summary>
    private static readonly GeneId[] FreeGenes =
    [
        GeneId.Damage, GeneId.AttackSpeed, GeneId.CritChance, GeneId.CritDamage, GeneId.Health, GeneId.Regen,
    ];

    private static readonly (GeneId Id, int Level)[] StressLevels =
    [
        (GeneId.Health, 400), (GeneId.Regen, 320), (GeneId.DefPct, 99), (GeneId.DefAbs, 700), (GeneId.Range, 40),
    ];

    private static readonly (GeneId Id, int Level)[] MidOffenseLevels =
    [
        (GeneId.Damage, 60), (GeneId.AttackSpeed, 25), (GeneId.CritChance, 20), (GeneId.CritDamage, 20),
        (GeneId.Range, 15), (GeneId.Health, 40), (GeneId.Regen, 10), (GeneId.AtpBonus, 10),
    ];

    private static readonly (GeneId Id, int Level)[] BounceMultiLevels =
    [
        (GeneId.Damage, 40), (GeneId.AttackSpeed, 30), (GeneId.Range, 20), (GeneId.MultishotChance, 60),
        (GeneId.MultishotTargets, 5), (GeneId.BounceChance, 60), (GeneId.BounceTargets, 5),
        (GeneId.BounceRange, 30), (GeneId.Health, 60), (GeneId.Regen, 20),
    ];

    private static (GeneId Id, int Level)[] LevelsOf(Build build) => build switch
    {
        Build.Empty => [],
        Build.Stress => StressLevels,
        Build.MidOffense => MidOffenseLevels,
        Build.BounceMulti => BounceMultiLevels,
        _ => throw new ArgumentOutOfRangeException(nameof(build)),
    };

    public static Levels LabFor(Build build)
    {
        var lab = new Levels();
        foreach (var (id, level) in LevelsOf(build)) lab[id] = level;
        return lab;
    }

    /// <summary>
    /// Empty stays down to the free genes, on purpose - it is also the "almost nothing unlocked"
    /// case. Every other build unlocks the whole catalog so its purchase policy has the full board
    /// to shop from, matching the debug stress culture it is modelled on.
    /// </summary>
    public static IEnumerable<GeneId> UnlockedFor(Build build) =>
        build == Build.Empty ? FreeGenes : UpgradeCatalog.All.Select(u => u.Id);
}

/// <summary>The 24-case grid plus the long-running traits, as described in the plan.</summary>
public static class ScenarioMatrix
{
    public static readonly uint[] Seeds = [1u, 2u, 0x9E3779B9u];

    private static int BaseMaxCycles(Build build) => build == Build.Stress ? 12 : 40;

    /// <summary>Tier 1 across every seed, plus tiers 2-4 at seed 1 - six cases per build.</summary>
    public static IEnumerable<Scenario> Standard(Build build)
    {
        var maxCycles = BaseMaxCycles(build);
        foreach (var seed in Seeds) yield return new Scenario(build, 1, seed, maxCycles);
        foreach (var tier in new[] { 2, 3, 4 }) yield return new Scenario(build, tier, 1u, maxCycles);
    }

    public static IEnumerable<Scenario> All() => Enum.GetValues<Build>().SelectMany(Standard);

    /// <summary>
    /// The two long-running cases called out in the plan: Stress far enough to be a real perf
    /// canary (120 cycles, ~252k ticks) and MidOffense far enough to cross the ATP-slowdown branch
    /// at cycle 200 (<see cref="EconomyBalance.AtpKillSlowCycle"/>).
    /// </summary>
    public static IEnumerable<Scenario> Long()
    {
        yield return new Scenario(Build.Stress, 1, 1u, 120, LongTrait: true);
        yield return new Scenario(Build.MidOffense, 1, 1u, 210, LongTrait: true);
    }
}
