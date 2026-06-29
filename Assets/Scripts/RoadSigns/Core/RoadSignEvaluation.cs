using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 標識のルール評価の結果を集めるクラス
/// </summary>
public sealed class RoadSignEvaluation
{
    private readonly HashSet<TurnDirection> blockedDirections = new();

    private bool hasSpeedLimit = false;
    private float speedLimit = 0.0f;

    private bool hasForcedDirection = false;
    private TurnDirection forcedDirection = TurnDirection.Straight;

    private bool requiresStop = false;
    private bool roadClosed = false;

    private bool randomDirectionRequested = false;

    private float accelerationDelta = 0.0f;
    private int laneReductionCount = 0;

    public bool RequiresStop => requiresStop;
    public bool RoadClosed => roadClosed;
    public bool RandomDirectionRequested => randomDirectionRequested;
    public float AccelerationDelta => accelerationDelta;
    public int LaneReductionCount => laneReductionCount;

    /// <summary>
    /// この方向への進行を禁止する
    /// </summary>
    public void BlockDirection(TurnDirection _direction)
    {
        blockedDirections.Add(_direction);
    }

    /// <summary>
    /// 指定方向への進行が禁止されているか確認する
    /// </summary>
    public bool IsBlocked(TurnDirection _direction)
    {
        return roadClosed || blockedDirections.Contains(_direction);
    }

    /// <summary>
    /// 速度制限を設定する
    /// </summary>
    public void SetSpeedLimit(float _limit)
    {
        if (_limit <= 0.0f) return;

        if(!hasSpeedLimit || _limit < speedLimit)
        {
            hasSpeedLimit = true;
            speedLimit = _limit;
        }
    }

    /// <summary>
    /// 加速と速度制限を反映した最終移動速度を計算する
    /// </summary>
    public float ResolveMoveSpeed(float _baseSpeed)
    {
        float result = _baseSpeed + accelerationDelta;

        if (hasSpeedLimit)
        {
            return Mathf.Min(result, speedLimit);
        }
        return Mathf.Max(0.0f, result);
    }

    /// <summary>
    /// 強制する進行方向を設定する
    /// </summary>
    public void SetForcedDirection(TurnDirection _direction)
    {
        hasForcedDirection = true;
        forcedDirection = _direction;
    }

    /// <summary>
    /// 強制進行方向が設定されているか確認して取得する
    /// </summary>
    public bool TryGetForcedDirection(out TurnDirection _direction)
    {
        _direction = forcedDirection;
        return hasForcedDirection;
    }

    /// <summary>
    /// 停止を要求する
    /// </summary>
    public void RequireStop()
    {
        requiresStop = true;
    }

    /// <summary>
    /// 道路を閉鎖する
    /// </summary>
    public void CloseRoad()
    {
        roadClosed = true;
    }
    
    /// <summary>
    /// 加速度を追加する
    /// </summary>
    public void AddAcceleration(float _deltaSpeed)
    {
        accelerationDelta += _deltaSpeed;
    }

    /// <summary>
    /// ランダムな方向への進行を要求する
    /// </summary>
    public void RequestRandomDirection()
    {
        randomDirectionRequested = true;
    }

    /// <summary>
    /// 適用する車線減少数を設定する
    /// </summary>
    public void SetLaneReduction(int _reductionCount)
    {
        laneReductionCount = Mathf.Max(LaneReductionCount, _reductionCount);
    }
}
