using UnityEngine;

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
    /// 標識の効果を評価して追加する
    /// </summary>
    /// <param name="_context">評価コンテキスト</param>
    /// <param name="_evaluation">評価結果</param>
    public override void Evaluate(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.AddEffect(new ForceDirectionEffect(forceDirection));
    }

    /// <summary>
    /// 指定方向を変更する
    /// </summary>
    /// <param name="_direction">指定する方向</param>
    public void SetForceDirection(TurnDirection _direction)
    {
        forceDirection = _direction;
    }
}