using UnityEngine;

/// <summary>
/// 標識の所有者以外に特定方向への進行禁止効果を与える
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/BlockDirection", fileName = "Eff_BlockDirection")]
public sealed class BlockDirectionEffectAsset : RoadSignEffectAsset
{
    [SerializeField] private TurnDirection blockedDirection = TurnDirection.Right;

    public override RoadSignEffectTarget Target => RoadSignEffectTarget.NonOwnerOnly;

    /// <summary>
    /// 設定された方向を進行禁止として評価結果へ追加する
    /// </summary>
    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.BlockDirection(blockedDirection);
    }
}
