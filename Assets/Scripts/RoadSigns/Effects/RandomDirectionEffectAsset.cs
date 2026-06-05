using UnityEngine;

/// <summary>
/// ランダムな進行方向を要求する標識効果
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/RandomDirection", fileName = "Eff_RandomDirection")]
public sealed class RandomDirectionEffectAsset : RoadSignEffectAsset
{
    public override RoadSignEffectTarget Target => RoadSignEffectTarget.NonOwnerOnly;

    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.RequestRandomDirection();
    }
}
