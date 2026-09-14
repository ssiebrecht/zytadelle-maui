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

    public static double CycleLength => SpawnDuration + Cooldown;
}
