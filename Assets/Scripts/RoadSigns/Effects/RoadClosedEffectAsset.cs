using UnityEngine;

/// <summary>
/// 標識の所有者以外へ通行止め効果を与える
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/RoadClosed", fileName = "Eff_RoadClosed")]
public sealed class RoadClosedEffectAsset : RoadSignEffectAsset
{
    public override RoadSignEffectTarget Target => RoadSignEffectTarget.NonOwnerOnly;

    /// <summary>
    /// 道路を通行不能として評価結果へ設定する
    /// </summary>
    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.CloseRoad();
    }
}
