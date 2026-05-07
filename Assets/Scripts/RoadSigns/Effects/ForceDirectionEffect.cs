using UnityEngine;

/// <summary>
/// 進行方向の指定効果
/// </summary>
public sealed class ForceDirectionEffect : ISignEffect
{
    public TurnDirection Direction { get; }

    /// <summary>
    /// 方向指定の効果を生成する
    /// </summary>
    /// <param name="_direction">指定する方向</param>
    public ForceDirectionEffect(TurnDirection _direction)
    {
        Direction = _direction;
    }
}
