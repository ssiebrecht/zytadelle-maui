using Zytadelle.Balancing;
using Zytadelle.Core.Missions;
using Zytadelle.Core.Upgrades;

namespace Zytadelle.Core.Persistence;

/// <summary>
/// Everything that survives a culture. The field names are the in-game ones; the JSON keys they are
/// written under are the original ones, so a save exported from the browser build loads unchanged
/// (see <see cref="SaveSerializer"/>).
/// </summary>
public sealed class SaveData
{
    public double Dna;

    /// <summary>Permanent Gene Lab levels; the starting levels of every culture.</summary>
    public Levels Lab = new();

    public List<UpgradeId> Unlocked = [];

    /// <summary>Best cycle reached per infection.</summary>
    public Dictionary<int, int> BestCycle = [];

    public int SelectedInfection = 1;

    public int Speed = 1;

    public int TotalRuns;

    /// <summary>Lifetime DNA earned. Not the same as DNA held, and not the same as DNA spent.</summary>
    public double TotalDna;

    public MissionDay? Missions;

    /// <summary>Genes visible in a culture: the free ones plus everything unlocked.</summary>
    public IEnumerable<UpgradeId> UnlockedIds() =>
        UpgradeCatalog.All.Where(u => u.UnlockDna == 0 || Unlocked.Contains(u.Id)).Select(u => u.Id);

    /// <summary>Books a finished culture: banks its DNA and keeps the record if it was one.</summary>
    public void RecordRun(int infection, int cycle, double dna)
    {
        Dna += dna;
        TotalDna += dna;
        TotalRuns += 1;
        BestCycle[infection] = Math.Max(BestCycle.GetValueOrDefault(infection), cycle);
    }

    public int Best(int infection) => BestCycle.GetValueOrDefault(infection);

    public int BestOverall() => BestCycle.Count == 0 ? 0 : BestCycle.Values.Max();
}
