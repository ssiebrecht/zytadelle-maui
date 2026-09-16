namespace Zytadelle.Core.Persistence;

/// <summary>
/// The local calendar day and the seed derived from it. Local rather than UTC because the missions
/// reset at the player's own midnight, and the format sorts lexicographically, which is all the
/// comparison this needs.
/// </summary>
public static class DayKey
{
    public static string Today(DateTime? now = null) => (now ?? DateTime.Now).ToString("yyyy-MM-dd");

    /// <summary>
    /// Stable 32-bit seed from a day key (FNV-1a over the UTF-16 units), so a day always rolls the
    /// same five missions on every machine.
    /// </summary>
    public static uint Seed(string key)
    {
        var h = 0x811c9dc5u;
        foreach (var c in key)
        {
            h ^= c;
            h = JsMath.Imul(h, 0x01000193u);
        }
        return h;
    }
}
