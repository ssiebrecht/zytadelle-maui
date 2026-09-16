
namespace Zytadelle.Core.Progression;

/// <summary>
/// What the save's per-infection records unlock. The thresholds are numbers in
/// <c>Zytadelle.Balancing</c>; the rules that read a record against them live here, because a
/// record is save data and the balance project does not know the save.
/// </summary>
public static class InfectionProgress
{
    /// <summary>The first infection is always open; each later one needs the previous record past the unlock cycle.</summary>
    public static bool IsUnlocked(int infection, IReadOnlyDictionary<int, int> bestCycle) =>
        infection <= 1
        || (bestCycle.TryGetValue(infection - 1, out var best) ? best : 0) >= InfectionBalance.UnlockCycle;

    /// <summary>Highest infection the player has opened.</summary>
    public static int HighestUnlocked(IReadOnlyDictionary<int, int> bestCycle)
    {
        var hi = 1;
        foreach (var t in InfectionBalance.All)
            if (IsUnlocked(t.Infection, bestCycle))
                hi = t.Infection;
        return hi;
    }

    /// <summary>
    /// The cycle the day's mission targets are measured against: the best cycle of the highest
    /// unlocked infection, with the previous one carrying <see cref="MissionBalance.AnchorPrevWeight"/>
    /// of the weight, never below <see cref="MissionBalance.AnchorFloor"/>.
    /// </summary>
    public static int AnchorCycle(IReadOnlyDictionary<int, int> bestCycle)
    {
        var hi = HighestUnlocked(bestCycle);
        var prev = bestCycle.TryGetValue(hi - 1, out var p) ? p : 0;
        var best = bestCycle.TryGetValue(hi, out var b) ? b : 0;
        return (int)Math.Max(MissionBalance.AnchorFloor, Math.Max(best, JsMath.Round(MissionBalance.AnchorPrevWeight * prev)));
    }
}
