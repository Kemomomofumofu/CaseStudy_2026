using UnityEngine;

/// <summary>
/// 速度制限の効果を表すクラス
/// </summary>
public class SpeedLimitEffect : ISignEffect
{
    public float LimitSpeed { get; } // 制限速度

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="_limitSpeed">制限速度</param>
    public SpeedLimitEffect(float _limitSpeed)
    {
        LimitSpeed = _limitSpeed;
    }
}