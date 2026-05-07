using System.Collections.Generic;
using UnityEngine;

public sealed class RoadSignHandView : MonoBehaviour
{
    [SerializeField] private RoadSignHandController handController = null;
    [SerializeField] private RoadSignHandViewItem itemPrefab = null;
    [SerializeField] private Transform contentRoot = null;

    private readonly List<RoadSignHandViewItem> items = new();

    /// <summary>
    /// 初期生成を行う
    /// </summary>
    private void Awake()
    {
        Rebuild();
    }

    /// <summary>
    /// 変更通知を登録する
    /// </summary>
    private void OnEnable()
    {
        if (handController != null)
        {
            handController.Changed += Refresh;
        }

        Refresh();
    }

    /// <summary>
    /// 変更通知を解除する
    /// </summary>
    private void OnDisable()
    {
        if (handController != null)
        {
            handController.Changed -= Refresh;
        }
    }

    /// <summary>
    /// UI一覧を再生成する
    /// </summary>
    private void Rebuild()
    {
        ClearItems();

        if (handController == null || itemPrefab == null || contentRoot == null)
        {
            return;
        }

        var entries = handController.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            RoadSignHandViewItem item = Instantiate(itemPrefab, contentRoot);
            item.Setup(handController, i, entries[i]);
            items.Add(item);
        }
    }

    /// <summary>
    /// 表示更新を行う
    /// </summary>
    private void Refresh()
    {
        if (handController == null)
        {
            return;
        }

        if (items.Count != handController.Entries.Count)
        {
            Rebuild();
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            items[i].UpdateView(handController.Entries[i], handController.SelectedIndex == i);
        }
    }

    /// <summary>
    /// 生成済みアイテムを破棄する
    /// </summary>
    private void ClearItems()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
            {
                Destroy(items[i].gameObject);
            }
        }

        items.Clear();
    }
}