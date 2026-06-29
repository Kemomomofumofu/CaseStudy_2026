using UnityEngine;

/// <summary>
/// 標識の所有者以外にランダムな進行方向を要求する
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/RandomDirection", fileName = "Eff_RandomDirection")]
public sealed class RandomDirectionEffectAsset : RoadSignEffectAsset
{
    public override RoadSignEffectTarget Target => RoadSignEffectTarget.NonOwnerOnly;

    /// <summary>
    /// ランダム方向への進行要求を評価結果へ設定する
    /// </summary>
    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.RequestRandomDirection();
    }
}
