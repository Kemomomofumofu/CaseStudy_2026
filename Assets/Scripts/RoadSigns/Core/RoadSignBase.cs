using UnityEngine;

/// <summary>
/// 標識の基底クラス
/// </summary>
public abstract class RoadSignBase : MonoBehaviour
{
    [Header("基本設定")]
    [SerializeField] private int priority = 0;
    [SerializeField] private Collider influenceTrigger = null;

    public int Priority => priority;

    /// <summary>
    /// 初期化
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
    /// 標識の影響範囲に入った場合の処理
    /// </summary>
    /// <param name="_other">影響範囲に入ったオブジェクトのコライダー</param>
    protected virtual void OnTriggerEnter(Collider _other)
    {
        if (_other.TryGetComponent(out RoadSignReceiver receiver))
        {
            receiver.AddSign(this);
        }
    }

    protected virtual void OnTriggerExit(Collider _other)
    {
        if (_other.TryGetComponent(out RoadSignReceiver receiver))
        {
            receiver.RemoveSign(this);
        }
    }

    /// <summary>
    /// この標識が現在の状況に関連するかを判定する
    /// </summary>
    public virtual bool IsRelevant(RoadSignQueryContext _context)
    {
        return true;
    }

    /// <summary>
    /// 標識の効果を評価する
    /// </summary>
    public abstract void Evaluate(RoadSignQueryContext _context, RoadSignEvaluation _evaluation);
}