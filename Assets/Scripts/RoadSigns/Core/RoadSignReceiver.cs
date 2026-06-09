using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 対象が現在影響を受けている道路標識を管理する
/// </summary>
public class RoadSignReceiver : MonoBehaviour
{
    private readonly List<RoadSign> activeSigns = new();

    /// <summary>
    /// 効果範囲へ入った標識を有効な標識として登録する
    /// </summary>
    public void AddSign(RoadSign _sign)
    {
        if (_sign == null || activeSigns.Contains(_sign)) return;

        activeSigns.Add(_sign);
    }

    /// <summary>
    /// 効果範囲から外れた標識を有効な標識から解除する
    /// </summary>
    public void RemoveSign(RoadSign _sign)
    {
        if (_sign == null) return;

        activeSigns.Remove(_sign);
    }

    /// <summary>
    /// 現在有効な標識を優先度順に評価する
    /// </summary>
    public RoadSignEvaluation Evaluate(RoadSignQueryContext _context)
    {
        var evaluation = new RoadSignEvaluation();

        RemoveDestroyedSigns();

        if (activeSigns.Count == 0)
        {
            return evaluation;
        }

        if (activeSigns.Count > 1)
        {
            // 同じ優先度なら、後に配置された標識ほど後に評価して効果を優先する。
            activeSigns.Sort(CompareSignPriority);
        }

        for (int i = 0; i < activeSigns.Count; i++)
        {
            activeSigns[i].Evaluate(_context, evaluation);
        }

        return evaluation;
    }

    /// <summary>
    /// 破棄済みの標識を管理リストから削除する
    /// </summary>
    private void RemoveDestroyedSigns()
    {
        activeSigns.RemoveAll(sign => sign == null);
    }

    /// <summary>
    /// 標識の優先度と配置順を比較する
    /// </summary>
    private int CompareSignPriority(RoadSign a, RoadSign b)
    {
        int priorityCompare = a.Priority.CompareTo(b.Priority);
        if (priorityCompare != 0)
        {
            return priorityCompare;
        }

        return a.PlacementOrder.CompareTo(b.PlacementOrder);
    }
}
