using UnityEngine;

public enum RegionState
{
    Locked,
    Available,
    Cleared
}

public enum RegionDifficulty
{
    Easy,
    Normal,
    Hard,
    Extreme
}

public enum NodeEventType
{
    None,
    Hostile,
    Neutral,
    Recruit,
    Resource,
    BaseCamp,
    Elite,
    Boss
}
