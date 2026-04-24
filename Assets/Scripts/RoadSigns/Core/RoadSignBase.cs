using UnityEngine;
using UnityEngine.Timeline;

/// <summary>
/// 標識の基底クラス
/// 新たに標識を追加する際はBaseクラスを継承して作成してください
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class RoadSignBase : MonoBehaviour
{
    [Header("共通設定")]
    [SerializeField] private int priority = 0; // 標識の優先度(0が最も高い)
    // todo: 影響範囲の設定方法は要検討
    [SerializeField] private Collider influenceTrigger = null; // 標識の影響範囲

    public int Priority => priority;

    /// <summary>
    /// 初期化処理
    /// </summary>
    protected virtual void Reset()
    {
        influenceTrigger = GetComponent<Collider>();
        if (influenceTrigger != null)
        {
            influenceTrigger.isTrigger = true;
        }
    }

    /// <summary>
    /// 標識の影響範囲に入った際の処理
    /// </summary>
    /// <param name="_other">影響範囲に入ったオブジェクトのコライダー</param>
    protected virtual void OnTriggerEnter(Collider _other)
    {
        if(_other.TryGetComponent(out RoadSignReceiver receiver))
        {
            receiver.AddSign(this);
        }
    }

    /// <summary>
    /// この標識が今回の状況に関連するかを判定する
    /// </summary>
    /// <param name="_context"></param>
    /// <returns></returns>
    public virtual bool IsRelevant(RoadSignQueryContext _context)
    {
        return true;
    }

    /// <summary>
    /// 派生した標識側でオーバーライドしてルールを実装してください
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="_decision"></param>
    public abstract void Evaluate(RoadSignQueryContext _context, RoadSignDecision _decision);
}
