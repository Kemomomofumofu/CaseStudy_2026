using UnityEngine;

/// <summary>
/// 右折禁止標識
/// </summary>
public sealed class NoRightTurnSign : RoadSignBase
{
    public override void Evaluate(RoadSignQueryContext _context, RoadSignDecision _decision)
    {
        if(_context.IntendedDirection == TurnDirection.Right)
        {
            _decision.Block("右折禁止");
        }
    }
}
