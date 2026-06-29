using UnityEngine;

/// <summary>
/// レーン同士の接続部分を滑らかに移動するための曲線を管理するクラス
/// </summary>
public sealed class LaneTransitionCurve
{
    private const int LengthSampleCount = 32;
    private const float MinimumCurveLength = 0.001f;

    private readonly float[] cumulativeLengths = new float[LengthSampleCount + 1];

    private Vector3 point0;
    private Vector3 point1;
    private Vector3 point2;
    private Vector3 point3;
    private float currentDistance;
    private float totalLength;

    public bool IsActive { get; private set; }
    public float TargetS { get; private set; }

    /// <summary>
    /// 現在レーン終端から次レーン上の合流位置までを結ぶ曲線を生成する
    /// </summary>
    public bool TryBegin(
        Lane _currentLane,
        Lane _nextLane,
        float _targetJoinDistance,
        float _maxHandleLength)
    {
        Clear();

        if (_currentLane == null || _nextLane == null)
        {
            return false;
        }

        float nextLaneLength = _nextLane.Length;
        if (nextLaneLength <= MinimumCurveLength)
        {
            return false;
        }

        TargetS = Mathf.Min(
            Mathf.Max(0f, _targetJoinDistance),
            nextLaneLength * 0.5f);

        point0 = _currentLane.GetPositionByS(_currentLane.Length);
        point3 = _nextLane.GetPositionByS(TargetS);

        Vector3 startForward = GetHorizontalDirection(
            _currentLane.GetForwardByS(_currentLane.Length),
            point3 - point0);
        Vector3 endForward = GetHorizontalDirection(
            _nextLane.GetForwardByS(TargetS),
            point3 - point0);

        float chordLength = Vector3.Distance(point0, point3);
        float desiredHandleLength = Mathf.Max(
            chordLength * 0.5f,
            TargetS * 0.75f);
        float handleLength = Mathf.Min(
            Mathf.Max(0f, _maxHandleLength),
            desiredHandleLength);

        point1 = point0 + startForward * handleLength;
        point2 = point3 - endForward * handleLength;

        BuildLengthTable();
        if (totalLength <= MinimumCurveLength)
        {
            Clear();
            return false;
        }

        currentDistance = 0f;
        IsActive = true;
        return true;
    }

    /// <summary>
    /// 指定距離だけ曲線上を進め、完了時は余った移動距離を返す
    /// </summary>
    public bool Advance(float _distance, out float _overflowDistance)
    {
        _overflowDistance = 0f;
        if (!IsActive)
        {
            return true;
        }

        currentDistance += Mathf.Max(0f, _distance);
        if (currentDistance < totalLength)
        {
            return false;
        }

        _overflowDistance = currentDistance - totalLength;
        currentDistance = totalLength;
        IsActive = false;
        return true;
    }

    /// <summary>
    /// 現在の移動距離に対応する曲線上の位置を返す
    /// </summary>
    public Vector3 GetPosition()
    {
        return EvaluatePosition(GetParameterByDistance(currentDistance));
    }

    /// <summary>
    /// 現在の移動距離に対応する曲線上の進行方向を返す
    /// </summary>
    public Vector3 GetForward()
    {
        float t = GetParameterByDistance(currentDistance);
        Vector3 forward = EvaluateTangent(t);
        forward.y = 0f;

        if (forward.sqrMagnitude <= 1e-6f)
        {
            forward = point3 - point0;
            forward.y = 0f;
        }

        return forward.sqrMagnitude > 1e-6f
            ? forward.normalized
            : Vector3.forward;
    }

    /// <summary>
    /// 補間中の状態を破棄して未使用状態へ戻す
    /// </summary>
    public void Clear()
    {
        IsActive = false;
        TargetS = 0f;
        currentDistance = 0f;
        totalLength = 0f;
    }

    /// <summary>
    /// 曲線を等間隔にサンプリングして距離から曲線位置を求めるための表を作る
    /// </summary>
    private void BuildLengthTable()
    {
        cumulativeLengths[0] = 0f;
        Vector3 previousPosition = point0;

        for (int i = 1; i <= LengthSampleCount; ++i)
        {
            float t = i / (float)LengthSampleCount;
            Vector3 position = EvaluatePosition(t);
            cumulativeLengths[i] =
                cumulativeLengths[i - 1] +
                Vector3.Distance(previousPosition, position);
            previousPosition = position;
        }

        totalLength = cumulativeLengths[LengthSampleCount];
    }

    /// <summary>
    /// 曲線上の移動距離をベジェ曲線のパラメーターへ変換する
    /// </summary>
    private float GetParameterByDistance(float _distance)
    {
        if (totalLength <= MinimumCurveLength)
        {
            return 1f;
        }

        float clampedDistance = Mathf.Clamp(_distance, 0f, totalLength);
        for (int i = 1; i <= LengthSampleCount; ++i)
        {
            if (clampedDistance > cumulativeLengths[i])
            {
                continue;
            }

            float segmentStart = cumulativeLengths[i - 1];
            float segmentLength = cumulativeLengths[i] - segmentStart;
            float segmentRate = segmentLength > MinimumCurveLength
                ? (clampedDistance - segmentStart) / segmentLength
                : 0f;

            return (i - 1 + segmentRate) / LengthSampleCount;
        }

        return 1f;
    }

    /// <summary>
    /// 3次ベジェ曲線上の指定パラメーターに対応する位置を返す
    /// </summary>
    private Vector3 EvaluatePosition(float _t)
    {
        float oneMinusT = 1f - _t;
        return
            oneMinusT * oneMinusT * oneMinusT * point0 +
            3f * oneMinusT * oneMinusT * _t * point1 +
            3f * oneMinusT * _t * _t * point2 +
            _t * _t * _t * point3;
    }

    /// <summary>
    /// 3次ベジェ曲線上の指定パラメーターに対応する接線を返す
    /// </summary>
    private Vector3 EvaluateTangent(float _t)
    {
        float oneMinusT = 1f - _t;
        return
            3f * oneMinusT * oneMinusT * (point1 - point0) +
            6f * oneMinusT * _t * (point2 - point1) +
            3f * _t * _t * (point3 - point2);
    }

    /// <summary>
    /// 水平成分を正規化し、使用できない場合は代替方向を返す
    /// </summary>
    private static Vector3 GetHorizontalDirection(
        Vector3 _direction,
        Vector3 _fallbackDirection)
    {
        _direction.y = 0f;
        if (_direction.sqrMagnitude > 1e-6f)
        {
            return _direction.normalized;
        }

        _fallbackDirection.y = 0f;
        return _fallbackDirection.sqrMagnitude > 1e-6f
            ? _fallbackDirection.normalized
            : Vector3.forward;
    }
}
