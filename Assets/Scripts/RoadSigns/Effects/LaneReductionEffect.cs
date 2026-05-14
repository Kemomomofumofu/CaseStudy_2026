/// <summary>
/// 車線数減少効果
/// </summary>
public sealed class LaneReductionEffect : ISignEffect
{
    public int ReduceCount { get; }

    public LaneReductionEffect(int _reduceCount)
    {
        ReduceCount = _reduceCount;
    }
}
