using UnityEngine;

/// <summary>
/// 速度制限の効果のアセット
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/SpeedLimit", fileName = "Eff_SpeedLimit")]
public sealed class SpeedLimitEffectAsset : RoadSignEffectAsset
{
    [SerializeField] private float limitSpeed = 10.0f;

    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.SetSpeedLimit(limitSpeed);
    }
}
