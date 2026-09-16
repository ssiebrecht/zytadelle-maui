namespace Zytadelle.Balancing.Missions;

/// <summary>Only two missions of a group per day, so a day is never five variations of the same grind.</summary>
public enum MissionGroup
{
    Progress,
    Volume,
    Kind,
    Economy,
    Chore,
}
