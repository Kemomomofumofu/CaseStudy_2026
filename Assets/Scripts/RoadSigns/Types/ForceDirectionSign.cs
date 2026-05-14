using UnityEngine;
using UnityEngine.InputSystem.Utilities;

/// <summary>
/// 進行方向指定標識
/// デフォルトは直進
/// </summary>
public sealed class ForceDirectionSign : RoadSignBase
{
    [Header("進行指示設定")]
    [Tooltip("指定する方向")]
    [SerializeField] private TurnDirection forceDirection = TurnDirection.Straight;

    /// <summary>
    /// 評価に追加
    /// </summary>
    public override void Evaluate(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.AddEffect(new ForceDirectionEffect(forceDirection));
    }

    /// <summary>
    /// 指定方向を設定する
    /// </summary>
    public void Configure(TurnDirection _direction)
    {
        forceDirection = _direction;
    }
}