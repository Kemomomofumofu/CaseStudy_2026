using UnityEngine;

/// <summary>
/// 標識の所有者以外へ一時停止を要求する
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/Stop", fileName = "Eff_Stop")]
public sealed class StopEffectAsset: RoadSignEffectAsset
{
    public override RoadSignEffectTarget Target => RoadSignEffectTarget.NonOwnerOnly;

    /// <summary>
    /// 一時停止の要求を評価結果へ設定する
    /// </summary>
    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.RequireStop();
    }
}
