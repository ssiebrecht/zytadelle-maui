namespace Zytadelle.Balancing.Enemies;

/// <summary>A count per pathogen type. Used for spawn expectations, the reference run and a finished culture.</summary>
public readonly record struct KindCounts(double Basic, double Fast, double Tank, double Ranged, double Boss)
{
    /// <summary>Every kind added up.</summary>
    public double Total => Basic + Fast + Tank + Ranged + Boss;

    public double Of(EnemyKind kind) => kind switch
    {
        EnemyKind.Basic => Basic,
        EnemyKind.Fast => Fast,
        EnemyKind.Tank => Tank,
        EnemyKind.Ranged => Ranged,
        EnemyKind.Boss => Boss,
        _ => 0,
    };

    public KindCounts With(EnemyKind kind, double value) => kind switch
    {
        EnemyKind.Basic => this with { Basic = value },
        EnemyKind.Fast => this with { Fast = value },
        EnemyKind.Tank => this with { Tank = value },
        EnemyKind.Ranged => this with { Ranged = value },
        EnemyKind.Boss => this with { Boss = value },
        _ => this,
    };
}
