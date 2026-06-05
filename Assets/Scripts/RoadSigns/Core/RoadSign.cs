using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 標識の共通クラス
/// </summary>
public class RoadSign : MonoBehaviour
{
    private static int nextPlacementOrder = 0;

    [Header("基本設定")]
    [SerializeField] private RoadSignDefinition definition = null;
    [SerializeField] private Collider influenceTrigger = null;
    [SerializeField] private GameObject owner = null;

    public int Priority => definition != null ? definition.Priority : 0;
    public int PlacementOrder { get; private set; } = 0;
    public GameObject Owner => owner;

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

    protected virtual void Awake()
    {
        PlacementOrder = ++nextPlacementOrder;
    }

    protected virtual void Start()
    {
        if (definition != null && definition.LifeTime > 0f)
        {
            Destroy(gameObject, definition.LifeTime);
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
    /// 標識の効果を評価する
    /// </summary>
    public virtual void Evaluate(RoadSignQueryContext _context, RoadSignEvaluation _evaluation)
    {
        if (definition == null) return;

        IReadOnlyList<RoadSignEffectAsset> effects = definition.Effects;
        if (effects == null) return;

        for (int i = 0; i < effects.Count; ++i)
        {
            RoadSignEffectAsset effect = effects[i];
            if (effect == null) continue;
            if (!effect.CanApplyTo(_context?.Actor, owner)) continue;

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

    public void SetOwner(GameObject _owner)
    {
        owner = _owner;
    }
}
