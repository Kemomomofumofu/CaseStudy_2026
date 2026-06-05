using UnityEngine;

/// <summary>
/// 通行止めにする標識効果
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/RoadClosed", fileName = "Eff_RoadClosed")]
public sealed class RoadClosedEffectAsset : RoadSignEffectAsset
{
    public override RoadSignEffectTarget Target => RoadSignEffectTarget.NonOwnerOnly;

    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.CloseRoad();
    }
}
