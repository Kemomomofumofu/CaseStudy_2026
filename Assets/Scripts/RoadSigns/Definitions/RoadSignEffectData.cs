using System;
using UnityEngine;

/// <summary>
/// 道路標識の効果データ
/// </summary>
[Serializable]
public sealed class RoadSignEffectData
{
    [SerializeField] private RoadSignEffectType effectType = RoadSignEffectType.ForceDirection;
    [SerializeField] private TurnDirection direction = TurnDirection.Straight;
    [SerializeField] private float floatValue = 0f;
    [SerializeField] private int intValue = 0;

    public RoadSignEffectType EffectType => effectType;
    public TurnDirection Direction => direction;
    public float FloatValue => floatValue;
    public int IntValue => intValue;
}
