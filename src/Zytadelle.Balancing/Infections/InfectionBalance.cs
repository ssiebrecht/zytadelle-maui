namespace Zytadelle.Balancing.Infections;

/// <summary>The four infections and the record that unlocks the next one.</summary>
public static class InfectionBalance
{
    /// <summary>
    /// The Tower's tiers 1-4. The DNA bonus series continues 1, 1.8, 2.6, 3.4, 4.2 if a fifth tier
    /// is ever added.
    /// </summary>
    public static IReadOnlyList<InfectionDef> All { get; } =
    [
        new(1, "Infection 1", HpMult: 1, AtkMult: 1, DnaMult: 1),
        new(2, "Infection 2", HpMult: 20, AtkMult: 20, DnaMult: 1.8),
        new(3, "Infection 3", HpMult: 60, AtkMult: 60, DnaMult: 2.6),
        new(4, "Infection 4", HpMult: 120, AtkMult: 114.9, DnaMult: 3.4),
    ];

    /// <summary>Cycle the previous infection must have reached to open the next one.</summary>
    public static int UnlockCycle { get; set; } = 100;

    /// <summary>Falls back to the first tier, so a corrupt save cannot crash the hub.</summary>
    public static InfectionDef Def(int infection) =>
        All.FirstOrDefault(t => t.Infection == infection) ?? All[0];
}
