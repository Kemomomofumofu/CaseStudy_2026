using UnityEngine;

/// <summary>
/// 制限速度を示す道路標識
/// </summary>
public class SpeedLimitSign : RoadSignBase
{
    [SerializeField]
    [Tooltip("制限速度")]
    private float limitSpeed = 10.0f;

    /// <summary>
    /// 評価に追加
    /// </summary>
    public override void Evaluate(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.AddEffect(new SpeedLimitEffect(limitSpeed));
    }

    /// <summary>
    /// 制限速度を設定する
    /// </summary>
    /// <param name="_limitSpeed"></param>
    public void Configure(float _limitSpeed)
    {
        limitSpeed = _limitSpeed;
    }
}

