using UnityEngine;

/// <summary>
/// 車線数を減少させる標識効果
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/LaneReduction", fileName = "Eff_LaneReduction")]
public sealed class LaneReductionEffectAsset : RoadSignEffectAsset
{
    [SerializeField] private int reduceCount = 1;

    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.SetLaneReduction(reduceCount);
    }
}