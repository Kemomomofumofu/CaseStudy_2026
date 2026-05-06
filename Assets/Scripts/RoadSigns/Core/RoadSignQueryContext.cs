using UnityEngine;

/// <summary>
/// 標識のルールを評価する際のコンテキスト情報をまとめたクラス
/// 例： Actorが大型車なら...といった具合で物によって評価を変えれるようにするのに必要
/// </summary>
public class RoadSignQueryContext
{
    public GameObject Actor;
    public Vector3 WorldPosition;
    public TurnDirection IntendedDirection;
    public float CurrentSpeed;
}
