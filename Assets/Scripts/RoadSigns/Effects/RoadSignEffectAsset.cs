using UnityEngine;

public enum RoadSignEffectTarget
{
    Everyone,
    OwnerOnly,
    NonOwnerOnly
}

/// <summary>
/// 標識効果の適用対象と評価処理を定義する基底クラス
/// </summary>
public abstract class RoadSignEffectAsset : ScriptableObject
{
    public virtual RoadSignEffectTarget Target => RoadSignEffectTarget.Everyone;

    /// <summary>
    /// 効果の対象設定と標識の所有者から適用可能か判定する
    /// </summary>
    public bool CanApplyTo(GameObject _actor, GameObject _owner)
    {
        if (_owner == null) return true;
        if (_actor == null) return Target == RoadSignEffectTarget.Everyone;

        bool isOwner = _actor == _owner;
        return Target switch
        {
            RoadSignEffectTarget.OwnerOnly => isOwner,
            RoadSignEffectTarget.NonOwnerOnly => !isOwner,
            _ => true
        };
    }

    /// <summary>
    /// 標識の効果を評価結果へ適用する
    /// </summary>
    public abstract void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation);
}
