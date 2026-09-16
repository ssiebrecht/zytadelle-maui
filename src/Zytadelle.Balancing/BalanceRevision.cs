namespace Zytadelle.Balancing;

/// <summary>
/// Bumped whenever a tuning pass writes new numbers into this project.
///
/// Everything that caches a derived number - a curve's running totals, the lifetime-spend prefix
/// sums the save keeps - stamps the revision it was filled at and throws itself away once the stamp
/// no longer matches. Retuning writes into the objects that are already in place, so without a
/// revision the old shape survives inside its own caches and quietly keeps answering.
/// </summary>
public static class BalanceRevision
{
    /// <summary>The stamp every cache compares itself against. A counter, not a tuning value.</summary>
    [BalanceIdentity]
    public static int Current { get; private set; }

    /// <summary>Call once after a tuning pass has written every value it means to write.</summary>
    public static void Bump() => Current++;
}
