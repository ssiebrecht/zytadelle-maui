using Zytadelle.Balancing.Enemies;

namespace Zytadelle.Balancing.Missions;

/// <summary>
/// What one culture up to the anchor cycle is worth. Every daily target is a multiple of this, so
/// the targets scale with the player instead of being written down.
/// </summary>
public sealed record MissionReference(
    int Cycle,
    int Infection,
    double Kills,
    KindCounts ByKind,
    double Dna,
    double Atp,
    double Time,
    double CritChance,
    double Spent);
