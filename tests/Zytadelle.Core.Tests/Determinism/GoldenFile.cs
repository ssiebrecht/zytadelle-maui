using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zytadelle.Core.Tests.Determinism;

public sealed record GoldenCheckpointDto(int Cycle, int Tick, string Hash);

/// <summary>RunSummary flattened for JSON; KillsByKind spelled out so a diff shows which kind moved.</summary>
public sealed record GoldenSummaryDto(
    int Cycle, double Dna, double AtpEarned, int Kills, double Time, int Infection,
    double KillsBasic, double KillsFast, double KillsTank, double KillsRanged, double KillsBoss,
    int Crits, int RunBuys)
{
    public static GoldenSummaryDto Of(RunSummary s) => new(
        s.Cycle, s.Dna, s.AtpEarned, s.Kills, s.Time, s.Infection,
        s.KillsByKind.Basic, s.KillsByKind.Fast, s.KillsByKind.Tank, s.KillsByKind.Ranged, s.KillsByKind.Boss,
        s.Crits, s.RunBuys);
}

public sealed record GoldenFileDto(
    int ProbeVersion, string Scenario, int Ticks, bool Died, string FinalHash, string RenderHash,
    List<GoldenCheckpointDto> Checkpoints, GoldenSummaryDto Summary, string GeneratedWith);

/// <summary>
/// Loads, saves and compares golden files. A mismatch reports the first checkpoint that disagrees
/// - within <see cref="ScenarioRunner.CheckpointStride"/> ticks of the actual divergence - rather
/// than just "hash differs", since a bare final-hash mismatch on a 250k-tick run gives a developer
/// nothing to start from.
/// </summary>
public static class GoldenFile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private static string GoldenDir([CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here)!, "..", "Golden");

    private static string PathFor(Scenario scenario) => Path.Combine(GoldenDir(), $"{scenario.Name}.json");

    private static string Hex(ulong v) => v.ToString("x16", CultureInfo.InvariantCulture);

    private static ulong UnHex(string v) => ulong.Parse(v, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    /// <summary>
    /// Regenerates the golden when <c>ZYTADELLE_UPDATE_GOLDEN=1</c> is set, otherwise asserts the
    /// fresh run matches what is on disk. Always run the regeneration pass once, then re-run without
    /// the variable before committing - a golden that was never verified to reproduce itself is worse
    /// than none.
    /// </summary>
    /// <summary>Runs the scenario and checks it against its golden in one call - what every test wants.</summary>
    public static void Verify(Scenario scenario) => AssertMatches(ScenarioRunner.Run(scenario));

    public static void AssertMatches(ScenarioResult result)
    {
        var path = PathFor(result.Scenario);
        if (Environment.GetEnvironmentVariable("ZYTADELLE_UPDATE_GOLDEN") == "1")
        {
            Save(path, result);
            return;
        }

        Assert.True(File.Exists(path),
            $"No golden for {result.Scenario.Name} at {path}. " +
            "Run once with ZYTADELLE_UPDATE_GOLDEN=1 to create it, then again without to verify it reproduces.");

        var expected = Load(path);
        Compare(result.Scenario.Name, expected, result);
    }

    private static void Save(string path, ScenarioResult result)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var dto = new GoldenFileDto(
            result.ProbeVersion, result.Scenario.Name, result.Ticks, result.Died,
            Hex(result.FinalHash), Hex(result.RenderHash),
            result.Checkpoints.Select(c => new GoldenCheckpointDto(c.Cycle, c.Tick, Hex(c.Hash))).ToList(),
            GoldenSummaryDto.Of(result.Summary),
            $"net{Environment.Version}/{RuntimeInformation()}");
        File.WriteAllText(path, JsonSerializer.Serialize(dto, JsonOptions));
    }

    private static string RuntimeInformation() =>
        System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;

    private static GoldenFileDto Load(string path) =>
        JsonSerializer.Deserialize<GoldenFileDto>(File.ReadAllText(path), JsonOptions)
        ?? throw new InvalidOperationException($"Golden at {path} deserialised to null.");

    private static void Compare(string name, GoldenFileDto expected, ScenarioResult actual)
    {
        Assert.True(expected.ProbeVersion == actual.ProbeVersion,
            $"{name}: golden was written by probe version {expected.ProbeVersion}, this run is version {actual.ProbeVersion}. " +
            "Bump WorldProbe.Version and regenerate deliberately if the contract really changed.");

        var firstDivergence = FirstDivergentCheckpoint(expected.Checkpoints, actual.Checkpoints);
        if (firstDivergence is { } d)
        {
            Assert.Fail(d.CountMismatch
                ? $"{name}: golden has {expected.Checkpoints.Count} checkpoints, this run produced {actual.Checkpoints.Count} " +
                  $"(last actual checkpoint: cycle {d.Cycle}, tick {d.Tick}). Likely two different scenarios sharing one " +
                  "golden file name, or a genuinely different stopping point - not a plain hash mismatch."
                : $"{name}: state diverged at cycle {d.Cycle}, tick {d.Tick} (±{ScenarioRunner.CheckpointStride} ticks). " +
                  $"expected hash {d.Expected:x16}, got {d.Actual:x16}.");
        }

        Assert.True(expected.Ticks == actual.Ticks,
            $"{name}: ran {actual.Ticks} ticks, golden has {expected.Ticks}.");
        Assert.True(expected.Died == actual.Died,
            $"{name}: died={actual.Died}, golden says died={expected.Died}.");
        Assert.True(UnHex(expected.FinalHash) == actual.FinalHash,
            $"{name}: final hash differs (expected {expected.FinalHash}, got {actual.FinalHash:x16}) but no checkpoint caught it - checkpoint list itself may have changed shape.");
        Assert.True(UnHex(expected.RenderHash) == actual.RenderHash,
            $"{name}: render hash differs (expected {expected.RenderHash}, got {actual.RenderHash:x16}).");

        CompareSummary(name, expected.Summary, GoldenSummaryDto.Of(actual.Summary));
    }

    private sealed record Divergence(int Cycle, int Tick, ulong Expected, ulong Actual, bool CountMismatch = false);

    private static Divergence? FirstDivergentCheckpoint(List<GoldenCheckpointDto> expected, IReadOnlyList<Checkpoint> actual)
    {
        var n = Math.Min(expected.Count, actual.Count);
        for (var i = 0; i < n; i++)
        {
            var e = expected[i];
            var a = actual[i];
            var eHash = UnHex(e.Hash);
            if (eHash == a.Hash && e.Cycle == a.Cycle && e.Tick == a.Tick) continue;
            return new Divergence(a.Cycle, a.Tick, eHash, a.Hash);
        }
        if (expected.Count != actual.Count)
        {
            var last = actual.Count > 0 ? actual[^1] : null;
            return new Divergence(last?.Cycle ?? -1, last?.Tick ?? -1, 0, last?.Hash ?? 0, CountMismatch: true);
        }
        return null;
    }

    /// <summary>Doubles compare on their raw bits, not on value equality - this is a "did anything
    /// at all change" check, not a numeric tolerance check.</summary>
    private static void CompareSummary(string name, GoldenSummaryDto e, GoldenSummaryDto a)
    {
        Assert.True(e.Cycle == a.Cycle, $"{name}: summary.Cycle expected {e.Cycle}, got {a.Cycle}.");
        Assert.True(e.Infection == a.Infection, $"{name}: summary.Infection expected {e.Infection}, got {a.Infection}.");
        Assert.True(e.Kills == a.Kills, $"{name}: summary.Kills expected {e.Kills}, got {a.Kills}.");
        Assert.True(e.Crits == a.Crits, $"{name}: summary.Crits expected {e.Crits}, got {a.Crits}.");
        Assert.True(e.RunBuys == a.RunBuys, $"{name}: summary.RunBuys expected {e.RunBuys}, got {a.RunBuys}.");
        BitEqual(name, "Dna", e.Dna, a.Dna);
        BitEqual(name, "AtpEarned", e.AtpEarned, a.AtpEarned);
        BitEqual(name, "Time", e.Time, a.Time);
        BitEqual(name, "KillsBasic", e.KillsBasic, a.KillsBasic);
        BitEqual(name, "KillsFast", e.KillsFast, a.KillsFast);
        BitEqual(name, "KillsTank", e.KillsTank, a.KillsTank);
        BitEqual(name, "KillsRanged", e.KillsRanged, a.KillsRanged);
        BitEqual(name, "KillsBoss", e.KillsBoss, a.KillsBoss);
    }

    private static void BitEqual(string name, string field, double expected, double actual) =>
        Assert.True(BitConverter.DoubleToInt64Bits(expected) == BitConverter.DoubleToInt64Bits(actual),
            $"{name}: summary.{field} expected {expected:R}, got {actual:R} (bit patterns differ).");
}
