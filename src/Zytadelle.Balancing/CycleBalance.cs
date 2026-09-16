namespace Zytadelle.Balancing;

/// <summary>Cycle timing. A culture ends only when the cell dies, never on a timer.</summary>
public static class CycleBalance
{
    /// <summary>Seconds the spawn phase of one cycle lasts.</summary>
    public static double SpawnDuration { get; set; } = 26;

    /// <summary>Quiet seconds after spawning.</summary>
    public static double Cooldown { get; set; } = 9;

    /// <summary>A boss every n-th cycle.</summary>
    public static int BossEvery { get; set; } = 10;

    /// <summary>
    /// Where inside the first spawn interval the first pathogen of a cycle appears, as a fraction
    /// of that interval. Half centres the spawn train in the phase instead of front-loading it.
    /// </summary>
    public static double SpawnTrainOffset { get; set; } = 0.5;

    public static double CycleLength => SpawnDuration + Cooldown;

    /// <summary>Whether the cycle brings a boss. Cycle 0 never does.</summary>
    public static bool IsBossCycle(int cycle) => cycle > 0 && cycle % BossEvery == 0;
}
