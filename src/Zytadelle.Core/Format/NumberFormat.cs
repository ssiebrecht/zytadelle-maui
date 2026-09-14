using System.Globalization;
using Zytadelle.Core.Upgrades;

namespace Zytadelle.Core.Format;

/// <summary>
/// How numbers read in the UI. Everything goes through the invariant culture: these strings end up
/// in CSS widths and in the save, where a decimal comma would be a bug rather than a preference.
/// </summary>
public static class NumberFormat
{
    private static readonly string[] Suffix = ["", "K", "M", "B", "T", "q", "Q", "s", "S", "O", "N", "D"];

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Num(double n)
    {
        if (!double.IsFinite(n)) return "∞";

        var neg = n < 0;
        var v = Math.Abs(n);

        if (v < 1000)
        {
            var small = v >= 100 || v == Math.Floor(v)
                ? Math.Floor(v).ToString("0", Inv)
                : v.ToString(v >= 10 ? "F1" : "F2", Inv);
            return (neg ? "-" : "") + small;
        }

        var i = 0;
        while (v >= 1000 && i < Suffix.Length - 1)
        {
            v /= 1000;
            i++;
        }

        var s = v >= 100 ? v.ToString("F0", Inv) : v.ToString(v >= 10 ? "F1" : "F2", Inv);
        return (neg ? "-" : "") + s + Suffix[i];
    }

    public static string Atp(double n) => $"{Num(n)} ATP";

    public static string Pct(double p) => (p * 100).ToString(p * 100 >= 10 ? "F0" : "F1", Inv) + "%";

    public static string Mult(double m) => "×" + m.ToString("F2", Inv);

    public static string Meters(double m) => m.ToString("F1", Inv) + "m";

    public static string PerSec(double v) => v.ToString("F2", Inv) + "/s";

    public static string Time(double seconds)
    {
        var m = (int)Math.Floor(seconds / 60);
        var r = (int)Math.Floor(seconds % 60);
        return $"{m}:{r:00}";
    }

    public static string Value(ValueFmt fmt, double v) => fmt switch
    {
        ValueFmt.Pct => Pct(v),
        ValueFmt.Mult => Mult(v),
        ValueFmt.Meters => Meters(v),
        ValueFmt.PerSec => PerSec(v),
        ValueFmt.Atp => Atp(v),
        _ => Num(v),
    };

    /// <summary>Time left until the next local midnight, as the missions panel shows it.</summary>
    public static string UntilMidnight(DateTime? now = null)
    {
        var t = now ?? DateTime.Now;
        var left = t.Date.AddDays(1) - t;
        var h = (int)left.TotalHours;
        var m = left.Minutes;
        return h > 0 ? $"{h}h {m}m" : $"{m}m";
    }
}
