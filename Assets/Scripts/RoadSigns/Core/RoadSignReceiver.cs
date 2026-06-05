using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 現在有効な標識を集めるクラス
/// プレイヤー側につける
/// </summary>
public class RoadSignReceiver : MonoBehaviour
{
    private readonly List<RoadSign> activeSigns = new();

    /// <summary>
    /// 現在影響範囲内にある標識を管理する
    /// プレイヤー側に付ける
    /// </summary>
    public void AddSign(RoadSign _sign)
    {
        if (_sign == null || activeSigns.Contains(_sign)) return;

        activeSigns.Add(_sign);
    }

    /// <summary>
    /// 有効な標識を削除する
    /// </summary>
    /// <param name="_sign">削除する標識</param>
    public void RemoveSign(RoadSign _sign)
    {
        if (_sign == null) return;

        activeSigns.Remove(_sign);
    }

    /// <summary>
    /// 現在有効な標識をすべて評価する
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
    /// リストから破壊された標識を削除する
    /// </summary>
    private void RemoveDestroyedSigns()
    {
        activeSigns.RemoveAll(sign => sign == null);
    }

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
