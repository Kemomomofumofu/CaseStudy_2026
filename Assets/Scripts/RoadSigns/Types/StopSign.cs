using UnityEngine;

/// <summary>
/// 一時停止を示す道路標識
/// </summary>
public sealed class StopSign : RoadSign
{
    /// <summary>
    /// 評価に追加
    /// </summary>
    public override void Evaluate(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.AddEffect(new StopEffect());
    }
}
