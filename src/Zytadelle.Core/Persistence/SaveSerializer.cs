using System.Text;
using System.Text.Json;

using Zytadelle.Core.Missions;
using Zytadelle.Core.Upgrades;

namespace Zytadelle.Core.Persistence;

/// <summary>
/// Reads and writes the save.
///
/// The JSON keys are the ones the browser build shipped with - <c>coins</c>, <c>workshop</c>,
/// <c>bestWave</c>, <c>selectedTier</c>, <c>totalCoins</c> - and five renamed gene ids are migrated
/// on load. A save exported from the browser therefore loads here unchanged.
///
/// Parsing is deliberately tolerant: anything missing, malformed or unknown falls back to its
/// default instead of failing the load, because the whole file is user-editable anyway.
/// </summary>
public static class SaveSerializer
{
    private const int Version = 1;

    private static readonly Dictionary<string, GeneId> UpgradeByJson = BuildUpgradeNames();
    private static readonly Dictionary<GeneId, string> UpgradeToJson =
        GeneIds.All.ToDictionary(id => id, id => Camel(id));
    private static readonly Dictionary<string, MissionId> MissionByJson =
        Enum.GetValues<MissionId>().ToDictionary(id => Camel(id), id => id);
    private static readonly Dictionary<MissionId, string> MissionToJson =
        Enum.GetValues<MissionId>().ToDictionary(id => id, id => Camel(id));

    private static string Camel(object e)
    {
        var s = e.ToString()!;
        return char.ToLowerInvariant(s[0]) + s[1..];
    }

    private static Dictionary<string, GeneId> BuildUpgradeNames()
    {
        var map = GeneIds.All.ToDictionary(id => Camel(id), id => id);
        // Ids renamed when the game was rethemed. Older saves still carry the old ones.
        map["critFactor"] = GeneId.CritDamage;
        map["cashBonus"] = GeneId.AtpBonus;
        map["cashPerWave"] = GeneId.AtpPerCycle;
        map["coinsPerKill"] = GeneId.DnaPerKill;
        map["coinsPerWave"] = GeneId.DnaPerCycle;
        return map;
    }

    // ---------------------------------------------------------------- write

    public static string Serialize(SaveData s)
    {
        var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteNumber("version", Version);
            w.WriteNumber("coins", s.Dna);

            w.WriteStartObject("workshop");
            foreach (var (id, level) in s.Lab.NonZero()) w.WriteNumber(UpgradeToJson[id], level);
            w.WriteEndObject();

            w.WriteStartArray("unlocked");
            foreach (var id in s.Unlocked) w.WriteStringValue(UpgradeToJson[id]);
            w.WriteEndArray();

            w.WriteStartObject("bestWave");
            foreach (var (infection, cycle) in s.BestCycle) w.WriteNumber(infection.ToString(), cycle);
            w.WriteEndObject();

            w.WriteNumber("selectedTier", s.SelectedInfection);
            w.WriteNumber("speed", s.Speed);
            w.WriteNumber("totalRuns", s.TotalRuns);
            w.WriteNumber("totalCoins", s.TotalDna);

            if (s.Missions is { } day)
            {
                w.WriteStartObject("missions");
                w.WriteString("day", day.Day);
                w.WriteStartArray("list");
                foreach (var m in day.List)
                {
                    w.WriteStartObject();
                    w.WriteString("id", MissionToJson[m.Id]);
                    w.WriteNumber("target", m.Target);
                    w.WriteNumber("pct", m.Pct);
                    w.WriteNumber("progress", m.Progress);
                    w.WriteBoolean("claimed", m.Claimed);
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                w.WriteEndObject();
            }

            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    // ---------------------------------------------------------------- read

    public static SaveData Deserialize(string? raw)
    {
        var d = new SaveData();
        if (string.IsNullOrWhiteSpace(raw)) return d;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(raw);
        }
        catch (JsonException)
        {
            return d;
        }

        using (doc)
        {
            var o = doc.RootElement;
            if (o.ValueKind != JsonValueKind.Object) return d;

            if (Num(o, "coins") is { } coins) d.Dna = Math.Max(0, coins);

            if (o.TryGetProperty("workshop", out var ws) && ws.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in ws.EnumerateObject())
                {
                    if (!UpgradeByJson.TryGetValue(p.Name, out var id)) continue;
                    if (p.Value.ValueKind != JsonValueKind.Number || !p.Value.TryGetDouble(out var v) || v <= 0) continue;
                    d.Lab[id] = (int)Math.Floor(v);
                }
            }

            if (o.TryGetProperty("unlocked", out var un) && un.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in un.EnumerateArray())
                {
                    if (e.ValueKind != JsonValueKind.String) continue;
                    if (!UpgradeByJson.TryGetValue(e.GetString()!, out var id)) continue;
                    if (!d.Unlocked.Contains(id)) d.Unlocked.Add(id);
                }
            }

            if (o.TryGetProperty("bestWave", out var bw) && bw.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in bw.EnumerateObject())
                {
                    if (!int.TryParse(p.Name, out var infection)) continue;
                    if (p.Value.ValueKind != JsonValueKind.Number || !p.Value.TryGetDouble(out var v)) continue;
                    d.BestCycle[infection] = (int)Math.Min(SimulationBalance.MaxCycle, Math.Max(0, Math.Floor(v)));
                }
            }

            if (Num(o, "selectedTier") is { } tier)
                d.SelectedInfection = (int)Math.Min(InfectionBalance.All.Count, Math.Max(1, Math.Floor(tier)));

            if (Num(o, "speed") is { } speed && SimulationBalance.Speeds.Contains((int)speed)) d.Speed = (int)speed;
            if (Num(o, "totalRuns") is { } runs) d.TotalRuns = (int)Math.Floor(runs);
            if (Num(o, "totalCoins") is { } totalCoins) d.TotalDna = totalCoins;

            d.Missions = o.TryGetProperty("missions", out var mis) ? ParseDay(mis) : null;
        }

        return d;
    }

    private static double? Num(JsonElement o, string name) =>
        o.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.Number && e.TryGetDouble(out var v) && double.IsFinite(v)
            ? v
            : null;

    /// <summary>All or nothing: a damaged day is dropped so the next check rolls a clean one.</summary>
    private static MissionDay? ParseDay(JsonElement x)
    {
        if (x.ValueKind != JsonValueKind.Object) return null;
        if (!x.TryGetProperty("day", out var dayEl) || dayEl.ValueKind != JsonValueKind.String) return null;
        if (!x.TryGetProperty("list", out var listEl) || listEl.ValueKind != JsonValueKind.Array) return null;
        if (listEl.GetArrayLength() != MissionBalance.MissionsPerDay) return null;

        var list = new List<MissionSlot>();
        foreach (var e in listEl.EnumerateArray())
        {
            var slot = ParseSlot(e);
            if (slot is null) return null;
            list.Add(slot);
        }

        if (list.Select(m => m.Id).Distinct().Count() != list.Count) return null;
        return new MissionDay { Day = dayEl.GetString()!, List = list };
    }

    private static MissionSlot? ParseSlot(JsonElement e)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;
        if (!e.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.String) return null;
        if (!MissionByJson.TryGetValue(idEl.GetString()!, out var id)) return null;

        if (Num(e, "target") is not { } target || target < 1) return null;
        if (Num(e, "pct") is not { } pct || pct < MissionBalance.RewardPctMin || pct > MissionBalance.RewardPctMax) return null;
        if (Num(e, "progress") is not { } progress || progress < 0) return null;
        if (!e.TryGetProperty("claimed", out var cl) || (cl.ValueKind != JsonValueKind.True && cl.ValueKind != JsonValueKind.False))
            return null;

        return new MissionSlot
        {
            Id = id,
            Target = target,
            Pct = pct,
            Progress = Math.Min(progress, target),
            Claimed = cl.GetBoolean(),
        };
    }
}
