using System;


/// <summary>
/// 標識のルール評価の結果をまとめるクラス
/// </summary>
public sealed class RoadSignDecision
{
    public bool CanProceed { get; private set; } = true;            // 進行可能かどうか
    public float MaxSpeed { get; private set; } = float.MaxValue;   // 制限速度
    public string Reason { get; private set; } = string.Empty;      // ルールに違反している場合の理由

    /// <summary>
    /// このルールに違反していると判断された場合に呼び出されるメソッド
    /// </summary>
    /// <param name="_reason">違反理由</param>
    public void Block(string _reason)
    {
        CanProceed = false;
        Reason = _reason;
    }

    public void LimitSpeed(float _speed)
    {
        MaxSpeed = MathF.Min(MaxSpeed, _speed);
    }
}