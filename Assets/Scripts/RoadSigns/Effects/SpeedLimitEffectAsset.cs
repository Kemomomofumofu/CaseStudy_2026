using UnityEngine;

/// <summary>
/// 標識の所有者以外へ速度制限効果を与える
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/SpeedLimit", fileName = "Eff_SpeedLimit")]
public sealed class SpeedLimitEffectAsset : RoadSignEffectAsset
{
    [SerializeField] private float limitSpeed = 10.0f;

    public override RoadSignEffectTarget Target => RoadSignEffectTarget.NonOwnerOnly;

    /// <summary>
    /// 設定された制限速度を評価結果へ設定する
    /// </summary>
    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.SetSpeedLimit(limitSpeed);
    }
}
