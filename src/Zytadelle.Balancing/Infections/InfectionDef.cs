namespace Zytadelle.Balancing.Infections;

/// <summary>One difficulty tier. The multipliers are constant over cycles.</summary>
/// <param name="Infection">Which tier this is. Identity, not balance.</param>
/// <param name="Name">What the hub calls it.</param>
/// <param name="HpMult">Multiplier on every pathogen's health.</param>
/// <param name="AtkMult">Multiplier on every pathogen's damage.</param>
/// <param name="DnaMult">Multiplier on all DNA earned in this tier.</param>
public sealed record InfectionDef(
    [property: BalanceIdentity] int Infection,
    string Name,
    double HpMult,
    double AtkMult,
    double DnaMult);
