using Zytadelle.Balancing.Curves;

namespace Zytadelle.Balancing.Enemies;

/// <summary>
/// What a basic pathogen on Infection 1 is worth at a given cycle. Every other pathogen and every
/// tier is a multiple of these two curves. Cycle 1 is 2.35 HP / 1.175 damage, cycle 100 roughly
/// 4 000 / 400. Both change character at cycle 90 and carry a second segment from there.
/// </summary>
public static class EnemyScalingBalance
{
    public static Curve CycleHp { get; set; } = new()
    {
        Base = 2.35, Rate = 3.45403, Decay = 1.195, Floor = 0.025, Shift = 6,
        TailFrom = 90,
        Tail = new Curve { Base = 2767.44, Rate = 0.865662, Decay = 0.785, Floor = 0.005, Shift = 64 },
    };

    public static Curve CycleAtk { get; set; } = new()
    {
        Base = 1.175, Rate = 27.5042, Decay = 1.55, Floor = 0.008, Shift = 24,
        TailFrom = 90,
        Tail = new Curve { Base = 296.462, Rate = 1.89328, Decay = 0.935, Floor = 0.001, Shift = 96 },
    };

    public static double BasicHpAt(double cycle) => CycleHp.ValueAt(Math.Max(1, cycle));

    public static double BasicAtkAt(double cycle) => CycleAtk.ValueAt(Math.Max(1, cycle));
}
