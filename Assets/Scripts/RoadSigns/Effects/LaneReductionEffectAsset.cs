using UnityEngine;

/// <summary>
/// 標識の所有者以外に車線減少効果を与える
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/LaneReduction", fileName = "Eff_LaneReduction")]
public sealed class LaneReductionEffectAsset : RoadSignEffectAsset
{
    [SerializeField] private int reduceCount = 1;

    public override RoadSignEffectTarget Target => RoadSignEffectTarget.NonOwnerOnly;

    /// <summary>
    /// 設定された車線減少数を評価結果へ設定する
    /// </summary>
    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.SetLaneReduction(reduceCount);
    }
}
