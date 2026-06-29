using UnityEngine;

/// <summary>
/// 道路標識を評価するときに使用する対象の状況情報
/// </summary>
public class RoadSignQueryContext
{
    public GameObject Actor;
    public Vector3 WorldPosition;
    public TurnDirection IntendedDirection;
    public float CurrentSpeed;
}
