using UnityEngine;

/// <summary>
/// ランダムな方向を示す道路標識
/// </summary>
public sealed class RandomDirectionSign : RoadSign
{
    /// <summary>
    /// 評価に追加
    /// </summary>
    public override void Evaluate(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.AddEffect(new RandomDirectionEffect());
    }
}
