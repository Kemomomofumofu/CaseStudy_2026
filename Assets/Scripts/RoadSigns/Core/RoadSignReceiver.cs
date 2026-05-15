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

        // 優先度順にソート
        activeSigns.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        // 各標識を評価
        foreach (var sign in activeSigns)
        {
            // 標識を評価
            sign.Evaluate(_context, evaluation);
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
}
