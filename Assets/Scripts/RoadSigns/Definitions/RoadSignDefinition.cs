using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RoadSigns/Definition", fileName = "RoadSignDefinition")]
public sealed class RoadSignDefinition : ScriptableObject
{
    [Header("表示設定")]
    [SerializeField] private string displayName = "";
    [SerializeField] private Sprite icon = null;
    [SerializeField] private Texture2D signTexture = null;

    [Header("効果設定")]
    [SerializeField] private int priority = 0;
    [SerializeField] private List<RoadSignEffectAsset> effects = new();

    [Header("配置設定")]
    [SerializeField] private RoadSign signPrefab = null;
    [SerializeField] private float lifeTime = 10f; // 配置後の寿命（秒）。0以下の場合は無期限

    private Sprite generatedIcon = null;

    public string DisplayName => displayName;

    public Sprite Icon
    {
        get
        {
            if (icon != null)
            {
                return icon;
            }

            if (signTexture == null)
            {
                return null;
            }

            if (generatedIcon == null)
            {
                generatedIcon = Sprite.Create(
                    signTexture,
                    new Rect(0, 0, signTexture.width, signTexture.height),
                    new Vector2(0.5f, 0.5f));
            }

            return generatedIcon;
        }
    }

    public int Priority => priority;
    public RoadSign SignPrefab => signPrefab;
    public List<RoadSignEffectAsset> Effects => effects;
    public float LifeTime => lifeTime;
}
