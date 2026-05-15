using UnityEngine;

/// <summary>
/// 標識効果のScriptableObject基底クラス
/// </summary>
public abstract class RoadSignEffectAsset : ScriptableObject
{
    /// <summary>
    /// 標識の効果を適用する
    /// </summary>
    /// <param name="_context">標識のクエリコンテキスト</param>
    /// <param name="_evaluation">標識の評価結果</param>
    public abstract void Apply(RoadSignQueryContext _context, RoadSignEvaluation _evaluation);
}