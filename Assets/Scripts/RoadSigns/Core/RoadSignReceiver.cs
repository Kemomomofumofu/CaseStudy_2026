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
    /// 標識追加
    /// </summary>
    /// <param name="_sign">追加する標識</param>
    public void AddSign(RoadSign _sign)
    {
        if (!activeSigns.Contains(_sign))
        {
            activeSigns.Add(_sign);
        }
    }
    /// <summary>
    /// 標識削除
    /// </summary>
    /// <param name="_sign">削除する標識</param>
    public void RemoveSign(RoadSign _sign)
    {
        activeSigns.Remove(_sign);
    }

    public RoadSignEvaluation Evaluate(RoadSignQueryContext _context)
    {
        var evaluation = new RoadSignEvaluation();

        // 破棄済みの標識を除外
        for (int i = activeSigns.Count - 1; i >= 0; i--)
        {
            if (activeSigns[i] == null)
            {
                activeSigns.RemoveAt(i);
            }
        }

        // 優先度順にソート
        activeSigns.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        // 各標識を評価
        foreach (var sign in activeSigns)
        {
            // 標識がクエリに関連するか確認
            if (!sign.IsRelevant(_context))
            {
                continue;
            }
            // 標識を評価
            sign.Evaluate(_context, evaluation);
        }

        return evaluation;
    }
}
