namespace Zytadelle.Balancing;

/// <summary>
/// The tempo knobs of the loop itself. Speed multiplies simulated time, never the frame rate:
/// it only decides how many fixed steps are run per rendered frame.
///
/// Every value here is settable so the Balance Lab can turn it; the game itself never writes one.
/// </summary>
public static class SimulationBalance
{
    /// <summary>
    /// Length of one simulation step. The loop runs whole steps or none. Changing it moves the
    /// balance rather than its resolution: the fire rate is deliberately quantised to the tick.
    /// </summary>
    public static double FixedDt { get; set; } = 1.0 / 60.0;

    /// <summary>Ceiling on steps per frame. 20x at 30 fps needs 40; this leaves headroom.</summary>
    public static int MaxStepsPerFrame { get; set; } = 90;

    /// <summary>Wall-clock delta is clamped to this before it is scaled, so a stall cannot spiral.</summary>
    public static double MaxFrameDelta { get; set; } = 0.25;

    /// <summary>Selectable speed multipliers.</summary>
    public static int[] Speeds { get; set; } = [1, 2, 3, 5, 10, 20];

    /// <summary>Seconds of income tracked for the per-minute readouts.</summary>
    public static int RateWindowSeconds { get; set; } = 60;

    /// <summary>How long a render effect lives before it is culled.</summary>
    public static double FxLifetime { get; set; } = 1.2;

    /// <summary>
    /// Ring size of the effect buffer; the oldest is dropped above it. Effects are read only by
    /// the frame encoder, never by the simulation, so a headless run may set this to 0.
    /// </summary>
    public static int FxRingCap { get; set; } = 200;

    /// <summary>Spawns a single tick may drain at most, so a long stall cannot dump a whole cycle at once.</summary>
    public static int MaxSpawnsPerTick { get; set; } = 10;

    /// <summary>
    /// Upper clamp on a stored best cycle. It is a corrupt-save guard: the daily missions walk every
    /// cycle up to the record, so an absurd number there would hang the day roll.
    /// </summary>
    public static int MaxCycle { get; set; } = 100000;
}
