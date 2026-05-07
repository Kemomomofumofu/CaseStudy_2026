using UnityEngine;

[System.Serializable]
public sealed class RoadSignHandEntry
{
    [SerializeField] private RoadSignDefinition definition = null;
    [SerializeField] private int count = 0;

    public RoadSignDefinition Definition => definition;
    public int Count => count;

    public string DisplayName => definition != null ? definition.DisplayName : string.Empty;
    public Sprite Icon => definition != null ? definition.Icon : null;
    public RoadSignBase SignPrefab => definition != null ? definition.SignPrefab : null;
    public bool OverrideDirection => definition != null && definition.OverrideDirection;
    public TurnDirection DirectionOverride => definition != null ? definition.DirectionOverride : TurnDirection.Straight;

    public bool CanUse => definition != null && definition.SignPrefab != null && count > 0;

    /// <summary>
    /// 1–‡Á”ï‚·‚é
    /// </summary>
    public bool TryConsume()
    {
        if (!CanUse)
        {
            return false;
        }

        count = Mathf.Max(0, count - 1);
        return true;
    }
}