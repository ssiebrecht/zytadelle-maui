namespace Zytadelle.Balancing.Genes;

/// <summary>All genes in persisted <see cref="GeneId"/> order, each wrapping its static class.</summary>
public static class GeneRegistry
{
    /// <summary>
    /// A view: every number behind it is reached through its own gene class. Marked so the Balance
    /// Lab does not offer the same curve twice.
    /// </summary>
    [BalanceIdentity]
    public static IReadOnlyList<GeneDefinition> All { get; } =
    [
        new(GeneId.Damage, typeof(DamageGene), () => DamageGene.Cap, () => DamageGene.UnlockDna, () => DamageGene.BaseValue, DamageGene.ValueGrowth, DamageGene.Price),
        new(GeneId.AttackSpeed, typeof(AttackSpeedGene), () => AttackSpeedGene.Cap, () => AttackSpeedGene.UnlockDna, () => AttackSpeedGene.BaseValue, AttackSpeedGene.ValueGrowth, AttackSpeedGene.Price),
        new(GeneId.CritChance, typeof(CritChanceGene), () => CritChanceGene.Cap, () => CritChanceGene.UnlockDna, () => CritChanceGene.BaseValue, CritChanceGene.ValueGrowth, CritChanceGene.Price),
        new(GeneId.CritDamage, typeof(CritDamageGene), () => CritDamageGene.Cap, () => CritDamageGene.UnlockDna, () => CritDamageGene.BaseValue, CritDamageGene.ValueGrowth, CritDamageGene.Price),
        new(GeneId.Range, typeof(RangeGene), () => RangeGene.Cap, () => RangeGene.UnlockDna, () => RangeGene.BaseValue, RangeGene.ValueGrowth, RangeGene.Price),
        new(GeneId.MultishotChance, typeof(MultishotChanceGene), () => MultishotChanceGene.Cap, () => MultishotChanceGene.UnlockDna, () => MultishotChanceGene.BaseValue, MultishotChanceGene.ValueGrowth, MultishotChanceGene.Price),
        new(GeneId.MultishotTargets, typeof(MultishotTargetsGene), () => MultishotTargetsGene.Cap, () => MultishotTargetsGene.UnlockDna, () => MultishotTargetsGene.BaseValue, MultishotTargetsGene.ValueGrowth, MultishotTargetsGene.Price),
        new(GeneId.BounceChance, typeof(BounceChanceGene), () => BounceChanceGene.Cap, () => BounceChanceGene.UnlockDna, () => BounceChanceGene.BaseValue, BounceChanceGene.ValueGrowth, BounceChanceGene.Price),
        new(GeneId.BounceTargets, typeof(BounceTargetsGene), () => BounceTargetsGene.Cap, () => BounceTargetsGene.UnlockDna, () => BounceTargetsGene.BaseValue, BounceTargetsGene.ValueGrowth, BounceTargetsGene.Price),
        new(GeneId.BounceRange, typeof(BounceRangeGene), () => BounceRangeGene.Cap, () => BounceRangeGene.UnlockDna, () => BounceRangeGene.BaseValue, BounceRangeGene.ValueGrowth, BounceRangeGene.Price),
        new(GeneId.Health, typeof(HealthGene), () => HealthGene.Cap, () => HealthGene.UnlockDna, () => HealthGene.BaseValue, HealthGene.ValueGrowth, HealthGene.Price),
        new(GeneId.Regen, typeof(RegenGene), () => RegenGene.Cap, () => RegenGene.UnlockDna, () => RegenGene.BaseValue, RegenGene.ValueGrowth, RegenGene.Price),
        new(GeneId.DefPct, typeof(DefPctGene), () => DefPctGene.Cap, () => DefPctGene.UnlockDna, () => DefPctGene.BaseValue, DefPctGene.ValueGrowth, DefPctGene.Price),
        new(GeneId.DefAbs, typeof(DefAbsGene), () => DefAbsGene.Cap, () => DefAbsGene.UnlockDna, () => DefAbsGene.BaseValue, DefAbsGene.ValueGrowth, DefAbsGene.Price),
        new(GeneId.AtpBonus, typeof(AtpBonusGene), () => AtpBonusGene.Cap, () => AtpBonusGene.UnlockDna, () => AtpBonusGene.BaseValue, AtpBonusGene.ValueGrowth, AtpBonusGene.Price),
        new(GeneId.AtpPerCycle, typeof(AtpPerCycleGene), () => AtpPerCycleGene.Cap, () => AtpPerCycleGene.UnlockDna, () => AtpPerCycleGene.BaseValue, AtpPerCycleGene.ValueGrowth, AtpPerCycleGene.Price),
        new(GeneId.StartAtp, typeof(StartAtpGene), () => StartAtpGene.Cap, () => StartAtpGene.UnlockDna, () => StartAtpGene.BaseValue, StartAtpGene.ValueGrowth, StartAtpGene.Price, buyableInRun: false),
        new(GeneId.DnaPerKill, typeof(DnaPerKillGene), () => DnaPerKillGene.Cap, () => DnaPerKillGene.UnlockDna, () => DnaPerKillGene.BaseValue, DnaPerKillGene.ValueGrowth, DnaPerKillGene.Price),
        new(GeneId.DnaPerCycle, typeof(DnaPerCycleGene), () => DnaPerCycleGene.Cap, () => DnaPerCycleGene.UnlockDna, () => DnaPerCycleGene.BaseValue, DnaPerCycleGene.ValueGrowth, DnaPerCycleGene.Price),
    ];

    private static readonly IReadOnlyDictionary<GeneId, GeneDefinition> ById =
        All.ToDictionary(gene => gene.Id);

    public static GeneDefinition Get(GeneId id) => ById[id];

    public static double ValueAt(GeneId id, int level) => Get(id).ValueAt(level);

    public static bool IsMaxed(GeneId id, int level) => Get(id).IsMaxed(level);
}
