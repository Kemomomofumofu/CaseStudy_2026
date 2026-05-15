using UnityEngine;

/// <summary>
/// ブロックの方向の効果のアセット
/// </summary>
[CreateAssetMenu(menuName = "RoadSign/Effects/BlockDirection", fileName = "Eff_BlockDirection")]
public sealed class BlockDirectionEffectAsset : RoadSignEffectAsset
{
    [SerializeField] private TurnDirection blockDirection = TurnDirection.Straight;

    public override void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.AddEffect(new BlockDirectionEffect(blockDirection));
    }
}