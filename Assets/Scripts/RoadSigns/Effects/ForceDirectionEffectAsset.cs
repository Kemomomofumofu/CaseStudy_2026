using UnityEngine;

/// <summary>
/// 進行方向を強制する標識効果
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/ForceDirection", fileName = "Eff_ForceDirection")]
public sealed class ForceDirectionEffectAsset : RoadSignEffectAsset
{
    [SerializeField] private TurnDirection direction = TurnDirection.Straight;

    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.SetForcedDirection(direction);
    }
}