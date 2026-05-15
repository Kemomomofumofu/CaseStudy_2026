using UnityEngine;

/// <summary>
/// 加速標識
/// </summary>
public sealed class AccelerationSign : RoadSign
{
    [SerializeField] private float deltaSpeed = 2f;

    public void Configure(float _deltaSpeed)
    {
        deltaSpeed = _deltaSpeed;
    }

    public override void Evaluate(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        _evaluation.AddEffect(new AccelerationEffect(deltaSpeed));
    }
}
