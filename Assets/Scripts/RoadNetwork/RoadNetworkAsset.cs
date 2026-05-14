using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoadConnection
{
    public Intersection from;
    public List<RoadTarget> to = new List<RoadTarget>();
}

public enum RoadDirectionType
{
    TwoWay,
    OneWay
}

[System.Serializable]
public class RoadTarget
{
    public Intersection intersection;
    public RoadDirectionType directionType = RoadDirectionType.TwoWay;
    public int laneCount = 5;
    public bool allowLeftTurn = true;
    public bool allowRightTurn = true;
    public bool allowStraight = true;
}

[CreateAssetMenu(fileName = "RoadNetwork", menuName = "Road/Network Asset")]
public class RoadNetworkAsset : ScriptableObject
{
    public List<RoadConnection> connections = new List<RoadConnection>();
}
