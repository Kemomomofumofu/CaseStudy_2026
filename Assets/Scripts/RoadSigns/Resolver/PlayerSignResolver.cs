using UnityEngine;

public sealed class PlayerSignResolver
{
    /// <summary>
    /// 指定方向に進めるか判定する
    /// </summary>
    public bool CanMove(RoadSignEvaluation _evaluation, TurnDirection _direction)
    {
        if (_evaluation == null) return true;

        return !_evaluation.IsBlocked(_direction);
    }

    /// <summary>
    /// 標識評価後の移動速度を返す
    /// </summary>
    public float ResolveMoveSpeed(RoadSignEvaluation _evaluation, float _baseSpeed)
    {
        if (_evaluation == null) return _baseSpeed;

        return _evaluation.ResolveMoveSpeed(_baseSpeed);
    }

    /// <summary>
    /// 標識評価後の進行方向を返す
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
    /// 一時停止が必要か判定する
    /// </summary>
    public bool RequiresStop(RoadSignEvaluation _evaluation)
    {
        return _evaluation != null && _evaluation.RequiresStop;
    }

    /// <summary>
    /// 車線減少数を返す
    /// </summary>
    public int ResolveLaneReduction(RoadSignEvaluation _evaluation)
    {
        return _evaluation != null ? _evaluation.LaneReductionCount : 0;
    }

    /// <summary>
    /// ランダム方向を決定する
    /// </summary>
    private TurnDirection GetRandomDirection()
    {
        int value = Random.Range(0, 4);
        return (TurnDirection)value;
    }
}
