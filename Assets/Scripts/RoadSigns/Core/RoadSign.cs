using UnityEngine;

/// <summary>
/// 標識の共通クラス
/// </summary>
public class RoadSign : MonoBehaviour
{
    [Header("基本設定")]
    [SerializeField] private RoadSignDefinition definition = null;
    [SerializeField] private Collider influenceTrigger = null;

    public int Priority => definition != null ? definition.Priority : 0;


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
    public virtual void Evaluate(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        if (definition == null || definition.Effects == null) return;

        for(int i = 0; i < definition.Effects.Count; ++i)
        {
            var effect = definition.Effects[i];
            if (effect == null) continue;

            effect.Apply(_context, _evaluation);
        }
    }

    /// <summary>
    /// 標識の定義を設定する
    /// </summary>
    public void SetDefinition(RoadSignDefinition _definition)
    {
        definition = _definition;
    }
}