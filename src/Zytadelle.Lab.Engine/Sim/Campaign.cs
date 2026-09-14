using Zytadelle.Balancing;
using Zytadelle.Core.Persistence;
using Zytadelle.Core.Sim;
using Zytadelle.Core.Upgrades;

namespace Zytadelle.Lab.Engine.Sim;

/// <summary>Per-cycle telemetry, collected only for the builds that end up on a chart.</summary>
/// <param name="Cycle">The cycle these numbers belong to.</param>
/// <param name="Seconds">Seconds it lasted - 35, except the one the cell died in.</param>
/// <param name="Atp">ATP earned during the cycle: kills plus the cycle-end payout.</param>
/// <param name="Dna">DNA earned during the cycle.</param>
/// <param name="Kills">Pathogens lysed during the cycle.</param>
/// <param name="HpFraction">Lowest integrity seen during the cycle, as a fraction of the maximum.</param>
public sealed record CycleSample(int Cycle, double Seconds, double Atp, double Dna, int Kills, double HpFraction);

/// <summary>One culture.</summary>
/// <param name="Cycle">Highest cycle reached: the one the cell died in, or the target.</param>
/// <param name="Dna">DNA banked, floored the way the game banks it.</param>
/// <param name="Seconds">Simulated seconds the culture lasted.</param>
/// <param name="Kills">Pathogens lysed.</param>
/// <param name="RunLevels">In-culture levels bought with ATP.</param>
/// <param name="BuyOrder">Genes in the order they were first bought this culture.</param>
/// <param name="Cycles">Empty unless the run was asked to collect telemetry.</param>
public sealed record RunResult(
    int Cycle,
    double Dna,
    double Seconds,
    int Kills,
    Levels RunLevels,
    IReadOnlyList<UpgradeId> BuyOrder,
    IReadOnlyList<CycleSample> Cycles);

/// <param name="Infection">Difficulty tier every culture of the campaign is run on.</param>
/// <param name="TargetCycle">The cycle a culture has to survive for the campaign to be done.</param>
/// <param name="BuyInterval">Simulated seconds between shop visits inside a culture.</param>
/// <param name="Collect">Whether to keep per-cycle telemetry. Off while ranking, on for the charts.</param>
/// <param name="TimeBudget">Simulated seconds the whole campaign may spend before it is called off.</param>
/// <param name="RunCap">Cultures the campaign may spend, whichever cap bites first.</param>
public sealed record CampaignConfig(
    int Infection = 1,
    int TargetCycle = 50,
    double BuyInterval = 0.5,
    bool Collect = false,
    double TimeBudget = 24 * 3600,
    int RunCap = 150);

/// <summary>A new best cycle, and what it cost to get there.</summary>
/// <param name="Cycle">The record that was just set.</param>
/// <param name="Runs">Cultures spent by then, counting from the start of the campaign.</param>
/// <param name="Seconds">Simulated seconds spent in cultures by then.</param>
public sealed record Milestone(int Cycle, int Runs, double Seconds);

/// <param name="Reached">Whether any culture survived the target cycle.</param>
/// <param name="TimeToTarget">Campaign seconds to the first culture that reached it; infinity if none did.</param>
/// <param name="BestCycle">Deepest cycle any culture reached.</param>
/// <param name="TotalSeconds">Simulated seconds spent in cultures over the whole campaign.</param>
/// <param name="Runs">Cultures spent.</param>
/// <param name="Dna">DNA banked over the whole campaign.</param>
/// <param name="Invested">DNA sunk into the Gene Lab - derived, because buying is the only way it leaves.</param>
/// <param name="Ladder">Every new best cycle and what it cost.</param>
/// <param name="BestRun">The deepest culture, with telemetry. Null unless collecting.</param>
/// <param name="Lab">Gene Lab levels the campaign ended on.</param>
/// <param name="Unlocked">Genes unlocked by the end.</param>
public sealed record CampaignResult(
    bool Reached,
    double TimeToTarget,
    int BestCycle,
    double TotalSeconds,
    int Runs,
    double Dna,
    double Invested,
    IReadOnlyList<Milestone> Ladder,
    RunResult? BestRun,
    Levels Lab,
    IReadOnlyList<UpgradeId> Unlocked);

/// <summary>
/// The Lab's unit of measurement: a whole campaign, not a single culture.
///
/// Cycles run on a timer - a spawn phase plus a cooldown, whether or not anything died - so inside
/// one culture cycle N is always reached after exactly (N-1) cycle lengths. There is nothing to
/// optimise there but survival. "How long until cycle Y" is therefore the loop around the culture:
/// run, die, bank the DNA, spend it in the Gene Lab, run again, until one culture reaches Y. That
/// loop is what a build is scored on, and its ladder of new best cycles is the answer this tool
/// exists to give.
///
/// Every purchase and every tick goes through the game's own code - <see cref="World.Create"/>,
/// <see cref="Step.Run"/>, <see cref="Purchase"/>, <see cref="LabPurchase"/>,
/// <see cref="SaveData.RecordRun"/>. The only thing added here is the decision of what to buy, and
/// a recorder that watches the cycle counter from the outside.
/// </summary>
public static class Campaign
{
    /// <summary>Levels per shop visit, as a player clicking the shop would manage.</summary>
    private const int BuysPerVisit = 5;

    /// <summary>Steps between cancellation checks inside a culture. A culture can be minutes long.</summary>
    private const int CancelCheckSteps = 4096;

    // ---------------------------------------------------------------- one culture

    public static RunResult SimulateRun(Levels lab, IEnumerable<UpgradeId> unlocked, Strategy strategy, uint seed, CampaignConfig cfg, CancellationToken ct = default)
    {
        var w = World.Create(cfg.Infection, lab, unlocked, seed);
        var cycles = new List<CycleSample>();
        var buyOrder = new List<UpgradeId>();

        var buyTimer = 0.0;
        var steps = 0;
        var curCycle = w.Cycle;
        var curStart = 0.0;
        var curAtp = 0.0;
        var curDna = 0.0;
        var curKills = 0;
        var curHp = 1.0;

        while (!w.Dead && w.Cycle <= cfg.TargetCycle)
        {
            if (++steps % CancelCheckSteps == 0) ct.ThrowIfCancellationRequested();

            var atp0 = w.AtpEarned;
            var dna0 = w.Dna;
            var kills0 = w.Kills;
            Step.Run(w, SimulationBalance.FixedDt);

            if (cfg.Collect)
            {
                curAtp += w.AtpEarned - atp0;
                curDna += w.Dna - dna0;
                curKills += w.Kills - kills0;
                curHp = Math.Min(curHp, w.Cell.Hp / w.Cell.MaxHp);

                // The cycle-end payout lands in the same step as the increment, so the step that
                // crosses the boundary is booked to the cycle that just ended - then the bucket closes.
                if (w.Cycle != curCycle)
                {
                    cycles.Add(new CycleSample(curCycle, w.Time - curStart, curAtp, curDna, curKills, curHp));
                    curCycle = w.Cycle;
                    curStart = w.Time;
                    curAtp = 0;
                    curDna = 0;
                    curKills = 0;
                    curHp = 1;
                }
            }

            buyTimer += SimulationBalance.FixedDt;
            if (buyTimer < cfg.BuyInterval) continue;
            buyTimer = 0;
            for (var i = 0; i < BuysPerVisit; i++)
                if (!Shop(w, strategy, buyOrder))
                    break;
        }

        if (cfg.Collect && w.Time > curStart)
            cycles.Add(new CycleSample(curCycle, w.Time - curStart, curAtp, curDna, curKills, curHp));

        return new RunResult(
            Math.Min(w.Cycle, cfg.TargetCycle),
            Math.Floor(w.Dna),
            w.Time,
            w.Kills,
            new Levels(w.Run),
            buyOrder,
            cycles);
    }

    /// <summary>One purchase. False ends the visit: nothing visible, affordable and wanted is left.</summary>
    private static bool Shop(World w, Strategy s, List<UpgradeId> buyOrder)
    {
        if (s.DamageGate)
        {
            // Save for Toxicity while a basic of the next cycle would survive a hit: a cell that
            // cannot one-shot the cheapest pathogen has nothing better to spend on.
            var offer = Purchase.Offer(w, UpgradeId.Damage);
            if (offer.Cost is not null &&
                w.Stats.Damage < EnemyScalingBalance.BasicHpAt(w.Cycle + 1) * w.Infection.HpMult)
                return offer.Affordable && Buy(w, UpgradeId.Damage, buyOrder);
        }

        UpgradeId? pick = null;
        var pickWeight = 0.0;
        foreach (var id in UpgradeIds.All)
        {
            var weight = s.Run(id);
            if (!(weight > pickWeight)) continue;
            var offer = Purchase.Offer(w, id);
            if (!offer.Visible || !offer.Affordable) continue;
            pickWeight = weight;
            pick = id;
        }

        return pick is { } chosen && Buy(w, chosen, buyOrder);
    }

    private static bool Buy(World w, UpgradeId id, List<UpgradeId> buyOrder)
    {
        if (Purchase.Buy(w, id) != BuyResult.Bought) return false;
        if (!buyOrder.Contains(id)) buyOrder.Add(id);
        return true;
    }

    // ---------------------------------------------------------------- the campaign

    /// <summary>
    /// Three termination conditions, every one of them a property of this campaign alone: the
    /// target, a simulated-time budget and a culture count. None of them look at how other
    /// candidates are doing - a cutoff against the current field leader would make the answer depend
    /// on the order the workers happened to finish things in, and the search would stop being
    /// reproducible.
    /// </summary>
    public static CampaignResult Run(Strategy strategy, uint seed, CampaignConfig cfg, CancellationToken ct = default)
    {
        var save = new SaveData();
        var ladder = new List<Milestone>();
        var best = 0;
        RunResult? bestRun = null;
        var elapsed = 0.0;
        var runs = 0;
        var timeToTarget = double.PositiveInfinity;

        while (runs < cfg.RunCap && elapsed < cfg.TimeBudget && best < cfg.TargetCycle)
        {
            ct.ThrowIfCancellationRequested();

            LabAllocator.Allocate(save, strategy.LabWeights, strategy.UnlockK);
            var run = SimulateRun(save.Lab, save.UnlockedIds(), strategy, Seeds.Run(seed, runs), cfg, ct);
            elapsed += run.Seconds;
            runs++;
            save.RecordRun(cfg.Infection, run.Cycle, run.Dna);

            if (run.Cycle <= best) continue;
            best = run.Cycle;
            ladder.Add(new Milestone(best, runs, elapsed));
            if (cfg.Collect) bestRun = run;
            if (best >= cfg.TargetCycle) timeToTarget = elapsed;
        }

        return new CampaignResult(
            best >= cfg.TargetCycle,
            timeToTarget,
            best,
            elapsed,
            runs,
            save.TotalDna,
            // Buying is the only way DNA ever leaves a save - no refund, no respec - so what is left
            // subtracted from what was ever earned is exactly what went into the Gene Lab. Deriving
            // it beats calling Spend, whose prefix cache is not safe to share between workers.
            save.TotalDna - save.Dna,
            ladder,
            bestRun,
            new Levels(save.Lab),
            [.. save.Unlocked]);
    }
}
