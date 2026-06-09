using UnityEngine;

/// <summary>
/// 標識の所有者へ指定方向への進行を強制する
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/ForceDirection", fileName = "Eff_ForceDirection")]
public sealed class ForceDirectionEffectAsset : RoadSignEffectAsset
{
    [SerializeField] private TurnDirection direction = TurnDirection.Straight;

    public TurnDirection Direction => direction;
    public override RoadSignEffectTarget Target => RoadSignEffectTarget.OwnerOnly;

    /// <summary>
    /// 設定された方向を強制進行方向として評価結果へ設定する
    /// </summary>
    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.SetForcedDirection(direction);
    }
}
