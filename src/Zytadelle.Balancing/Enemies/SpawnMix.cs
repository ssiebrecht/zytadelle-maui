namespace Zytadelle.Balancing.Enemies;

/// <summary>The spawn mix as percentages. Basics take whatever the other three leave.</summary>
public readonly record struct SpawnMix(double Basic, double Fast, double Tank, double Ranged)
{
    public double Sum => Basic + Fast + Tank + Ranged;

    public double Of(EnemyKind kind) => kind switch
    {
        EnemyKind.Basic => Basic,
        EnemyKind.Fast => Fast,
        EnemyKind.Tank => Tank,
        EnemyKind.Ranged => Ranged,
        _ => 0,
    };
}
