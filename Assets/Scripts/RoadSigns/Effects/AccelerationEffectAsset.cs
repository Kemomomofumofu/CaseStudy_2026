using UnityEngine;

/// <summary>
/// 移動速度を増加させる標識効果
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/Acceleration", fileName = "Eff_Acceleration")]
public sealed class AccelerationEffectAsset : RoadSignEffectAsset
{
    [SerializeField] private float deltaSpeed = 2.0f;

    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.AddAcceleration(deltaSpeed);
    }
}