using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing;

/// <summary>Everything numeric about one gene. Its name and description are content, not balance.</summary>
public sealed class UpgradeCurves
{
    /// <summary>Highest level, Gene Lab and in-culture combined.</summary>
    public required int Cap { get; init; }

    /// <summary>DNA to unlock the gene in the Gene Lab; 0 = available from the start.</summary>
    public required int UnlockDna { get; init; }

    /// <summary>The value of the gene before any level is bought.</summary>
    public required double ValueBase0 { get; init; }

    /// <summary>Straight-line genes gain this much per level.</summary>
    public double ValuePerLevel { get; init; }

    /// <summary>Value gained by the l-th level. Set only for the four genes that accelerate.</summary>
    public Curve? ValueInc { get; init; }

    public required PriceCurve Dna { get; init; }

    /// <summary>Null = Gene Lab only, no in-culture purchase.</summary>
    public PriceCurve? Atp { get; init; }
}

/// <summary>
/// The value and price curves of all nineteen genes. Fitted against the workshop tables of The
/// Tower; over the levels a culture actually reaches they stay within about 3 % of them on average.
///
/// Three things no continuous curve reproduces are written out as rules instead of fitted away: the
/// two half-price starter levels (see <see cref="PricingBalance"/>), the bracket jumps on Rupture
/// Damage and Diffusion Range, and the tail segments a few curves pick up near their cap.
/// </summary>
public static class UpgradeBalance
{
    private static Curve C(double b, double rate, double decay, double floor, double shift, double tailFrom = 0, Curve? tail = null) =>
        new() { Base = b, Rate = rate, Decay = decay, Floor = floor, Shift = shift, TailFrom = tailFrom, Tail = tail };

    private static PriceCurve P(Curve c, params Bracket[] brackets) =>
        new() { Curve = c, Brackets = brackets.Length == 0 ? null : brackets };

    private static Bracket B(double from, double mult) => new(from, mult);

    private static IReadOnlyDictionary<UpgradeId, UpgradeCurves> _all = Build();

    public static IReadOnlyDictionary<UpgradeId, UpgradeCurves> All
    {
        get => _all;
        set => _all = value;
    }

    public static UpgradeCurves Of(UpgradeId id) => _all[id];

    /// <summary>
    /// Value at a level. Most genes gain a flat amount per level; Toxicity, Membrane, Repair and
    /// Cell Wall gain a growing amount, so their value is the running total of that growth.
    /// </summary>
    public static double ValueAt(UpgradeId id, int level)
    {
        var c = _all[id];
        var l = Math.Min(Math.Max(0, level), c.Cap);
        return c.ValueInc is not null ? c.ValueBase0 + c.ValueInc.SumTo(l) : c.ValueBase0 + c.ValuePerLevel * l;
    }

    public static bool IsMaxed(UpgradeId id, int level) => level >= _all[id].Cap;

    private static Dictionary<UpgradeId, UpgradeCurves> Build() => new()
    {
        [UpgradeId.Damage] = new()
        {
            Cap = 6000, UnlockDna = 0, ValueBase0 = 3,
            ValueInc = C(2.8656, 0.872797, 0.975, 0, 16, 965.5, C(156.316, 0.00632982, 0.215, 0, 96)),
            Dna = P(C(30, 2.86045, 1.075, 0.005, 3, 90, C(64302.1, 3.55703, 1.06, 0, 96))),
            Atp = P(C(10, 40.6457, 1.605, 0.002, 24, 303.5, C(35232, 0.185021, 0.69, 0, 32))),
        },
        [UpgradeId.AttackSpeed] = new()
        {
            Cap = 99, UnlockDna = 0, ValueBase0 = 1, ValuePerLevel = 0.05,
            Dna = P(C(30, 4.70944, 1.21, 0.008, 4)),
            Atp = P(C(5, 36.263, 1.715, 0.012, 12)),
        },
        [UpgradeId.CritChance] = new()
        {
            Cap = 79, UnlockDna = 0, ValueBase0 = 0.01, ValuePerLevel = 0.01,
            Dna = P(C(50, 8.56046, 1.34, 0.008, 8)),
            Atp = P(C(4, 5.60944, 1.24, 0.005, 6)),
        },
        [UpgradeId.CritDamage] = new()
        {
            Cap = 150, UnlockDna = 0, ValueBase0 = 1.2, ValuePerLevel = 0.1,
            Dna = P(C(50, 4.17447, 1.145, 0.003, 6),
                B(101, 6.6254), B(111, 8.162), B(121, 10.6975), B(131, 13.7064), B(141, 16.2128)),
            Atp = P(C(10, 12.8997, 1.375, 0.0015, 16),
                B(101, 1.5312), B(111, 1.5283), B(121, 1.5268), B(131, 1.5244), B(141, 1.5229)),
        },
        [UpgradeId.Range] = new()
        {
            Cap = 79, UnlockDna = 50, ValueBase0 = 30, ValuePerLevel = 0.5,
            Dna = P(C(50, 10.2705, 1.41, 0.012, 8)),
            Atp = P(C(10, 3.16634, 1.08, 0.00025, 6)),
        },
        [UpgradeId.MultishotChance] = new()
        {
            Cap = 99, UnlockDna = 150, ValueBase0 = 0, ValuePerLevel = 0.005,
            Dna = P(C(60, 2.22095, 0.975, 0.00025, 3)),
            Atp = P(C(10, 5.49776, 1.235, 0.005, 6)),
        },
        [UpgradeId.MultishotTargets] = new()
        {
            Cap = 7, UnlockDna = 250, ValueBase0 = 2, ValuePerLevel = 1,
            Dna = P(C(450, 1.75984, 0.21, 0.02, 6)),
            Atp = P(C(125, 1070.11, 1.52, 0, 96)),
        },
        [UpgradeId.BounceChance] = new()
        {
            Cap = 85, UnlockDna = 150, ValueBase0 = 0, ValuePerLevel = 0.008,
            Dna = P(C(200, 11.7668, 1.42, 0.012, 12)),
            Atp = P(C(20, 44.1839, 1.685, 0.008, 16)),
        },
        [UpgradeId.BounceTargets] = new()
        {
            Cap = 7, UnlockDna = 250, ValueBase0 = 1, ValuePerLevel = 1,
            Dna = P(C(700, 1.45155, 0.2, 0.04, 0.5)),
            Atp = P(C(250, 2.4597, 0.2, 0.04, 96)),
        },
        [UpgradeId.BounceRange] = new()
        {
            Cap = 60, UnlockDna = 200, ValueBase0 = 20, ValuePerLevel = 1,
            Dna = P(C(200, 28.0876, 1.585, 0, 16),
                B(41, 10.5533), B(44, 10.5364), B(47, 11.5274), B(50, 11.5056), B(53, 12.5288), B(55, 12.504), B(58, 13.5103)),
            Atp = P(C(20, 24.1811, 1.565, 0.00025, 12),
                B(41, 1.4809), B(46, 1.576), B(51, 1.5683), B(56, 1.5625)),
        },
        [UpgradeId.Health] = new()
        {
            Cap = 6000, UnlockDna = 0, ValueBase0 = 5,
            ValueInc = C(4.65, 7.33176, 0.91, 0.00025, 96, 25, C(55.3672, 2.42122, 1.05, 0, 32)),
            Dna = P(C(30, 1.95587, 0.955, 0, 2, 163.5, C(250932, 1.65909, 0.97, 0, 96))),
            Atp = P(C(10, 12.1536, 1.365, 0.001, 16, 303.5, C(32400, 0.235298, 0.725, 0, 48))),
        },
        [UpgradeId.Regen] = new()
        {
            Cap = 6000, UnlockDna = 0, ValueBase0 = 0,
            ValueInc = C(0.0392, 8.13459, 1.445, 0.005, 8, 735.5, C(569.67, 0.203004, 0.75, 0.00025, 0)),
            Dna = P(C(30, 1.95817, 0.955, 0, 2, 303.5, C(1173100, 0.468253, 0.81, 0, 96))),
            Atp = P(C(5, 3.67559, 1.12, 0, 6, 303.5, C(35231.6, 0.402637, 0.795, 0, 96))),
        },
        [UpgradeId.DefPct] = new()
        {
            Cap = 99, UnlockDna = 75, ValueBase0 = 0, ValuePerLevel = 0.005,
            Dna = P(C(50, 8.20928, 1.33, 0.008, 8)),
            Atp = P(C(5, 1.90866, 0.985, 0.001, 2)),
        },
        [UpgradeId.DefAbs] = new()
        {
            Cap = 5000, UnlockDna = 75, ValueBase0 = 0,
            ValueInc = C(0.5325, 0.538451, 1.54, 0.04, 0, 24, C(3.02744, 1.80072, 1.005, 0.00025, 32)),
            Dna = P(C(50, 4.08255, 1.14, 0.003, 6, 153.5, C(207085, 2.9104, 1.075, 0.00025, 96))),
            Atp = P(C(3, 2.49223, 1.035, 0, 3, 1848.5, C(1485620, 0.00778611, 0.34, 0, 0.25))),
        },
        [UpgradeId.AtpBonus] = new()
        {
            Cap = 149, UnlockDna = 40, ValueBase0 = 1, ValuePerLevel = 0.01,
            Dna = P(C(30, 4.36143, 1.185, 0.008, 4)),
            Atp = P(C(10, 9.66511, 1.33, 0.003, 12)),
        },
        [UpgradeId.AtpPerCycle] = new()
        {
            Cap = 149, UnlockDna = 40, ValueBase0 = 0, ValuePerLevel = 4,
            Dna = P(C(30, 4.36143, 1.185, 0.008, 4)),
            Atp = P(C(16, 51.3312, 1.675, 0.005, 24)),
        },
        // Starting Cash is a lab in The Tower: DNA only, no in-culture purchase.
        [UpgradeId.StartAtp] = new()
        {
            Cap = 99, UnlockDna = 40, ValueBase0 = 0, ValuePerLevel = 5,
            Dna = P(C(30, 7.65929, 1.28, 0.008, 3)),
            Atp = null,
        },
        [UpgradeId.DnaPerKill] = new()
        {
            Cap = 149, UnlockDna = 100, ValueBase0 = 1, ValuePerLevel = 0.01,
            Dna = P(C(50, 5.90511, 1.245, 0.008, 6)),
            Atp = P(C(10, 17.8949, 1.475, 0.005, 16)),
        },
        [UpgradeId.DnaPerCycle] = new()
        {
            Cap = 149, UnlockDna = 100, ValueBase0 = 1, ValuePerLevel = 1,
            Dna = P(C(50, 5.90511, 1.245, 0.008, 6)),
            Atp = P(C(12, 6.08168, 1.24, 0.003, 8)),
        },
    };
}
