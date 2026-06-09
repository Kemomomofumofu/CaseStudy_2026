using UnityEngine;

/// <summary>
/// 標識の所有者へ加速効果を与える
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/Acceleration", fileName = "Eff_Acceleration")]
public sealed class AccelerationEffectAsset : RoadSignEffectAsset
{
    [SerializeField] private float deltaSpeed = 2.0f;

    public override RoadSignEffectTarget Target => RoadSignEffectTarget.OwnerOnly;

    /// <summary>
    /// 設定された加速量を標識の評価結果へ追加する
    /// </summary>
    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.AddAcceleration(deltaSpeed);
    }
}
