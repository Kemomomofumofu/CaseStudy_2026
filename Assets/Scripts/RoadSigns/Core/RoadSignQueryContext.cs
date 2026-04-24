using UnityEngine;

/// <summary>
/// 標識のルールを評価する際のコンテキスト情報をまとめたクラス
/// </summary>
public class RoadSignQueryContext
{
    public GameObject Actor;
    public Vector3 WorldPosition;
    public TurnDirection IntendedDirection;
    public float CurrentSpeed;
}
