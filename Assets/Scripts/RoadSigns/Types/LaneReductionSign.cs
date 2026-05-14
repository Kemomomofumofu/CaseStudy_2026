using UnityEngine;

/// <summary>
/// 車線減少を示す道路標識
/// </summary>
public sealed class LaneReductionSign : RoadSignBase
{
    [Tooltip("減少する車線の数")]
    [SerializeField] private int reduceCount = 1;

    /// <summary>
    /// 評価に追加
    /// </summary>
    public override void Evaluate(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.AddEffect(new LaneReductionEffect(reduceCount));
    }

    /// <summary>
    /// 減少する車線の数を設定
    /// </summary>
    public void Configure(int _reduceCount)
    {
        reduceCount = Mathf.Max(1, _reduceCount);
    }
}
