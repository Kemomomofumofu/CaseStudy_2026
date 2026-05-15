using UnityEngine;

/// <summary>
/// 標識を評価するときに渡す状況情報
/// </summary>
public class RoadSignQueryContext
{
    public GameObject Actor;
    public Vector3 WorldPosition;
    public TurnDirection IntendedDirection;
    public float CurrentSpeed;
}
