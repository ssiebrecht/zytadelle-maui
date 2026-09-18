using Zytadelle.Core.Engine;

namespace Zytadelle.Core.Entities;

/// <summary>
/// One pathogen. Positions are metres with the cell at the origin, so the distance to the cell is
/// just the length of (X, Y). A struct, held in a <see cref="StructList{T}"/>, so a spawn is a
/// slot write instead of an allocation. The fields from <see cref="AttackInterval"/> down to
/// <see cref="Dna"/> are hoisted off its <c>EnemyDef</c> at spawn time (see
/// <see cref="Sim.Spawning.Spawn"/>) so the hot loops in <c>Combat</c> never chase that pointer -
/// a Gene-Lab edit to those numbers only ever reaches pathogens spawned after it, which is fine
/// since Balancing statics are fixed for the lifetime of a campaign.
/// </summary>
public struct Enemy : IEntity
{
    /// <summary>
    /// Identity, and at the same time the renderer's decoration seed: flagella lengths, granule
    /// layout and septum placement are hashed from it. Never renumber a living pathogen or its
    /// anatomy will flicker.
    /// </summary>
    public int Id;

    public EnemyKind Kind;

    public double X;
    public double Y;
    public double Hp;
    public double MaxHp;
    public double Atk;
    public double Speed;
    public double Radius;

    /// <summary>The cycle it spawned in; drives the decay on its DNA drop.</summary>
    public int SpawnCycle;

    public double AttackInterval;

    /// <summary>Whether this kind shoots from range instead of closing to melee.</summary>
    public bool IsRanged;

    /// <summary>Fraction of the cell's Reach it stops at. Only meaningful when <see cref="IsRanged"/>.</summary>
    public double RangeFrac;

    /// <summary>Seconds from reaching the fire line to the first shot. Only meaningful when <see cref="IsRanged"/>.</summary>
    public double Windup;

    /// <summary>Only meaningful when <see cref="IsRanged"/>.</summary>
    public double ProjectileSpeed;

    /// <summary>DNA it drops when it dies, before the per-kill and tier multipliers.</summary>
    public double Dna;

    /// <summary>
    /// Heat-up multiplier on the damage it deals. Never resets once it has climbed. A struct's
    /// default is 0, not the balance default - unlike the class field initializer it replaces,
    /// nothing sets this implicitly, so <see cref="Sim.Spawning.Spawn"/> always assigns it explicitly.
    /// </summary>
    public double DmgMult;

    public double AttackCd;

    /// <summary>Reached its stop distance. A shooting pathogen starts its windup here.</summary>
    public bool Arrived;

    /// <summary>Struct default is false - <see cref="Sim.Spawning.Spawn"/> always sets this explicitly.</summary>
    public bool Alive { get; set; }

    /// <summary>Seconds of hit flash left. Render only.</summary>
    public double Flash;
}

/// <summary>A toxin from the cell or a shot at it. A struct, held in a <see cref="StructList{T}"/>.</summary>
public struct Projectile : IEntity
{
    public int Id;

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
    /// pathogen; a shot at the cell flies straight and is spawned with this at -1 and never
    /// touches it again). -1 means no target - either it never had one, or Combat.CompactEnemies
    /// remapped it there because the pathogen it was tracking died. Valid only until the next
    /// enemy compaction, which either remaps it to that pathogen's new position or clears it;
    /// never index into World.Enemies with it without going through that remap first.
    /// </summary>
    public int TargetIndex;

    /// <summary>The same pathogen's stable identity, alongside <see cref="TargetIndex"/> - compaction
    /// never touches this one, so it stays valid for the golden probe and the sanity assert in
    /// <see cref="Sim.Combat.UpdateProjectiles"/> even across a remap.</summary>
    public int TargetId;

    /// <summary>Diffusion hops left. 0 means the toxin stops on its target.</summary>
    public int Bounces;

    /// <summary>
    /// Slot in World.HitSlab holding the pathogen ids this toxin already hit - it never diffuses
    /// back into one of them. -1 for a shot that was never bounce-capable (Bounces was 0 at
    /// spawn), which never rents a slot at all.
    /// </summary>
    public int HitSlot;

    /// <summary>How many of HitSlot's rented capacity are filled so far.</summary>
    public int HitCount;

    /// <summary>Hops taken so far. A rupture counts once per toxin, not once per hop.</summary>
    public int Hops;

    public double Life;

    /// <summary>Struct default is false - every spawn site sets this explicitly to true.</summary>
    public bool Alive { get; set; }
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
public struct FxEvent
{
    public FxKind Kind;
    public double X;
    public double Y;
    public double Value;

    /// <summary>Age in seconds.</summary>
    public double T;
}
