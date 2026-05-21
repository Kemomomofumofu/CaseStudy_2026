using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class RoadSignHandView : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RoadSignHandController handController = null;
    [SerializeField] private RoadSignHandViewItem itemPrefab = null;
    [SerializeField] private Transform contentRoot = null;
    [SerializeField] private RectTransform viewport = null;
    [SerializeField] private float wheelScrollSpeed = 30f;

    private readonly List<RoadSignHandViewItem> items = new();
    private RectTransform contentRect = null;

    private void Awake()
    {
        if (contentRoot != null)
        {
            contentRect = contentRoot as RectTransform;
        }

        Rebuild();
    }

    private void OnEnable()
    {
        if (handController != null)
        {
            handController.Changed += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (handController != null)
        {
            handController.Changed -= Refresh;
        }
    }

    public void OnScroll(PointerEventData _eventData)
    {
        ScrollBy(_eventData.scrollDelta.y * wheelScrollSpeed);
    }

    public void OnBeginDrag(PointerEventData _eventData)
    {
        if (contentRect == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRect, _eventData.position, _eventData.pressEventCamera, out _);
    }

    public void OnDrag(PointerEventData _eventData)
    {
        ScrollBy(_eventData.delta.y);
    }

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

        ClampContentPosition();
    }

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
