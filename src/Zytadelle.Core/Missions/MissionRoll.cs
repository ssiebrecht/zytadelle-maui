using Zytadelle.Core.Engine;
using Zytadelle.Core.Persistence;

namespace Zytadelle.Core.Missions;

/// <summary>
/// The daily set: five missions per local day, all five rerolled at midnight, whatever is left
/// unfinished expires. Everything here is seeded off the day key, so the same day always rolls the
/// same five on every machine.
/// </summary>
public static class MissionRoll
{
    public static MissionDay Roll(SaveData s, string day)
    {
        var rng = new Rng(DayKey.Seed(day));
        var reference = ReferenceRun.Compute(s);

        // Shuffle the eligible catalog, then take in order while the group caps allow it.
        var pool = MissionBalance.Rules.Where(m => m.Eligible?.Invoke(reference) ?? true).ToList();
        for (var i = pool.Count - 1; i > 0; i--)
        {
            var j = (int)Math.Floor(rng.Next() * (i + 1));
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var used = new Dictionary<MissionGroup, int>();
        var list = new List<MissionSlot>();

        // First pass respects the group caps; the second fills up if the eligible catalog was thin.
        foreach (var respectCaps in new[] { true, false })
        {
            foreach (var rule in pool)
            {
                if (list.Count >= MissionBalance.MissionsPerDay) break;
                if (list.Any(x => x.Id == rule.Id)) continue;
                if (respectCaps && used.GetValueOrDefault(rule.Group) >= MissionBalance.GroupCap[rule.Group]) continue;

                used[rule.Group] = used.GetValueOrDefault(rule.Group) + 1;
                var pct = MissionBalance.RewardPctMin + rng.Next() * (MissionBalance.RewardPctMax - MissionBalance.RewardPctMin);
                list.Add(new MissionSlot
                {
                    Id = rule.Id,
                    Target = MissionBalance.TargetFor(rule, reference),
                    Pct = MissionBalance.RoundPct(pct),
                });
            }
        }

        return new MissionDay { Day = day, List = list };
    }

    /// <summary>
    /// Rolls a new day when the calendar moved on. A clock put back keeps the current set, which is
    /// as much anti-cheat as a locally editable save can justify.
    /// </summary>
    public static bool Ensure(SaveData s, string? key = null)
    {
        key ??= DayKey.Today();
        if (s.Missions is not null && string.CompareOrdinal(s.Missions.Day, key) >= 0) return false;
        s.Missions = Roll(s, key);
        return true;
    }

    /// <summary>
    /// Books a finished culture and returns how many missions it completed. A culture that outlasts
    /// midnight is booked into the new day, because the day is settled first.
    /// </summary>
    public static int ApplyRun(SaveData s, RunStats run)
    {
        if (s.Missions is null) return 0;
        var completed = 0;
        foreach (var m in s.Missions.List)
        {
            if (m.Claimed) continue;
            var rule = MissionBalance.Rule(m.Id);
            var was = m.IsDone;
            var v = MissionCatalog.Text(m.Id).Read(run);
            m.Progress = rule.Scope == MissionScope.Sum
                ? Math.Min(m.Target, m.Progress + v)
                : Math.Max(m.Progress, Math.Min(m.Target, v));
            if (!was && m.IsDone) completed++;
        }
        return completed;
    }

    /// <summary>
    /// What a mission pays right now. The percentage was frozen when the day rolled, the investment
    /// it applies to is read live - so investing during the day raises what the day still owes.
    /// </summary>
    public static double Reward(SaveData s, MissionSlot m) => MissionBalance.Reward(Spend.Total(s), m.Pct);

    public static double Claim(SaveData s, int index)
    {
        var m = s.Missions?.List.ElementAtOrDefault(index);
        if (m is null || m.Claimed || !m.IsDone) return 0;
        var dna = Reward(s, m);
        m.Claimed = true;
        s.Dna += dna;
        s.TotalDna += dna;
        return dna;
    }

    public static int ClaimableCount(SaveData s) =>
        s.Missions?.List.Count(m => !m.Claimed && m.IsDone) ?? 0;
}
