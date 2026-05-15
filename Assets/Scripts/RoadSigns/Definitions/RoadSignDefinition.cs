using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RoadSigns/Definition", fileName = "RoadSignDefinition")]
public sealed class RoadSignDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "";
    [SerializeField] private Sprite icon = null;
    [SerializeField] private int priority = 0;
    [SerializeField] private RoadSign signPrefab = null;
    [SerializeField] private List<RoadSignEffectAsset> effectData = new();
    [SerializeField] private bool overrideDirection = false;
    [SerializeField] private TurnDirection directionOverride = TurnDirection.Straight;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public int Priority => priority;
    public RoadSign SignPrefab => signPrefab;
    public List<RoadSignEffectAsset> Effects => effects;
    public bool OverrideDirection => overrideDirection;
    public TurnDirection DirectionOverride => directionOverride;
}