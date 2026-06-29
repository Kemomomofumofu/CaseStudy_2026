using UnityEngine;

[System.Serializable]
/// <summary>
/// 手札に含まれる標識定義と残り枚数を管理する
/// </summary>
public sealed class RoadSignHandEntry
{
    [SerializeField] private RoadSignDefinition definition = null;
    [SerializeField] private int count = 0;

    public RoadSignDefinition Definition => definition;
    public int Count => count;

    public string DisplayName => definition != null ? definition.DisplayName : string.Empty;
    public Sprite Icon => definition != null ? definition.Icon : null;
    public RoadSign SignPrefab => definition != null ? definition.SignPrefab : null;

    public bool CanUse => definition != null && definition.SignPrefab != null && count > 0;

    /// <summary>
    /// 使用可能な標識を1枚消費する
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
