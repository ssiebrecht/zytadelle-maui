using Zytadelle.Balancing;
using Zytadelle.Core.Persistence;
using Zytadelle.Core.Upgrades;

namespace Zytadelle.Lab.Engine.Sim;

/// <summary>
/// What a build is to the Lab: not a list of levels but the two priorities that produce them - how
/// banked DNA is spent in the Gene Lab between cultures, and how ATP is spent inside one. Caps run
/// to 6000 levels, so the levels themselves cannot be searched; the priorities can, and the levels
/// fall out of them.
///
/// Weights are an array indexed by <see cref="UpgradeId"/>, never a dictionary. Both buying loops
/// break ties with a strictly-greater comparison, so the order the genes are walked in decides who
/// wins a tie - and catalog order is the order the game itself would have offered them in.
/// </summary>
/// <param name="LabWeights">DNA priority per gene. 0 means never buy. Only the ratios matter.</param>
/// <param name="RunWeights">ATP priority per gene during a culture.</param>
/// <param name="DamageGate">Hold ATP for Toxicity while a basic of the next cycle still survives a hit.</param>
/// <param name="UnlockK">Levels bought in one go when a locked gene is first unlocked.</param>
public sealed record Strategy(
    double[] LabWeights,
    double[] RunWeights,
    bool DamageGate,
    int UnlockK)
{
    public static double[] Uniform() => Enumerable.Repeat(1.0, UpgradeIds.Count).ToArray();

    public static Strategy Flat(bool damageGate = true, int unlockK = 5) =>
        new(Uniform(), Uniform(), damageGate, unlockK);

    public double Lab(UpgradeId id) => LabWeights[(int)id];

    public double Run(UpgradeId id) => RunWeights[(int)id];

    /// <summary>
    /// Identity for the score cache. The weights only matter to a few digits - two builds that
    /// agree to three decimals will meet the same cultures and are not worth simulating twice.
    /// </summary>
    public string Key() => $"{Join(LabWeights)}|{Join(RunWeights)}|{(DamageGate ? 1 : 0)}|{UnlockK}";

    private static string Join(double[] w) =>
        string.Join(',', w.Select(x => x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)));
}

/// <summary>Spending the bank between cultures.</summary>
public static class LabAllocator
{
    /// <summary>Every purchase costs at least 1 DNA, so this only ever catches an absurd budget.</summary>
    private const int MaxBuys = 200_000;

    /// <summary>
    /// The fractional gain one more level buys. A raw weight-per-DNA ratio is unusable as a prior
    /// because the genes are not in the same units - Toxicity gains +2.87 on a base of 3 for 30 DNA
    /// while ATP Yield gains +0.01 on a base of 1 for the same money. Dividing by the current value
    /// makes the numerator dimensionless, so a weight of 1 everywhere is a sane starting point and
    /// the search only has to learn the deviations.
    ///
    /// Genes that start at zero - Repair, Cell Wall, the two spread chances - would divide by
    /// nothing, so their first level is measured against itself: a gain of 1.0, this level is the
    /// whole stat.
    /// </summary>
    public static double RelGain(UpgradeId id, int level)
    {
        var here = UpgradeBalance.ValueAt(id, level);
        var next = UpgradeBalance.ValueAt(id, level + 1);
        return (next - here) / (here > 0 ? here : Math.Max(next, 1e-9));
    }

    /// <summary>Unlock price plus the first k levels - what entering a locked gene actually costs.</summary>
    private static double PackageCost(UpgradeId id, int k)
    {
        var c = UpgradeBalance.Of(id);
        var sum = (double)c.UnlockDna;
        for (var i = 0; i < k; i++) sum += c.Dna.CostAt(i);
        return sum;
    }

    /// <summary>
    /// Spends everything in the bank, one level at a time, always on the best fractional gain per
    /// DNA.
    ///
    /// A locked gene is scored on its whole entry package - the unlock plus <c>unlockK</c> levels -
    /// amortised over those levels, and may only win when the package is affordable in full. Then it
    /// is bought in full, before the loop looks at anything else. Without that commitment the unlock
    /// is paid, the ratio immediately falls back behind the incumbent, and the gene is stranded at
    /// level 0 forever: a pure loss the search would learn to avoid by never unlocking anything.
    /// </summary>
    public static void Allocate(SaveData save, double[] weights, int unlockK)
    {
        var k = Math.Max(1, unlockK);

        for (var guard = 0; guard < MaxBuys; guard++)
        {
            UpgradeId? pick = null;
            var pickScore = 0.0;
            var pickUnlock = false;

            foreach (var id in UpgradeIds.All)
            {
                var w = weights[(int)id];
                if (!(w > 0)) continue;

                var action = LabPurchase.Action(save, id);
                if (action.Kind == LabActionKind.Maxed) continue;

                double score;
                if (action.Kind == LabActionKind.Unlock)
                {
                    var package = PackageCost(id, k);
                    if (save.Dna < package) continue;
                    score = w * RelGain(id, 0) / (package / k);
                }
                else
                {
                    if (save.Dna < action.Cost) continue;
                    score = w * RelGain(id, action.Level) / action.Cost;
                }

                if (score <= pickScore) continue;
                pickScore = score;
                pick = id;
                pickUnlock = action.Kind == LabActionKind.Unlock;
            }

            if (pick is not { } chosen) return;
            LabPurchase.Buy(save, chosen);
            if (pickUnlock)
                for (var i = 0; i < k; i++)
                    LabPurchase.Buy(save, chosen);
        }
    }
}
