using System.Collections;
using UnityEngine;


public class SpeedLimitSign : RoadSignBase
{
    [SerializeField] 
    [Tooltip("制限速度")]
    private float limitSpeed = 10.0f;

    public override void Evaluate(RoadSignQueryContext _context, RoadSignDecision _decision)
    {
        _decision.LimitSpeed(limitSpeed);
    }
}

