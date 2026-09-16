namespace Zytadelle.Balancing.Missions;

/// <summary>
/// One mission as a row of a table: which quantity of the reference run it measures, how much of it
/// it asks for, and the floor below which the ask never drops.
///
/// <see cref="Basis"/> only picks the quantity and carries no number of its own - every number a
/// mission has is a column here, which is what puts them in front of the Balance Lab. The gates in
/// <see cref="Eligible"/> read their thresholds from <see cref="MissionBalance"/> for the same reason.
/// </summary>
/// <param name="Id">Which mission this is. Identity, not balance.</param>
/// <param name="Group">The group its daily cap is counted against.</param>
/// <param name="Scope">Whether the day adds every culture up or keeps the single best one.</param>
/// <param name="Basis">Which quantity of the reference run the target is a multiple of.</param>
/// <param name="Mult">How many times that quantity a day asks for.</param>
/// <param name="Floor">The target never drops below this, however small the reference run is.</param>
/// <param name="Rounding">How the raw multiple is turned into a whole target.</param>
/// <param name="Eligible">Null = always offered. Otherwise the reference run has to pass this first.</param>
public sealed record MissionRule(
    MissionId Id,
    MissionGroup Group,
    MissionScope Scope,
    Func<MissionReference, double> Basis,
    double Mult,
    double Floor = 0,
    MissionRounding Rounding = MissionRounding.Js,
    Func<MissionReference, bool>? Eligible = null)
{
    /// <summary>The whole target this mission asks of the given reference run.</summary>
    public double Target(MissionReference r)
    {
        var raw = Mult * Basis(r);
        var whole = Rounding == MissionRounding.Up ? Math.Ceiling(raw) : JsMath.Round(raw);
        return Math.Max(Floor, whole);
    }
}
