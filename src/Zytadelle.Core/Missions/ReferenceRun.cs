using Zytadelle.Core.Persistence;
using Zytadelle.Core.Progression;
using Zytadelle.Core.Upgrades;

namespace Zytadelle.Core.Missions;

/// <summary>
/// What one culture up to the anchor cycle is worth, computed from the spawn tables and the
/// player's Gene Lab levels. Targets are multiples of this, so they scale with the player instead
/// of being written down anywhere.
///
/// Two simplifications are deliberate: in-culture purchases are ignored and the decay on stale DNA
/// drops is left out. Both under-rate a real culture, so targets land a little easy rather than
/// impossible.
/// </summary>
public static class ReferenceRun
{
    public static MissionReference Compute(SaveData s)
    {
        var cycle = InfectionProgress.AnchorCycle(s.BestCycle);
        var infection = InfectionProgress.HighestUnlocked(s.BestCycle);
        var inf = InfectionBalance.Def(infection);
        var stats = Stats.From(s.Lab, new Levels());
        var t = SpawnBalance.Totals(cycle);

        var kills = t.Total;

        // Basics are left out: their drop is a sliver and the estimate is meant to be conservative.
        var dropDna = t.Fast * EnemyBalance.Def(EnemyKind.Fast).Dna
                      + t.Tank * EnemyBalance.Def(EnemyKind.Tank).Dna
                      + t.Ranged * EnemyBalance.Def(EnemyKind.Ranged).Dna
                      + t.Boss * EnemyBalance.Def(EnemyKind.Boss).Dna;
        var dna = dropDna * stats.DnaPerKill * inf.DnaMult + cycle * stats.DnaPerCycle * inf.DnaMult;

        var killAtp = 0.0;
        for (var c = 1; c <= cycle; c++) killAtp += SpawnBalance.SpawnsPerCycle(c) * EconomyBalance.BaseAtpPerKill(c);
        var atp = (killAtp + cycle * stats.AtpPerCycle) * stats.AtpBonus;

        return new MissionReference(
            cycle,
            infection,
            kills,
            t,
            dna,
            atp,
            cycle * CycleBalance.CycleLength,
            stats.CritChance,
            Spend.Total(s));
    }
}
