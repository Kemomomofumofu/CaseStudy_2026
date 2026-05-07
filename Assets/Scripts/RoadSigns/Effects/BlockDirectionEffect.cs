using UnityEngine.EventSystems;

/// <summary>
/// 特定の方向への移動をブロックする
/// </summary>
public sealed class BlockDirectionEffect : ISignEffect
{
    public TurnDirection Direction { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="_direction">ブロックする方向</param>
    public BlockDirectionEffect(TurnDirection _direction)
    {
        Direction = _direction;
    }
}
