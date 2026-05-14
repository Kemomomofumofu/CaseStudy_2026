/// <summary>
/// 加速效果
/// </summary>
public sealed class AccelerationEffect : ISignEffect
{
    public float DeltaSpeed { get; }

    public AccelerationEffect(float _deltaSpeed)
    {
        DeltaSpeed = _deltaSpeed;
    }
}
