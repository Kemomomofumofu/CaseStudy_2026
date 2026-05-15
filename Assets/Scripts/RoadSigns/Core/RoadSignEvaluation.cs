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
    /// <param name="_direction"></param>
    public void BlockDirection(TurnDirection _direction)
    {
        blockedDirections.Add(_direction);
    }

    /// <summary>
    /// この方向への進行が禁止されているか
    /// </summary>
    /// <param name="_direction"></param>
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
    /// 速度関係の効果を解決する
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
    /// 移動する方向の設定
    /// </summary>
    /// <param name="_direction"></param>
    public void SetForcedDirection(TurnDirection _direction)
    {
        hasForcedDirection = true;
        forcedDirection = _direction;
    }

    /// <summary>
    /// 
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
    /// <param name="_deltaSpeed"></param>
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
    /// 車線数現象
    /// </summary>
    /// <param name="_reductionCount"></param>
    public void SetLaneReduction(int _reductionCount)
    {
        laneReductionCount = Mathf.Max(LaneReductionCount, _reductionCount);
    }
}