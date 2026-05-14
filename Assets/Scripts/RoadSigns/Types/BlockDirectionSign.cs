using UnityEngine;

/// <summary>
/// 右折禁止標識
/// </summary>
public sealed class BlockDirectionSign : RoadSignBase
{
    [Header("進行禁止設定")]
    [Tooltip("禁止する方向")]
    [SerializeField] private TurnDirection blockDirection = TurnDirection.Right;

    /// <summary>
    /// 評価に進行禁止効果を追加する
    /// </summary>
    public override void Evaluate(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.AddEffect(new BlockDirectionEffect(blockDirection));
    }

    /// <summary>
    /// 禁止する方向を設定する
    /// </summary>
    public void Configure(TurnDirection _direction)
    {
        blockDirection = _direction;
    }

}
