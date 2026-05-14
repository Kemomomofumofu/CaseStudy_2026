using UnityEngine;

[CreateAssetMenu(menuName = "RoadSigns/Definition", fileName = "RoadSignDefinition")]
public sealed class RoadSignDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "";
    [SerializeField] private Sprite icon = null;
    [SerializeField] private RoadSignBase signPrefab = null;
    [SerializeField] private RoadSignType signType = RoadSignType.TurnRight;
    [SerializeField] private RoadSignEffectData effectData = new();
    [SerializeField] private bool overrideDirection = false;
    [SerializeField] private TurnDirection directionOverride = TurnDirection.Straight;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public RoadSignType SignType => signType;
    public RoadSignEffectData EffectData => effectData;
    public RoadSignBase SignPrefab => signPrefab;
    public bool OverrideDirection => overrideDirection;
    public TurnDirection DirectionOverride => directionOverride;
}