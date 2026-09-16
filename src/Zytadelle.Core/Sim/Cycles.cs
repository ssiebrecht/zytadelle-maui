using Zytadelle.Core.Entities;

namespace Zytadelle.Core.Sim;

public static class Cycles
{
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
        w.SpawnTimer = SpawnBalance.SpawnInterval(w.Cycle) * CycleBalance.SpawnTrainOffset;
        if (!CycleBalance.IsBossCycle(w.Cycle)) return;
        var b = Spawning.Spawn(w, EnemyKind.Boss);
        if (b is not null) w.PushFx(FxKind.Hit, b.X, b.Y);
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
        // World time is advanced at the end of a step, so this fires exactly once, on the first tick.
        if (w.Time == 0) OnCycleStart(w);

        if (AdvanceTimer(w, dt))
        {
            OnCycleEnd(w);
            OnCycleStart(w);
        }

        if (w.Phase != CyclePhase.Spawn) return;

        w.SpawnTimer -= dt;
        var interval = SpawnBalance.SpawnInterval(w.Cycle);
        var guard = 0;
        while (w.SpawnTimer <= 0 && guard++ < SimulationBalance.MaxSpawnsPerTick)
        {
            w.SpawnTimer += interval;
            Spawning.Spawn(w, Spawning.PickKind(w));
        }
    }
}
