using System;

public sealed class PlayerSignResolver
{
    private readonly Random random = new();

    /// <summary>
    /// 道路標識の評価結果から、指定した進行方向に進めるかどうかを判定する
    /// </summary>
    public bool CanMove(RoadSignEvaluation _evaluation, TurnDirection _direction)
    {
        foreach (var effect in _evaluation.Effects)
        {
            // 進行方向が通行止めになっていないか
            if (effect is RoadClosedEffect) return false;
            // 進行方向がブロックされていないか
            if ((effect is BlockDirectionEffect block && block.Direction == _direction)) return false;
        }

        return true;
    }

    /// <summary>
    /// 道路標識の評価結果から、強制的に進まなければならない方向があるかどうかを判定し、あればその方向を返す
    /// </summary>
    public bool TryResolveForcedDirection(RoadSignEvaluation _evaluation, out TurnDirection _direction)
    {
        foreach (var effect in _evaluation.Effects)
        {
            // 進行方向が強制されている場合、その方向を返す
            if (effect is ForceDirectionEffect force)
            {
                _direction = force.Direction;
                return true;
            }

            // 進行方向がランダムに決まる場合、ランダムに方向を選んで返す
            if (effect is RandomDirectionEffect)
            {
                _direction = (TurnDirection)random.Next(0, 4);
                return true;
            }
        }

        _direction = TurnDirection.Straight;
        return false;
    }

    /// <summary>
    /// 道路標識の評価結果から、最大速度を決定する。
    /// </summary>
    public float ResolveMaxSpeed(RoadSignEvaluation _evaluation, float _defaultSpeed)
    {
        float result = _defaultSpeed;
        foreach (var effect in _evaluation.Effects)
        {
            if (effect is SpeedLimitEffect speedLimit && speedLimit.LimitSpeed < result)
            {
                result = speedLimit.LimitSpeed;
            }

            if( effect is AccelerationEffect acceleration)
            {
                result += acceleration.DeltaSpeed;
            }
        }

        return result;
    }

    /// <summary>
    /// 道路標識の評価結果から、停止が必要かどうかを判定する
    /// </summary>
    public bool RequiresStop(RoadSignEvaluation _evaluation)
    {
        foreach (var effect in _evaluation.Effects)
        {
            if (effect is StopEffect)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 道路標識の評価結果から、車線減少の数を決定する。
    /// todo: 実装途中
    /// </summary>
    public int ResolveLaneReduction(RoadSignEvaluation _evaluation)
    {
        int reduce = 0;
        foreach (var effect in _evaluation.Effects)
        {
            if (effect is LaneReductionEffect laneReduction)
            {
                reduce = Math.Max(reduce, laneReduction.ReduceCount);
            }
        }

        return reduce;
    }
}
