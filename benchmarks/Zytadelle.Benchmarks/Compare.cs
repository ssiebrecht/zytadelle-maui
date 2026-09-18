using System.Globalization;
using System.Text.Json;

namespace Zytadelle.Benchmarks;

/// <summary>
/// Diffs two BenchmarkDotNet full-JSON exports (<c>*-report-full.json</c>, produced by
/// <c>--exporters json</c>) by benchmark full name. Reads only <c>Statistics.Median</c> and
/// <c>Memory.BytesAllocatedPerOperation</c> via <see cref="JsonDocument"/> rather than a strict
/// POCO - BDN's report schema carries far more than that, and binding to all of it would break on
/// the first BDN version bump that adds or renames an unrelated field.
/// </summary>
public static class Compare
{
    public static void Run(string[] args)
    {
        var positional = new List<string>();
        var threshold = 3.0;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--threshold" && i + 1 < args.Length)
            {
                threshold = double.Parse(args[++i], CultureInfo.InvariantCulture);
                continue;
            }
            positional.Add(args[i]);
        }

        if (positional.Count < 2)
        {
            Console.WriteLine("usage: --compare <baseDir> <diffDir> [--threshold percent]");
            return;
        }

        var baseline = LoadReports(positional[0]);
        var current = LoadReports(positional[1]);

        Console.WriteLine($"{"benchmark",-50} {"base ns",10} {"new ns",10} {"time D%",8}   {"base B",10} {"new B",10} {"bytes D%",8}");
        foreach (var name in baseline.Keys.Intersect(current.Keys).OrderBy(n => n, StringComparer.Ordinal))
        {
            var b = baseline[name];
            var c = current[name];
            var timeDelta = Pct(b.Median, c.Median);
            var bytesDelta = Pct(b.Bytes, c.Bytes);
            var flag = Math.Abs(timeDelta) >= threshold || Math.Abs(bytesDelta) >= threshold ? "  <<<" : "";
            Console.WriteLine(
                $"{name,-50} {b.Median,10:F1} {c.Median,10:F1} {timeDelta,7:F1}%   {b.Bytes,10:F0} {c.Bytes,10:F0} {bytesDelta,7:F1}%{flag}");
        }

        var onlyBase = baseline.Keys.Except(current.Keys).ToList();
        var onlyCurrent = current.Keys.Except(baseline.Keys).ToList();
        if (onlyBase.Count > 0) Console.WriteLine($"only in baseline: {string.Join(", ", onlyBase)}");
        if (onlyCurrent.Count > 0) Console.WriteLine($"only in current: {string.Join(", ", onlyCurrent)}");
    }

    private static double Pct(double before, double after) => before == 0 ? 0 : (after - before) / before * 100;

    private sealed record Entry(double Median, double Bytes);

    private static Dictionary<string, Entry> LoadReports(string dir)
    {
        var result = new Dictionary<string, Entry>();
        if (!Directory.Exists(dir))
        {
            Console.WriteLine($"warning: {dir} does not exist");
            return result;
        }

        // BDN's --exporters json produces "-report-full-compressed.json" by default in current
        // versions (minified layout, not gzip - still plain text); match both that and the older
        // pretty-printed "-report-full.json" name.
        foreach (var file in Directory.EnumerateFiles(dir, "*-report-full*.json", SearchOption.AllDirectories))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            if (!doc.RootElement.TryGetProperty("Benchmarks", out var benchmarks)) continue;

            foreach (var b in benchmarks.EnumerateArray())
            {
                var name = b.GetProperty("FullName").GetString() ?? file;
                var median = b.GetProperty("Statistics").GetProperty("Median").GetDouble();
                var bytes = b.TryGetProperty("Memory", out var mem) &&
                            mem.TryGetProperty("BytesAllocatedPerOperation", out var bpo)
                    ? bpo.GetDouble()
                    : 0;
                result[name] = new Entry(median, bytes);
            }
        }
        return result;
    }
}
