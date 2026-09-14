using Zytadelle.Balancing.Curves;

namespace Zytadelle.Lab.Engine.Sim;

/// <summary>
/// Where a campaign's seeds come from. They are derived from the campaign seed and the culture
/// index, never drawn from a running stream: two strategies that need a different number of
/// cultures must still meet the same world on culture 7, or the paired comparison the whole search
/// rests on falls apart exactly where it is needed.
///
/// The mixing constants are structure, not balance - they identify a hash, they do not tune a game.
/// </summary>
public static class Seeds
{
    /// <summary>One round of splitmix32 - a hash, not a stream.</summary>
    public static uint Mix(uint x)
    {
        unchecked
        {
            var z = x + 0x9e3779b9u;
            z = JsMath.Imul(z ^ (z >> 16), 0x21f0aaadu);
            z = JsMath.Imul(z ^ (z >> 15), 0x735a2d97u);
            return z ^ (z >> 15);
        }
    }

    /// <summary>The seed of the k-th culture of a campaign.</summary>
    public static uint Run(uint campaign, int k) => Mix(unchecked(campaign ^ JsMath.Imul((uint)(k + 1), 0x85ebca6bu)));

    /// <summary>
    /// A block of campaign seeds. Training and held-out sets use different salts, so the seeds the
    /// finalists are re-scored on took no part in the search that produced them.
    /// </summary>
    public static uint[] Set(uint master, uint salt, int block, int count) =>
        Enumerable.Range(0, count)
            .Select(i => Mix(unchecked(master ^ salt ^ JsMath.Imul((uint)(block + 1), 0x6d2b79f5u) ^ JsMath.Imul((uint)(i + 1), 0x85ebca6bu))))
            .ToArray();

    public const uint TrainSalt = 0x2545f491;

    public const uint HoldoutSalt = 0x9e3779b1;
}
