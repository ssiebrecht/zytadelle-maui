namespace Zytadelle.Balancing.Missions;

/// <summary>
/// How a raw target is rounded off. Counting missions round the way every ported number in the game
/// rounds; the two currency missions always round up, so their target can never land below the
/// reference run they were measured from.
/// </summary>
public enum MissionRounding
{
    Js,
    Up,
}
