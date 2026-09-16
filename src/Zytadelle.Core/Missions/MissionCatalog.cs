namespace Zytadelle.Core.Missions;

/// <summary>Unit of a mission target, for display.</summary>
public enum MissionFmt
{
    Count,
    Time,
    Dna,
    Atp,
}

/// <summary>The wording and the counter of one mission. Its difficulty lives in <see cref="MissionBalance"/>.</summary>
public sealed record MissionText(MissionFmt Fmt, Func<RunStats, double> Read, Func<string, string> Label);

public static class MissionCatalog
{
    private static readonly Dictionary<MissionId, MissionText> Texts = new()
    {
        [MissionId.CycleSingle] = new(MissionFmt.Count, s => s.Cycle, t => $"Reach cycle {t} in one culture"),
        [MissionId.TimeSingle] = new(MissionFmt.Time, s => s.Time, t => $"Hold one culture for {t}"),
        [MissionId.CyclesTotal] = new(MissionFmt.Count, s => s.Cycle, t => $"Clear {t} cycles today"),
        [MissionId.KillsTotal] = new(MissionFmt.Count, s => s.Kills, t => $"Lyse {t} pathogens today"),
        [MissionId.KillsSingle] = new(MissionFmt.Count, s => s.Kills, t => $"Lyse {t} pathogens in one culture"),
        [MissionId.BossTotal] = new(MissionFmt.Count, s => s.KillsByKind.Boss, t => $"Lyse {t} paramecia"),
        [MissionId.FastTotal] = new(MissionFmt.Count, s => s.KillsByKind.Fast, t => $"Lyse {t} spirochetes"),
        [MissionId.TankTotal] = new(MissionFmt.Count, s => s.KillsByKind.Tank, t => $"Lyse {t} staphylococci"),
        [MissionId.RangedTotal] = new(MissionFmt.Count, s => s.KillsByKind.Ranged, t => $"Lyse {t} phages"),
        [MissionId.CritTotal] = new(MissionFmt.Count, s => s.Crits, t => $"Land {t} ruptures"),
        [MissionId.DnaTotal] = new(MissionFmt.Dna, s => s.Dna, t => $"Harvest {t} DNA today"),
        [MissionId.AtpTotal] = new(MissionFmt.Atp, s => s.AtpEarned, t => $"Generate {t} ATP today"),
        [MissionId.BuysTotal] = new(MissionFmt.Count, s => s.RunBuys, t => $"Buy {t} in-culture upgrades"),
        [MissionId.Cultures] = new(MissionFmt.Count, _ => 1, t => $"Finish {t} cultures"),
    };

    public static MissionText Text(MissionId id) => Texts[id];
}
