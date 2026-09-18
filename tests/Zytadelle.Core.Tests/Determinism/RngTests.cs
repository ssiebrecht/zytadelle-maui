using System.Globalization;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Zytadelle.Core.Tests.Determinism;

/// <summary>
/// A standalone regression guard on the mulberry32 port, independent of any <see cref="World"/>.
/// Every scenario golden's determinism ultimately rests on this sequence never moving; keeping it
/// as its own fast check means a break here is reported as "the RNG changed", not as sixty
/// confusing scenario diffs at once.
/// </summary>
public sealed class RngTests
{
    private static readonly uint[] Seeds = [0u, 1u, 2u, 0x9E3779B9u];

    public static IEnumerable<object[]> Cases() => Seeds.Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(Cases))]
    public void FirstSixteen_MatchesGolden(uint seed)
    {
        var rng = new Rng(seed);
        var buf = new WordWriter();
        for (var i = 0; i < 16; i++) buf.F64(rng.Next());
        AssertHash($"rng_first16_{seed:x8}", XxHash3.HashToUInt64(buf.Span));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void OneMillionDraws_MatchesGolden(uint seed)
    {
        const int total = 1_000_000;
        const int batch = 8192;

        var rng = new Rng(seed);
        var hasher = new StateHasher();
        var buf = new WordWriter();
        var done = 0;
        while (done < total)
        {
            var n = Math.Min(batch, total - done);
            buf.Reset();
            for (var i = 0; i < n; i++) buf.F64(rng.Next());
            hasher.Append(buf.Span);
            done += n;
        }
        AssertHash($"rng_1e6_{seed:x8}", hasher.Checkpoint());
    }

    private static string GoldenPath(string name, [CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here)!, "..", "Golden", $"{name}.json");

    private static void AssertHash(string name, ulong actual)
    {
        var path = GoldenPath(name);
        if (Environment.GetEnvironmentVariable("ZYTADELLE_UPDATE_GOLDEN") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(new { hash = actual.ToString("x16", CultureInfo.InvariantCulture) }));
            return;
        }

        Assert.True(File.Exists(path), $"No golden at {path}. Run once with ZYTADELLE_UPDATE_GOLDEN=1 to create it.");
        var doc = JsonDocument.Parse(File.ReadAllText(path));
        var expected = ulong.Parse(doc.RootElement.GetProperty("hash").GetString()!, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        Assert.True(expected == actual, $"{name}: expected {expected:x16}, got {actual:x16}.");
    }
}
