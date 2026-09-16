using Zytadelle.Balancing.Curves;
using Zytadelle.Balancing.Enemies;
using Zytadelle.Balancing.Genes;
using Zytadelle.Balancing.Infections;
using Zytadelle.Balancing.Missions;

namespace Zytadelle.Balancing;

/// <summary>
/// A sanity pass over the whole tuning set. The Balance Lab writes numbers into this project by
/// reflection and a search can propose any number at all, so the shapes that everything downstream
/// assumes - a positive cap, positive increments, a price that is a finite number - are checked here
/// rather than discovered as a hang or a blank screen three screens later.
///
/// It reports; it never corrects. A finding is a statement about the numbers as they stand.
/// </summary>
public static class BalanceCheck
{
    /// <summary>Every problem found, empty when the tuning set is sound.</summary>
    public static IReadOnlyList<string> Validate()
    {
        var found = new List<string>();

        foreach (var c in GeneRegistry.All)
        {
            var id = c.Id;
            if (c.Cap <= 0) found.Add($"{id}: cap is {c.Cap}, must be at least 1");
            if (c.UnlockDna < 0) found.Add($"{id}: unlock price is {c.UnlockDna}");

            CheckFinite(found, $"{id}.value@0", c.ValueAt(0));
            CheckFinite(found, $"{id}.value@cap", c.ValueAt(c.Cap));
            CheckPrice(found, $"{id}.price", c.Price, c.Cap);
            CheckValue(found, $"{id}.valueGrowth", c.ValueGrowth, c.Cap);
            if (c.Price.Exponent <= c.ValueGrowth.Exponent)
                found.Add($"{id}: price powers must exceed increment power for declining long-term efficiency");
        }

        CheckCurve(found, "enemyScaling.hp", EnemyScalingBalance.CycleHp);
        CheckCurve(found, "enemyScaling.atk", EnemyScalingBalance.CycleAtk);

        if (CycleBalance.SpawnDuration <= 0) found.Add("cycle: spawn duration must be positive");
        if (CycleBalance.BossEvery <= 0) found.Add("cycle: boss period must be positive");
        if (SimulationBalance.FixedDt <= 0) found.Add("sim: fixed dt must be positive");
        if (SimulationBalance.Speeds.Length == 0) found.Add("sim: no selectable speeds");
        if (CellBalance.Radius <= 0) found.Add("cell: radius must be positive");
        if (CellBalance.BaseAttackSpeed <= 0) found.Add("cell: base attack speed must be positive");
        if (CellBalance.ProjectileSpeed <= 0) found.Add("cell: projectile speed must be positive");
        if (CellBalance.ProjectileLifetime <= 0) found.Add("cell: projectile lifetime must be positive");
        if (CellBalance.RicochetHopLifetime <= 0) found.Add("cell: ricochet hop lifetime must be positive");
        if (SimulationBalance.MaxEnemies <= 0) found.Add("simulation: enemy cap must be positive");
        if (RenderBalance.MaxProjectiles <= 0) found.Add("render: projectile cap must be positive");

        for (var cycle = 1; cycle <= CycleSpan; cycle++)
        {
            if (SpawnBalance.Rate(cycle) <= 0) found.Add($"spawn: rate is not positive at cycle {cycle}");
            if (SpawnBalance.Weights(cycle).Sum <= 0) found.Add($"spawn: mix sums to zero at cycle {cycle}");
        }

        CheckShare(found, "fast", SpawnBalance.ShareFast);
        CheckShare(found, "tank", SpawnBalance.ShareTank);
        CheckShare(found, "ranged", SpawnBalance.ShareRanged);

        foreach (var rule in MissionBalance.Rules)
        {
            if (rule.Mult <= 0) found.Add($"mission {rule.Id}: multiplier is {rule.Mult}");
            if (rule.Floor < 0) found.Add($"mission {rule.Id}: floor is {rule.Floor}");
        }
        if (MissionBalance.RewardPctMin > MissionBalance.RewardPctMax)
            found.Add("missions: reward percentage range is inverted");
        if (MissionBalance.MissionsPerDay <= 0)
            found.Add("missions: a day rolls no missions");
        if (MissionBalance.GroupCap.Values.Sum() < MissionBalance.MissionsPerDay)
            found.Add("missions: the group caps cannot fill a day");

        foreach (var inf in InfectionBalance.All)
        {
            if (inf.HpMult <= 0) found.Add($"infection {inf.Infection}: hp multiplier is {inf.HpMult}");
            if (inf.AtkMult <= 0) found.Add($"infection {inf.Infection}: attack multiplier is {inf.AtkMult}");
            if (inf.DnaMult <= 0) found.Add($"infection {inf.Infection}: dna multiplier is {inf.DnaMult}");
        }

        foreach (var kind in Enum.GetValues<EnemyKind>())
        {
            var d = EnemyBalance.Def(kind);
            if (d.Radius <= 0) found.Add($"enemy {kind}: radius is {d.Radius}");
            if (d.Speed <= 0) found.Add($"enemy {kind}: speed is {d.Speed}");
            if (d.AttackInterval <= 0) found.Add($"enemy {kind}: attack interval is {d.AttackInterval}");
        }

        foreach (var fade in new (string Name, double Value)[]
                 {
                     ("crit", RenderBalance.CritFxFade), ("hit", RenderBalance.HitFxFade),
                     ("atp", RenderBalance.AtpFxFade), ("boss flash", RenderBalance.BossFlashFade),
                 })
        {
            if (fade.Value <= 0) found.Add($"render: {fade.Name} fade is {fade.Value}");
            if (fade.Value > SimulationBalance.FxLifetime)
                found.Add($"render: {fade.Name} fade outlives the effect ring ({fade.Value} > {SimulationBalance.FxLifetime})");
        }

        return found;
    }

    /// <summary>Cycles the cycle-shaped curves are walked over. Well past where a culture ever gets.</summary>
    private const int CycleSpan = 500;

    /// <summary>
    /// A spawn share is read through a clamp at zero, so a negative one is not a crash. Below zero
    /// at the start is the intended shape - it is how a pathogen is held back until its cycle. Below
    /// zero again after it has been positive is not: the species would vanish from the mix for good.
    /// </summary>
    private static void CheckShare(List<string> found, string name, LogCurve share)
    {
        var seen = false;
        for (var cycle = 1; cycle <= CycleSpan; cycle++)
        {
            var v = share.At(cycle);
            if (v > 0) seen = true;
            else if (seen)
            {
                found.Add($"spawn: {name} share falls back to {v} at cycle {cycle} after having been positive");
                return;
            }
        }
        if (!seen) found.Add($"spawn: {name} share is never positive over {CycleSpan} cycles");
    }

    private static void CheckPrice(List<string> found, string what, PriceCurve price, int cap)
    {
        if (!double.IsFinite(price.FirstCost) || price.FirstCost < 1)
            found.Add($"{what}: first cost must be finite and at least 1");
        if (!double.IsFinite(price.Ramp) || price.Ramp <= 0)
            found.Add($"{what}: ramp must be finite and positive");
        if (!double.IsFinite(price.Exponent) || price.Exponent <= 0)
            found.Add($"{what}: price power must be finite and positive");
        CheckFinite(found, $"{what}@0", price.CostAt(0));
        CheckFinite(found, $"{what}@cap", price.CostAt(Math.Max(0, cap - 1)));
    }

    private static void CheckValue(List<string> found, string what, UpgradeValueCurve c, int cap)
    {
        if (!double.IsFinite(c.GainAtCap) || c.GainAtCap <= 0)
            found.Add($"{what}: cap gain must be finite and positive");
        if (!double.IsFinite(c.Ramp) || c.Ramp <= 0)
            found.Add($"{what}: ramp must be finite and positive");
        if (!double.IsFinite(c.Exponent) || c.Exponent < 0)
            found.Add($"{what}: increment power must be finite and nonnegative");
        if (!double.IsFinite(c.WaveAmplitude) || c.WaveAmplitude < 0 || c.WaveAmplitude >= 1)
            found.Add($"{what}: wave amplitude must be in [0, 1)");
        if (!double.IsFinite(c.WavePeriod) || c.WavePeriod < 3)
            found.Add($"{what}: wave period must be finite and at least 3");
        CheckFinite(found, $"{what}.phase", c.WavePhase);
        // Endpoints are explicit anchors; sample the interior to detect overflow in the weights.
        CheckFinite(found, $"{what}@mid", c.SumTo(cap / 2, cap));
    }

    private static void CheckCurve(List<string> found, string what, Curve c)
    {
        CheckFinite(found, $"{what}@1", c.ValueAt(1));
        if (c.Tail is null)
        {
            if (c.TailFrom != 0) found.Add($"{what}: tailFrom is {c.TailFrom} but there is no tail");
            return;
        }
        if (c.TailFrom <= 1) found.Add($"{what}: tail takes over at {c.TailFrom}, before the curve starts");
        CheckCurve(found, $"{what}.tail", c.Tail);
    }

    private static void CheckFinite(List<string> found, string what, double v)
    {
        if (!double.IsFinite(v)) found.Add($"{what} is {v}");
    }
}
