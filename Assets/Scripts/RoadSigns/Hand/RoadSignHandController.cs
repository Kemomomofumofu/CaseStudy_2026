using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RoadSignHandController : MonoBehaviour
{
    [SerializeField] private List<RoadSignHandEntry> entries = new();
    [SerializeField] private int selectedIndex = 0;

    public event Action Changed;

    public IReadOnlyList<RoadSignHandEntry> Entries => entries;
    public int SelectedIndex => selectedIndex;

    /// <summary>
    /// 表示/配置の対象を選択する
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
    /// 選択中の標識を1枚消費する
    /// </summary>
    public bool TryConsumeSelected(out RoadSignBase _signPrefab, out bool _overrideDirection, out TurnDirection _direction)
    {
        _signPrefab = null;
        _overrideDirection = false;
        _direction = TurnDirection.Straight;

        if (!TryGetSelected(out RoadSignHandEntry entry))
        {
            return false;
        }

        if (!entry.TryConsume())
        {
            return false;
        }

        _signPrefab = entry.SignPrefab;
        _overrideDirection = entry.OverrideDirection;
        _direction = entry.DirectionOverride;

        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// 選択中のエントリを取得する
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