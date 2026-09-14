using Zytadelle.Balancing;
using Zytadelle.Core.Upgrades;

namespace Zytadelle.Core.Persistence;

public enum LabActionKind
{
    Unlock,
    Buy,
    Maxed,
}

/// <summary>What clicking a card in the Gene Lab would do right now.</summary>
public sealed record LabAction(LabActionKind Kind, double Cost, int Level);

public enum LabResult
{
    Unlocked,
    Bought,
    Maxed,
    Poor,
}

/// <summary>
/// The Gene Lab: permanent levels bought with DNA. The price index is the Gene Lab level, unlike
/// the in-culture shop, which counts only what was bought during that culture.
///
/// This is the only place DNA ever leaves a save - there is no refund and no respec - which is what
/// lets <see cref="Spend"/> derive lifetime investment instead of counting it.
/// </summary>
public static class LabPurchase
{
    public static LabAction Action(SaveData s, UpgradeId id)
    {
        var def = UpgradeCatalog.Def(id);
        var level = s.Lab[id];
        if (def.UnlockDna > 0 && !s.Unlocked.Contains(id)) return new LabAction(LabActionKind.Unlock, def.UnlockDna, level);
        if (UpgradeBalance.IsMaxed(id, level)) return new LabAction(LabActionKind.Maxed, 0, level);
        return new LabAction(LabActionKind.Buy, def.Curves.Dna.CostAt(level), level);
    }

    public static LabResult Buy(SaveData s, UpgradeId id)
    {
        var a = Action(s, id);
        if (a.Kind == LabActionKind.Maxed) return LabResult.Maxed;
        if (s.Dna < a.Cost) return LabResult.Poor;

        s.Dna -= a.Cost;
        if (a.Kind == LabActionKind.Unlock)
        {
            s.Unlocked.Add(id);
            return LabResult.Unlocked;
        }

        s.Lab[id] = a.Level + 1;
        return LabResult.Bought;
    }
}
