namespace Zytadelle.Balancing;

/// <summary>One difficulty tier. The multipliers are constant over cycles.</summary>
/// <param name="Infection">Which tier this is. Identity, not balance.</param>
/// <param name="Name">What the hub calls it.</param>
/// <param name="HpMult">Multiplier on every pathogen’s health.</param>
/// <param name="AtkMult">Multiplier on every pathogen’s damage.</param>
/// <param name="DnaMult">Multiplier on all DNA earned in this tier.</param>
/// <param name="AtpMult">Multiplier on all ATP earned in this tier.</param>
public sealed record InfectionDef(
    [property: BalanceIdentity] int Infection,
    string Name,
    double HpMult,
    double AtkMult,
    double DnaMult,
    double AtpMult);

/// <summary>The four infections and the record that unlocks the next one.</summary>
public static class InfectionBalance
{
    /// <summary>
    /// The Tower's tiers 1-4. There is no per-tier ATP multiplier in the source tables; the DNA
    /// bonus series continues 1, 1.8, 2.6, 3.4, 4.2 if a fifth tier is ever added.
    /// </summary>
    public static IReadOnlyList<InfectionDef> All { get; set; } =
    [
        new(1, "Infection 1", HpMult: 1, AtkMult: 1, DnaMult: 1, AtpMult: 1),
        new(2, "Infection 2", HpMult: 20, AtkMult: 20, DnaMult: 1.8, AtpMult: 1),
        new(3, "Infection 3", HpMult: 60, AtkMult: 60, DnaMult: 2.6, AtpMult: 1),
        new(4, "Infection 4", HpMult: 120, AtkMult: 114.9, DnaMult: 3.4, AtpMult: 1),
    ];

    /// <summary>Cycle the previous infection must have reached to open the next one.</summary>
    public static int UnlockCycle { get; set; } = 100;

    /// <summary>Falls back to the first tier, so a corrupt save cannot crash the hub.</summary>
    public static InfectionDef Def(int infection) =>
        All.FirstOrDefault(t => t.Infection == infection) ?? All[0];

    public static bool IsUnlocked(int infection, IReadOnlyDictionary<int, int> bestCycle) =>
        infection <= 1 || (bestCycle.TryGetValue(infection - 1, out var best) ? best : 0) >= UnlockCycle;

    /// <summary>Highest infection the player has opened.</summary>
    public static int HighestUnlocked(IReadOnlyDictionary<int, int> bestCycle)
    {
        var hi = 1;
        foreach (var t in All)
            if (IsUnlocked(t.Infection, bestCycle))
                hi = t.Infection;
        return hi;
    }
}
