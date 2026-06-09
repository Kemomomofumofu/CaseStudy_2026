using UnityEngine;

/// <summary>
/// 標識の評価結果をプレイヤーの移動状態へ変換する
/// </summary>
public sealed class PlayerSignResolver
{
    /// <summary>
    /// 標識の通行制限を考慮して指定方向へ進めるか判定する
    /// </summary>
    public bool CanMove(RoadSignEvaluation _evaluation, TurnDirection _direction)
    {
        if (_evaluation == null) return true;

        return !_evaluation.IsBlocked(_direction);
    }

    /// <summary>
    /// 標識の加速と速度制限を反映した移動速度を取得する
    /// </summary>
    public float ResolveMoveSpeed(RoadSignEvaluation _evaluation, float _baseSpeed)
    {
        if (_evaluation == null) return _baseSpeed;

        return _evaluation.ResolveMoveSpeed(_baseSpeed);
    }

    /// <summary>
    /// 強制方向やランダム方向を反映した進行方向を取得する
    /// </summary>
    public TurnDirection ResolveTurnDirection(RoadSignEvaluation _evaluation, TurnDirection _defaultDirection)
    {
        if (_evaluation == null) return _defaultDirection;
        if (_evaluation.RoadClosed) return _defaultDirection;

        if (_evaluation.TryGetForcedDirection(out TurnDirection forcedDirection)) return forcedDirection;

        if (_evaluation.RandomDirectionRequested) return GetRandomDirection();

        return _defaultDirection;
    }

    /// <summary>
    /// 標識による一時停止が必要か判定する
    /// </summary>
    public bool RequiresStop(RoadSignEvaluation _evaluation)
    {
        return _evaluation != null && _evaluation.RequiresStop;
    }

    /// <summary>
    /// 標識によって減少する車線数を取得する
    /// </summary>
    public int ResolveLaneReduction(RoadSignEvaluation _evaluation)
    {
        return _evaluation != null ? _evaluation.LaneReductionCount : 0;
    }

    /// <summary>
    /// すべての進行方向からランダムな方向を決定する
    /// </summary>
    private TurnDirection GetRandomDirection()
    {
        int value = Random.Range(0, 4);
        return (TurnDirection)value;
    }
}
