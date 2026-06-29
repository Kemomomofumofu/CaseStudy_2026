using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 標識手札の一覧表示とスクロール操作を管理する
/// </summary>
public sealed class RoadSignHandView : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RoadSignHandController handController = null;
    [SerializeField] private RoadSignHandViewItem itemPrefab = null;
    [SerializeField] private RoadSignPlacementController placementController = null;
    [SerializeField] private Transform contentRoot = null;
    [SerializeField] private RectTransform viewport = null;
    [SerializeField] private float wheelScrollSpeed = 30f;

    private readonly List<RoadSignHandViewItem> items = new();
    private RectTransform contentRect = null;

    /// <summary>
    /// 配置コントローラーと表示領域を初期化して一覧を構築する
    /// </summary>
    private void Awake()
    {
        if (placementController == null)
        {
            placementController = ResolvePlacementController();
        }

        if (contentRoot != null)
        {
            contentRect = contentRoot as RectTransform;
        }

        Rebuild();
    }

    /// <summary>
    /// 手札の変更通知を購読して表示を更新する
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
    /// 手札の変更通知の購読を解除する
    /// </summary>
    private void OnDisable()
    {
        if (handController != null)
        {
            handController.Changed -= Refresh;
        }
    }

    /// <summary>
    /// マウスホイールの入力に応じて手札一覧をスクロールする
    /// </summary>
    public void OnScroll(PointerEventData _eventData)
    {
        ScrollBy(_eventData.scrollDelta.y * wheelScrollSpeed);
    }

    /// <summary>
    /// ドラッグ開始時に表示領域内の座標を確認する
    /// </summary>
    public void OnBeginDrag(PointerEventData _eventData)
    {
        if (contentRect == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRect, _eventData.position, _eventData.pressEventCamera, out _);
    }

    /// <summary>
    /// ドラッグ量に応じて手札一覧をスクロールする
    /// </summary>
    public void OnDrag(PointerEventData _eventData)
    {
        ScrollBy(_eventData.delta.y);
    }

    /// <summary>
    /// 手札情報からUIアイテムの一覧を再構築する
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
            item.SetPlacementController(placementController);
            item.Setup(handController, i, entries[i]);
            items.Add(item);
        }

        ClampContentPosition();
    }

    /// <summary>
    /// 各UIアイテムの枚数と選択状態を更新する
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

        ClampContentPosition();
    }

    /// <summary>
    /// 指定量だけ一覧を移動して表示範囲内へ補正する
    /// </summary>
    private void ScrollBy(float _deltaY)
    {
        if (contentRect == null)
        {
            return;
        }

        Vector2 anchored = contentRect.anchoredPosition;
        anchored.y -= _deltaY;
        contentRect.anchoredPosition = anchored;
        ClampContentPosition();
    }

    /// <summary>
    /// 一覧の位置をスクロール可能な範囲内へ制限する
    /// </summary>
    private void ClampContentPosition()
    {
        if (contentRect == null)
        {
            return;
        }

        RectTransform viewportRect = viewport != null ? viewport : contentRect.parent as RectTransform;
        if (viewportRect == null)
        {
            return;
        }

        float contentHeight = contentRect.rect.height;
        float viewportHeight = viewportRect.rect.height;
        float maxY = Mathf.Max(0f, contentHeight - viewportHeight);

        Vector2 anchored = contentRect.anchoredPosition;
        anchored.y = Mathf.Clamp(anchored.y, 0f, maxY);
        contentRect.anchoredPosition = anchored;
    }

    /// <summary>
    /// 生成済みの手札UIアイテムをすべて破棄する
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

    /// <summary>
    /// 所有プレイヤーに対応する標識配置コントローラーを取得する
    /// </summary>
    private RoadSignPlacementController ResolvePlacementController()
    {
        PlayerController owner = GetComponentInParent<PlayerController>();
        if (owner != null)
        {
            RoadSignPlacementController childController = owner.GetComponentInChildren<RoadSignPlacementController>(true);
            if (childController != null)
            {
                return childController;
            }
        }

        return FindFirstObjectByType<RoadSignPlacementController>();
    }
}
