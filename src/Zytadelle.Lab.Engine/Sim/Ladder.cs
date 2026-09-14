namespace Zytadelle.Lab.Engine.Sim;

/// <summary>
/// One rung of the ladder as the table shows it: the record that was set, and what the jump from
/// the previous record cost in cultures and in minutes.
/// </summary>
/// <param name="Cycle">The new best cycle.</param>
/// <param name="Runs">Cultures spent since the campaign started.</param>
/// <param name="RunsSincePrevious">Cultures this jump took.</param>
/// <param name="SimMinutes">Simulated minutes spent in cultures since the campaign started.</param>
/// <param name="SimMinutesSincePrevious">Simulated minutes this jump took.</param>
/// <param name="RealMinutes">The same clock divided by the speed the player would be running at.</param>
/// <param name="RealMinutesSincePrevious">Real minutes this jump took at that speed.</param>
public sealed record LadderRow(
    int Cycle,
    int Runs,
    int RunsSincePrevious,
    double SimMinutes,
    double SimMinutesSincePrevious,
    double RealMinutes,
    double RealMinutesSincePrevious);

public static class Ladder
{
    /// <summary>
    /// Turns the milestones a campaign recorded into rows, adding the difference to the previous
    /// rung - which is the question the tool is here to answer, and the one number a cumulative
    /// series does not show.
    ///
    /// Both clocks come off the same simulated seconds. Speed multiplies simulated time per frame
    /// rather than the frame rate, so real time is simply that divided by the speed - as long as the
    /// machine can hold the step budget. The clock also counts time inside cultures only: no
    /// shopping, no menus, no idling in the hub.
    /// </summary>
    public static IReadOnlyList<LadderRow> Rows(IReadOnlyList<Milestone> ladder, int speed)
    {
        var rows = new List<LadderRow>(ladder.Count);
        var prevRuns = 0;
        var prevSeconds = 0.0;
        var divisor = Math.Max(1, speed);

        foreach (var m in ladder)
        {
            var sim = m.Seconds / 60;
            var simStep = (m.Seconds - prevSeconds) / 60;
            rows.Add(new LadderRow(
                m.Cycle,
                m.Runs,
                m.Runs - prevRuns,
                sim,
                simStep,
                sim / divisor,
                simStep / divisor));
            prevRuns = m.Runs;
            prevSeconds = m.Seconds;
        }

        return rows;
    }
}
