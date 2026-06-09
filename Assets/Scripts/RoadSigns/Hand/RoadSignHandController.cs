using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 標識の手札と選択状態を管理する
/// </summary>
public sealed class RoadSignHandController : MonoBehaviour
{
    [SerializeField] private List<RoadSignHandEntry> entries = new();
    [SerializeField] private int selectedIndex = 0;

    public event Action Changed;

    public IReadOnlyList<RoadSignHandEntry> Entries => entries;
    public int SelectedIndex => selectedIndex;

    /// <summary>
    /// 指定された位置の手札を選択して変更を通知する
    /// </summary>
    public void SelectIndex(int _index)
    {

        if (_index < 0 || _index >= entries.Count)
        {
            return;
        }

        selectedIndex = _index;
        Changed?.Invoke();
    }

    /// <summary>
    /// 選択中の標識を1枚消費して定義とPrefabを取得する
    /// </summary>
    public bool TryConsumeSelected(out RoadSignDefinition _definition, out RoadSign _signPrefab)
    {
        _definition = null;
        _signPrefab = null;

        if (!TryGetSelected(out RoadSignHandEntry entry))
        {
            return false;
        }

        if (!entry.TryConsume())
        {
            return false;
        }

        _definition = entry.Definition;
        _signPrefab = entry.SignPrefab;

        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// 使用可能な選択中の手札情報を取得する
    /// </summary>
    public bool TryGetSelected(out RoadSignHandEntry _entry)
    {
        _entry = null;

        if (entries.Count == 0 || selectedIndex < 0 || selectedIndex >= entries.Count)
        {
            return false;
        }

        RoadSignHandEntry entry = entries[selectedIndex];
        if (entry == null || !entry.CanUse)
        {
            return false;
        }

        _entry = entry;
        return true;
    }
}
