using UnityEngine;

public enum RoadSignEffectTarget
{
    Everyone,
    OwnerOnly,
    NonOwnerOnly
}

/// <summary>
/// 標識効果のScriptableObject基底クラス
/// </summary>
public abstract class RoadSignEffectAsset : ScriptableObject
{
    public virtual RoadSignEffectTarget Target => RoadSignEffectTarget.Everyone;

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
    /// 標識の効果を適用する
    /// </summary>
    /// <param name="_context">標識のクエリコンテキスト</param>
    /// <param name="_evaluation">標識の評価結果</param>
    public abstract void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation);
}
