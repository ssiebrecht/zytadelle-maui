using Zytadelle.Core.Entities;

namespace Zytadelle.Core.Sim;

public static class Cycles
{
    /// <summary>
    /// Refreshes the per-cycle cache when it has fallen behind the world's current cycle or a live
    /// tuning pass has bumped <see cref="BalanceRevision.Current"/> - the same live-edit immediacy
    /// the uncached reads had, since those re-evaluated the statics fresh on every single call.
    /// Cheap (two int comparisons) when nothing changed, which is every call except a cycle
    /// transition or an actual retune.
    /// </summary>
    public static void EnsureCache(World w)
    {
        if (w.CacheCycle == w.Cycle && w.CacheRevision == BalanceRevision.Current) return;
        w.CacheCycle = w.Cycle;
        w.CacheRevision = BalanceRevision.Current;
        w.SpawnIntervalC = SpawnBalance.SpawnInterval(w.Cycle);
        w.WeightsC = SpawnBalance.Weights(w.Cycle);
        w.WeightsSumC = w.WeightsC.Sum;
        w.HpAtC = EnemyScalingBalance.BasicHpAt(w.Cycle);
        w.AtkAtC = EnemyScalingBalance.BasicAtkAt(w.Cycle);
        w.BaseAtpPerKillC = EconomyBalance.BaseAtpPerKill(w.Cycle);
    }

    /// <summary>
    /// Advances the phase timer and reports whether a cycle just ended. The timer is additive at a
    /// transition - the negative remainder carries into the next phase - so cycles never drift away
    /// from their nominal length no matter what the step size is.
    /// </summary>
    public static bool AdvanceTimer(World w, double dt)
    {
        w.PhaseTimer -= dt;
        if (w.PhaseTimer > 0) return false;

        if (w.Phase == CyclePhase.Spawn)
        {
            w.Phase = CyclePhase.Cooldown;
            w.PhaseTimer += CycleBalance.Cooldown;
            return false;
        }

        w.Phase = CyclePhase.Spawn;
        w.PhaseTimer += CycleBalance.SpawnDuration;
        w.Cycle++;
        return true;
    }

    private static void OnCycleStart(World w)
    {
        // Centred, so exactly one cycle's worth of pathogens fits into the spawn phase.
        w.SpawnTimer = w.SpawnIntervalC * CycleBalance.SpawnTrainOffset;
        if (!CycleBalance.IsBossCycle(w.Cycle)) return;
        var bi = Spawning.Spawn(w, EnemyKind.Boss);
        if (bi >= 0)
        {
            var b = w.Enemies[bi];
            w.PushFx(FxKind.Hit, b.X, b.Y);
        }
    }

    private static void OnCycleEnd(World w)
    {
        // The ATP bonus applies to the cycle payout; the infection tier only multiplies DNA.
        var atpGained = w.Stats.AtpPerCycle * w.Stats.AtpBonus;
        var dnaGained = w.Stats.DnaPerCycle * w.Infection.DnaMult;

        w.Atp += atpGained;
        w.AtpEarned += atpGained;
        w.Dna += dnaGained;
        if (atpGained > 0) w.PushFx(FxKind.Atp, 0, RenderBalance.CycleAtpPopupY, atpGained);
    }

    public static void Update(World w, double dt)
    {
        // Catches a live revision bump even mid-cycle, same as the uncached reads did.
        EnsureCache(w);

        // World time is advanced at the end of a step, so this fires exactly once, on the first tick.
        if (w.Time == 0) OnCycleStart(w);

        if (AdvanceTimer(w, dt))
        {
            // Cycle just changed - refresh before OnCycleEnd/OnCycleStart read the new cycle's cache.
            EnsureCache(w);
            OnCycleEnd(w);
            OnCycleStart(w);
        }

        if (w.Phase != CyclePhase.Spawn) return;

        w.SpawnTimer -= dt;
        var interval = w.SpawnIntervalC;
        var guard = 0;
        while (w.SpawnTimer <= 0 && guard++ < SimulationBalance.MaxSpawnsPerTick)
        {
            w.SpawnTimer += interval;
            Spawning.Spawn(w, Spawning.PickKind(w));
        }
    }
}
