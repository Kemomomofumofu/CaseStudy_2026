using System.Collections.Generic;

/// <summary>
/// 標識のルール評価の結果を集めるクラス
/// </summary>
public sealed class RoadSignEvaluation
{
    private readonly List<ISignEffect> effects = new();

    public IReadOnlyList<ISignEffect> Effects => effects;

    public void AddEffect(ISignEffect _effect)
    {
        effects.Add(_effect);
    }
}