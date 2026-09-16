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
/// cost - comes from the gene definition in <see cref="GeneRegistry"/>.
/// </summary>
public sealed record UpgradeDef(GeneId Id, GeneTab Tab, string Name, string Desc, ValueFmt Fmt)
{
    public GeneDefinition Gene => GeneRegistry.Get(Id);

    public int Cap => Gene.Cap;

    public int UnlockDna => Gene.UnlockDna;

    public bool BuyableInRun => Gene.BuyableInRun;
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
        new(GeneId.Damage, GeneTab.Attack, "Toxicity", "Toxin damage per hit", ValueFmt.Num),
        new(GeneId.AttackSpeed, GeneTab.Attack, "Secretion Rate", "Multiplier on the cell's base fire rate", ValueFmt.Mult),
        new(GeneId.CritChance, GeneTab.Attack, "Rupture Chance", "Chance of a rupturing hit", ValueFmt.Pct),
        new(GeneId.CritDamage, GeneTab.Attack, "Rupture Damage", "Rupture damage multiplier", ValueFmt.Mult),
        new(GeneId.Range, GeneTab.Attack, "Reach", "Targeting radius", ValueFmt.Meters),
        new(GeneId.MultishotChance, GeneTab.Attack, "Granule Chance", "Chance of a granule burst", ValueFmt.Pct),
        new(GeneId.MultishotTargets, GeneTab.Attack, "Granule Count", "Pathogens a burst hits at once", ValueFmt.Num),
        new(GeneId.BounceChance, GeneTab.Attack, "Diffusion Chance", "Chance a toxin diffuses onward", ValueFmt.Pct),
        new(GeneId.BounceTargets, GeneTab.Attack, "Diffusion Depth", "Further pathogens a toxin reaches", ValueFmt.Num),
        new(GeneId.BounceRange, GeneTab.Attack, "Diffusion Range", "How far a toxin diffuses", ValueFmt.Meters),
        // Barrier
        new(GeneId.Health, GeneTab.Defense, "Membrane", "Max cell integrity", ValueFmt.Num),
        new(GeneId.Regen, GeneTab.Defense, "Repair", "Integrity restored per second", ValueFmt.PerSec),
        new(GeneId.DefPct, GeneTab.Defense, "Resistance %", "Incoming damage reduction", ValueFmt.Pct),
        new(GeneId.DefAbs, GeneTab.Defense, "Cell Wall", "Flat damage blocked per hit", ValueFmt.Num),
        // Metabolism
        new(GeneId.AtpBonus, GeneTab.Utility, "ATP Yield", "Multiplier on all ATP earned", ValueFmt.Mult),
        new(GeneId.AtpPerCycle, GeneTab.Utility, "ATP / Cycle", "ATP granted at cycle end", ValueFmt.Atp),
        new(GeneId.StartAtp, GeneTab.Utility, "ATP Reserve", "ATP available at culture start", ValueFmt.Atp),
        new(GeneId.DnaPerKill, GeneTab.Utility, "DNA / Kill", "Multiplier on DNA drops", ValueFmt.Mult),
        new(GeneId.DnaPerCycle, GeneTab.Utility, "DNA / Cycle", "DNA granted at cycle end", ValueFmt.Num),
    ];

    private static readonly Dictionary<GeneId, UpgradeDef> ById = All.ToDictionary(u => u.Id);

    public static UpgradeDef Def(GeneId id) => ById[id];

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
