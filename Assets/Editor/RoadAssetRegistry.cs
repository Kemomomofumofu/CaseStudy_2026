using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoadAssetEntry
{
    public GameObject prefab;
    public string tag;
}

/// <summary>
/// Editor-time registry of road prefabs that can be reused in the Way Generator.
/// An asset of this type is created automatically when missing.
/// </summary>
public class RoadAssetRegistry : ScriptableObject
{
    public List<RoadAssetEntry> entries = new();
}
