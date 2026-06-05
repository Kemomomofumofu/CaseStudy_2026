using UnityEngine;

/// <summary>
/// 一時停止を要求する標識効果
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/Stop", fileName = "Eff_Stop")]
public sealed class StopEffectAsset: RoadSignEffectAsset
{
    public override RoadSignEffectTarget Target => RoadSignEffectTarget.NonOwnerOnly;

    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.RequireStop();
    }
}
