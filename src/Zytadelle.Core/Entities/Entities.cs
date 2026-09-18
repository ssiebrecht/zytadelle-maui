namespace Zytadelle.Core.Entities;

/// <summary>
/// One pathogen. Positions are metres with the cell at the origin, so the distance to the cell is
/// just the length of (X, Y).
/// </summary>
public sealed class Enemy
{
    /// <summary>
    /// Identity, and at the same time the renderer's decoration seed: flagella lengths, granule
    /// layout and septum placement are hashed from it. Never renumber a living pathogen or its
    /// anatomy will flicker.
    /// </summary>
    public required int Id { get; init; }

    public required EnemyKind Kind { get; init; }

    public required EnemyDef Def { get; init; }

    public double X;
    public double Y;
    public double Hp;
    public double MaxHp;
    public double Atk;
    public double Speed;
    public double Radius;

    /// <summary>The cycle it spawned in; drives the decay on its DNA drop.</summary>
    public int SpawnCycle;

    /// <summary>Heat-up multiplier on the damage it deals. Never resets once it has climbed.</summary>
    public double DmgMult = CombatBalance.HeatupBase;

    public double AttackCd;

    /// <summary>Reached its stop distance. A shooting pathogen starts its windup here.</summary>
    public bool Arrived;

    public bool Alive = true;

    /// <summary>Seconds of hit flash left. Render only.</summary>
    public double Flash;
}

/// <summary>A toxin from the cell or a shot at it.</summary>
public sealed class Projectile
{
    public required int Id { get; init; }

    public double X;
    public double Y;
    public double Vx;
    public double Vy;
    public double Speed;
    public double Dmg;
    public bool Crit;
    public bool FromCell;

    /// <summary>
    /// Index into World.Enemies as of the last assignment (a toxin from the cell homes onto a
    /// pathogen; a shot at the cell flies straight and never sets this). -1 means no target -
    /// either it never had one, or Combat.CompactEnemies remapped it there because the pathogen it
    /// was tracking died. Valid only until the next enemy compaction, which either remaps it to
    /// that pathogen's new position or clears it; never index into World.Enemies with it without
    /// going through that remap first.
    /// </summary>
    public int TargetIndex = -1;

    /// <summary>The same pathogen's stable identity, alongside <see cref="TargetIndex"/> - compaction
    /// never touches this one, so it stays valid for the golden probe and the sanity assert in
    /// <see cref="Sim.Combat.UpdateProjectiles"/> even across a remap.</summary>
    public int TargetId = -1;

    /// <summary>Diffusion hops left. 0 means the toxin stops on its target.</summary>
    public int Bounces;

    /// <summary>Pathogens this toxin already hit - it never diffuses back into one of them.</summary>
    public List<int>? Hit;

    /// <summary>Hops taken so far. A rupture counts once per toxin, not once per hop.</summary>
    public int Hops;

    public double Life;
    public bool Alive = true;
}

/// <summary>The player's macrophage.</summary>
public sealed class Cell
{
    public double Hp;
    public double MaxHp;
    public double FireCd;
    public double Flash;
}

public enum FxKind
{
    Kill,
    Crit,
    BossKill,
    Hit,
    Atp,
}

/// <summary>A transient render effect. Aged by the simulation, drawn by the renderer.</summary>
public sealed class FxEvent
{
    public required FxKind Kind { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    public double Value { get; init; }

    /// <summary>Age in seconds.</summary>
    public double T;
}
