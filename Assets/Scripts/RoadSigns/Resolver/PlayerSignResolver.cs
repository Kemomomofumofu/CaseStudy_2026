

using Unity.VisualScripting;

public sealed class PlayerSignResolver
{
    public bool CanMove(RoadSignEvaluation _evaluation, TurnDirection _direction)
    {
        foreach(var effect in _evaluation.Effects)
        {
            if(effect is BlockDirectionEffect block && block.Direction == _direction)
            {
                return false;
            }
        }

        return true;
    }

    public float ResolveMaxSpeed(RoadSignEvaluation _evaluation, float _defaultSpeed)
    {
        float result = _defaultSpeed;
        foreach(var effect in _evaluation.Effects)
        {
            if(effect is SpeedLimitEffect speedLimit && speedLimit.LimitSpeed < result)
            {
                result = speedLimit.LimitSpeed;
            }
        }

        return result;
    }
}
