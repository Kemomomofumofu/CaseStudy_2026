using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RoadSigns/Definition", fileName = "RoadSignDefinition")]
public sealed class RoadSignDefinition : ScriptableObject
{
    [Header("ï\é¶ê›íË")]
    [SerializeField] private string displayName = "";
    [SerializeField] private Sprite icon = null;

    [Header("ï]âøê›íË")]
    [SerializeField] private int priority = 0;
    [SerializeField] private List<RoadSignEffectAsset> effects = new();

    [Header("îzíuê›íË")]
    [SerializeField] private RoadSign signPrefab = null;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public int Priority => priority;
    public RoadSign SignPrefab => signPrefab;
    public List<RoadSignEffectAsset> Effects => effects;
}