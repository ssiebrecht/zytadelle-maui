using Zytadelle.Balancing;

namespace Zytadelle.Core.Upgrades;

/// <summary>Where a gene lives in the UI.</summary>
public enum GeneTab
{
    Attack,
    Defense,
    Utility,
}

/// <summary>How a gene's value reads.</summary>
public enum ValueFmt
{
    Num,
    Pct,
    Mult,
    Meters,
    PerSec,
    Atp,
}

/// <summary>
/// What a gene is called and where it sits. Every number behind it - value, price, cap and unlock
/// cost - comes from <see cref="UpgradeBalance"/>.
/// </summary>
public sealed record UpgradeDef(UpgradeId Id, GeneTab Tab, string Name, string Desc, ValueFmt Fmt)
{
    public UpgradeCurves Curves => UpgradeBalance.Of(Id);

    public int Cap => Curves.Cap;

    public int UnlockDna => Curves.UnlockDna;

    /// <summary>Null = Gene Lab only, no in-culture purchase.</summary>
    public bool BuyableInRun => Curves.Atp is not null;
}

/// <summary>
/// The nineteen genes in catalog order, with the mapping the balance was taken from:
/// Toxicity is Damage, Secretion Rate is Attack Speed, Rupture Chance is Crit Chance, Rupture
/// Damage is Crit Damage, Reach is Range, Membrane is Health, Repair is Health Regen,
/// Resistance % is Defense %, Cell Wall is Defense Absolute, ATP Yield is Cash Bonus,
/// ATP / Cycle is Cash / Wave, ATP Reserve is Starting Cash, DNA / Kill is Coins / Kill,
/// DNA / Cycle is Coins / Wave, Granule Chance and Count are Multishot, Diffusion Chance, Depth
/// and Range are Bounce Shot.
/// </summary>
public static class UpgradeCatalog
{
    public static readonly IReadOnlyList<UpgradeDef> All =
    [
        // Offense
        new(UpgradeId.Damage, GeneTab.Attack, "Toxicity", "Toxin damage per hit", ValueFmt.Num),
        new(UpgradeId.AttackSpeed, GeneTab.Attack, "Secretion Rate", "Toxins fired per second", ValueFmt.PerSec),
        new(UpgradeId.CritChance, GeneTab.Attack, "Rupture Chance", "Chance of a rupturing hit", ValueFmt.Pct),
        new(UpgradeId.CritDamage, GeneTab.Attack, "Rupture Damage", "Rupture damage multiplier", ValueFmt.Mult),
        new(UpgradeId.Range, GeneTab.Attack, "Reach", "Targeting radius", ValueFmt.Meters),
        new(UpgradeId.MultishotChance, GeneTab.Attack, "Granule Chance", "Chance of a granule burst", ValueFmt.Pct),
        new(UpgradeId.MultishotTargets, GeneTab.Attack, "Granule Count", "Pathogens a burst hits at once", ValueFmt.Num),
        new(UpgradeId.BounceChance, GeneTab.Attack, "Diffusion Chance", "Chance a toxin diffuses onward", ValueFmt.Pct),
        new(UpgradeId.BounceTargets, GeneTab.Attack, "Diffusion Depth", "Further pathogens a toxin reaches", ValueFmt.Num),
        new(UpgradeId.BounceRange, GeneTab.Attack, "Diffusion Range", "How far a toxin diffuses", ValueFmt.Meters),
        // Barrier
        new(UpgradeId.Health, GeneTab.Defense, "Membrane", "Max cell integrity", ValueFmt.Num),
        new(UpgradeId.Regen, GeneTab.Defense, "Repair", "Integrity restored per second", ValueFmt.PerSec),
        new(UpgradeId.DefPct, GeneTab.Defense, "Resistance %", "Incoming damage reduction", ValueFmt.Pct),
        new(UpgradeId.DefAbs, GeneTab.Defense, "Cell Wall", "Flat damage blocked per hit", ValueFmt.Num),
        // Metabolism
        new(UpgradeId.AtpBonus, GeneTab.Utility, "ATP Yield", "Multiplier on all ATP earned", ValueFmt.Mult),
        new(UpgradeId.AtpPerCycle, GeneTab.Utility, "ATP / Cycle", "ATP granted at cycle end", ValueFmt.Atp),
        new(UpgradeId.StartAtp, GeneTab.Utility, "ATP Reserve", "ATP available at culture start", ValueFmt.Atp),
        new(UpgradeId.DnaPerKill, GeneTab.Utility, "DNA / Kill", "Multiplier on DNA drops", ValueFmt.Mult),
        new(UpgradeId.DnaPerCycle, GeneTab.Utility, "DNA / Cycle", "DNA granted at cycle end", ValueFmt.Num),
    ];

    private static readonly Dictionary<UpgradeId, UpgradeDef> ById = All.ToDictionary(u => u.Id);

    public static UpgradeDef Def(UpgradeId id) => ById[id];

    public static readonly IReadOnlyList<(GeneTab Tab, string Label)> Tabs =
    [
        (GeneTab.Attack, "Offense"),
        (GeneTab.Defense, "Barrier"),
        (GeneTab.Utility, "Metabolism"),
    ];

    public static IEnumerable<UpgradeDef> InTab(GeneTab tab) => All.Where(u => u.Tab == tab);

    /// <summary>Genes that need no DNA unlock; they count as unlocked from the start.</summary>
    public static readonly int FreeGeneCount = All.Count(u => u.UnlockDna == 0);
}
