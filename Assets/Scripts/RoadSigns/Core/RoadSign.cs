using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 道路標識の定義、所有者、効果範囲を管理する
/// </summary>
public class RoadSign : MonoBehaviour
{
    private static int nextPlacementOrder = 0;

    [Header("基本設定")]
    [SerializeField] private RoadSignDefinition definition = null;
    [SerializeField] private Collider influenceTrigger = null;
    [SerializeField] private GameObject owner = null;

    [Header("表示設定")]
    [SerializeField] private Transform visualRoot = null;

    public int Priority => definition != null ? definition.Priority : 0;
    public int PlacementOrder { get; private set; } = 0;
    public GameObject Owner => owner;

    /// <summary>
    /// コライダーを標識の効果範囲として初期設定する
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
    /// 標識が配置された順番を記録する
    /// </summary>
    protected virtual void Awake()
    {
        PlacementOrder = ++nextPlacementOrder;
        ApplyDefinitionVisual();
    }

    /// <summary>
    /// 標識に寿命が設定されている場合は自動破棄を予約する
    /// </summary>
    protected virtual void Start()
    {
        if (definition != null && definition.LifeTime > 0f)
        {
            Destroy(gameObject, definition.LifeTime);
        }
    }

    /// <summary>
    /// 効果範囲へ入った対象にこの標識を登録する
    /// </summary>
    protected virtual void OnTriggerEnter(Collider _other)
    {
        if (_other.TryGetComponent(out RoadSignReceiver receiver))
        {
            receiver.AddSign(this);
        }
    }

    /// <summary>
    /// 効果範囲から出た対象からこの標識を解除する
    /// </summary>
    protected virtual void OnTriggerExit(Collider _other)
    {
        if (_other.TryGetComponent(out RoadSignReceiver receiver))
        {
            receiver.RemoveSign(this);
        }
    }

    /// <summary>
    /// 対象と所有者の関係を確認し、適用可能な標識効果を評価する
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
    /// この標識で使用する定義を設定する
    /// </summary>
    public void SetDefinition(RoadSignDefinition _definition)
    {
        definition = _definition;
        ApplyDefinitionVisual();
    }

    /// <summary>
    /// この標識を配置した所有者を設定する
    /// </summary>
    public void SetOwner(GameObject _owner)
    {
        owner = _owner;
    }

    /// <summary>
    /// 標識定義に対応する表示モデルだけを有効にする
    /// </summary>
    private void ApplyDefinitionVisual()
    {
        if (visualRoot == null || definition == null || string.IsNullOrEmpty(definition.VisualObjectName))
        {
            return;
        }

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        Renderer targetRenderer = null;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].gameObject.name == definition.VisualObjectName)
            {
                targetRenderer = renderers[i];
                break;
            }
        }

        if (targetRenderer == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            GameObject visualObject = renderers[i].gameObject;
            if (!visualObject.name.StartsWith("看板"))
            {
                continue;
            }

            visualObject.SetActive(renderers[i] == targetRenderer);
        }
    }
}
