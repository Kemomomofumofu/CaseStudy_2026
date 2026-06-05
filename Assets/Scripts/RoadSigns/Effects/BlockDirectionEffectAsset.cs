using UnityEngine;

/// <summary>
/// 特定方向への進行を禁止する標識効果
/// </summary>
[CreateAssetMenu(menuName = "RoadSigns/Effects/BlockDirection", fileName = "Eff_BlockDirection")]
public sealed class BlockDirectionEffectAsset : RoadSignEffectAsset
{
    [SerializeField] private TurnDirection blockedDirection = TurnDirection.Right;

    public override RoadSignEffectTarget Target => RoadSignEffectTarget.NonOwnerOnly;

    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.BlockDirection(blockedDirection);
    }
}
